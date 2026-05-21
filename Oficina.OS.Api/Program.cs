using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Oficina.OS.Api.Middlewares;
using Oficina.OS.Application.Interfaces;
using Oficina.OS.Application.UseCases;
using Oficina.OS.Domain.Repositories;
using Oficina.OS.Infrastructure.Database;
using Oficina.OS.Infrastructure.Messaging;
using Oficina.OS.Infrastructure.Repositories;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog (JSON + correlação Datadog) ────────────────────────────
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Oficina.OS.Api")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
});

// ── Banco de dados ─────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("OSDb")!;
builder.Services.AddDbContext<OSDbContext>(options =>
{
    options.UseSqlServer(connectionString,
        sql => sql.MigrationsAssembly(typeof(OSDbContext).Assembly.FullName));
});

// ── Health checks ──────────────────────────────────────────────────
var rabbitConn = $"amqp://{builder.Configuration["RabbitMq:Username"]}:{builder.Configuration["RabbitMq:Password"]}@{builder.Configuration["RabbitMq:Host"]}:5672";
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "sqlserver", failureStatus: HealthStatus.Unhealthy, tags: ["db"])
    .AddRabbitMQ(rabbitConn, name: "rabbitmq", failureStatus: HealthStatus.Unhealthy, tags: ["broker"]);

// ── Repositórios + UoW ─────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IVeiculoRepository, VeiculoRepository>();
builder.Services.AddScoped<IPecaRepository, PecaRepository>();
builder.Services.AddScoped<IServicoRepository, ServicoRepository>();
builder.Services.AddScoped<IOrdemDeServicoRepository, OrdemDeServicoRepository>();

// ── Use Cases ──────────────────────────────────────────────────────
builder.Services.AddScoped<IClienteUseCase, ClienteUseCase>();
builder.Services.AddScoped<IVeiculoUseCase, VeiculoUseCase>();
builder.Services.AddScoped<IPecaUseCase, PecaUseCase>();
builder.Services.AddScoped<IServicoUseCase, ServicoUseCase>();
builder.Services.AddScoped<IOrdemDeServicoUseCase, OrdemDeServicoUseCase>();

// ── MassTransit + RabbitMQ + Saga ──────────────────────────────────
builder.Services.AddOSMassTransit(builder.Configuration);

// ── JWT (compatível com Azure Function de autenticação da Fase 3) ──
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
    });
builder.Services.AddAuthorization();

// ── Controllers + Swagger ──────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Oficina OS Service", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Cole o token JWT (sem 'Bearer').",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var payload = new { status = report.Status.ToString(), timestamp = DateTime.UtcNow };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
});

app.MapHealthChecks("/health/detail", new HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var payload = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs = e.Value.Duration.TotalMilliseconds
            })
        };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
});

// ── Migrations no startup com retry ────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<OSDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    for (var i = 1; i <= 10; i++)
    {
        try
        {
            logger.LogInformation("Aplicando migrations… tentativa {Attempt}/10", i);
            ctx.Database.Migrate();
            logger.LogInformation("Migrations aplicadas com sucesso.");
            break;
        }
        catch (Exception ex) when (i < 10)
        {
            logger.LogWarning(ex, "Falha ao conectar ao banco. Retry em 3s.");
            Thread.Sleep(3000);
        }
    }
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
