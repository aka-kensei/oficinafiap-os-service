FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY oficinafiap-os-service.sln .
COPY Oficina.OS.Api/Oficina.OS.Api.csproj Oficina.OS.Api/
COPY Oficina.OS.Application/Oficina.OS.Application.csproj Oficina.OS.Application/
COPY Oficina.OS.Domain/Oficina.OS.Domain.csproj Oficina.OS.Domain/
COPY Oficina.OS.Infrastructure/Oficina.OS.Infrastructure.csproj Oficina.OS.Infrastructure/
COPY Oficina.OS.Domain.UnitTests/Oficina.OS.Domain.UnitTests.csproj Oficina.OS.Domain.UnitTests/
COPY Oficina.OS.Application.UnitTests/Oficina.OS.Application.UnitTests.csproj Oficina.OS.Application.UnitTests/
COPY Oficina.OS.Api.IntegrationTests/Oficina.OS.Api.IntegrationTests.csproj Oficina.OS.Api.IntegrationTests/

RUN dotnet restore "oficinafiap-os-service.sln"

COPY . .

RUN dotnet build "oficinafiap-os-service.sln" -c Release --no-restore

RUN dotnet test "Oficina.OS.Domain.UnitTests/Oficina.OS.Domain.UnitTests.csproj" -c Release --no-build --no-restore
RUN dotnet test "Oficina.OS.Application.UnitTests/Oficina.OS.Application.UnitTests.csproj" -c Release --no-build --no-restore

WORKDIR /src/Oficina.OS.Api
RUN dotnet publish "Oficina.OS.Api.csproj" -c Release -o /app/publish --no-restore --no-build

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080

# Datadog .NET Tracer (CLR Profiler) — auto-instrumentação
RUN apt-get update && apt-get install -y curl && \
    curl -Lo /tmp/datadog-dotnet-apm.deb https://github.com/DataDog/dd-trace-dotnet/releases/download/v3.3.1/datadog-dotnet-apm_3.3.1_amd64.deb && \
    dpkg -i /tmp/datadog-dotnet-apm.deb && \
    rm /tmp/datadog-dotnet-apm.deb && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_DOTNET_TRACER_HOME=/opt/datadog

EXPOSE 8080
ENTRYPOINT ["dotnet", "Oficina.OS.Api.dll"]
