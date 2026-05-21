using FluentAssertions;
using MassTransit;
using Moq;
using Oficina.Contracts.Events;
using Oficina.OS.Application.DTOs;
using Oficina.OS.Application.Interfaces;
using Oficina.OS.Application.UseCases;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.Repositories;
using Oficina.OS.Domain.ValueObjects;
using Reqnroll;

namespace Oficina.OS.Api.IntegrationTests.Steps;

[Binding]
public class AberturaDeOSSteps
{
    private readonly Mock<IOrdemDeServicoRepository> _osRepo = new();
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IVeiculoRepository> _veiculoRepo = new();
    private readonly Mock<IPecaRepository> _pecaRepo = new();
    private readonly Mock<IServicoRepository> _servicoRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPublishEndpoint> _publish = new();

    private readonly List<object> _eventosPublicados = new();
    private OrdemDeServicoUseCase _useCase = null!;
    private OrdemDeServicoViewModel? _osCriada;
    private OrdemDeServico? _osExistente;

    private Cliente _cliente = null!;
    private Veiculo _veiculo = null!;
    private Servico _servico = null!;

    public AberturaDeOSSteps()
    {
        // Matches MassTransit's generic Publish<T>(T, CancellationToken) overload.
        _publish
            .Setup(p => p.Publish(It.IsAny<It.IsAnyType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(new InvocationAction(inv => _eventosPublicados.Add(inv.Arguments[0])));

        _useCase = new OrdemDeServicoUseCase(
            _osRepo.Object, _clienteRepo.Object, _veiculoRepo.Object,
            _pecaRepo.Object, _servicoRepo.Object, _uow.Object, _publish.Object);
    }

    [Given(@"que existe um cliente cadastrado com CPF ""(.*)""")]
    public void DadoClienteCadastrado(string cpfStr)
    {
        var cpf = CPF.Criar(cpfStr)!;
        _cliente = new Cliente("João Silva", cpf, "joao@email.com", "11999999999");
        _clienteRepo.Setup(r => r.ObterPorCpfAsync(It.Is<CPF>(c => c.Numero == cpf.Numero)))
            .ReturnsAsync(_cliente);
    }

    [Given(@"que o cliente possui um veículo de placa ""(.*)""")]
    public void DadoVeiculoDoCliente(string placaStr)
    {
        var placa = Placa.Criar(placaStr)!;
        _veiculo = new Veiculo(placa, "Toyota", "Corolla", 2020, _cliente.Id);
        _veiculoRepo.Setup(r => r.ObterPorPlacaAsync(It.Is<Placa>(p => p.Valor == placa.Valor)))
            .ReturnsAsync(_veiculo);
    }

    [Given(@"que existe um serviço ""(.*)"" no catálogo com preço (.*)")]
    public void DadoServicoNoCatalogo(string descricao, decimal preco)
    {
        _servico = new Servico(descricao, preco, 1m);
        _servicoRepo.Setup(r => r.ObterPorIdAsync(_servico.Id)).ReturnsAsync(_servico);
    }

    [When(@"uma OS é aberta para esse cliente com o serviço de ""(.*)""")]
    public async Task QuandoOSAberta(string _)
    {
        var input = new CriarOSInput(
            _cliente.Cpf.ToString(),
            _veiculo.Placa.ToString(),
            new[] { _servico.Id },
            Array.Empty<ItemPecaInput>());

        _osCriada = await _useCase.CriarAsync(input);
    }

    [Then(@"a OS é persistida com status ""(.*)""")]
    public void EntaoOSPersistida(string statusEsperado)
    {
        _osCriada.Should().NotBeNull();
        _osCriada!.Status.ToString().Should().Be(statusEsperado);
        _osRepo.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()), Times.Once);
    }

    [Then(@"o evento ""(.*)"" é publicado no barramento com o CPF e a placa corretos")]
    public void EntaoEventoPublicadoComDados(string nomeEvento)
    {
        nomeEvento.Should().Be(nameof(OSCriada));
        var evt = _eventosPublicados.OfType<OSCriada>().Should().ContainSingle().Subject;
        evt.ClienteCpf.Should().Be(_cliente.Cpf.ToString());
        evt.VeiculoPlaca.Should().Be(_veiculo.Placa.ToString());
    }

    [Then(@"o orçamento da OS deve ser (.*)")]
    public void EntaoOrcamentoDeveSer(decimal valorEsperado)
    {
        _osCriada!.ValorOrcamento.Should().Be(valorEsperado);
    }

    [Given(@"que existe uma OS no status ""AguardandoAprovacao""")]
    public void DadoOSEmAguardandoAprovacao()
    {
        _osExistente = new OrdemDeServico(Guid.NewGuid(), Guid.NewGuid());
        _osExistente.MarcarDiagnosticoIniciado();
        _osExistente.MarcarAguardandoAprovacao(250m);
        _osRepo.Setup(r => r.ObterPorIdCompletaAsync(_osExistente.Id)).ReturnsAsync(_osExistente);
    }

    [When(@"o cliente aprova a OS")]
    public async Task QuandoClienteAprova()
    {
        await _useCase.AprovarAsync(_osExistente!.Id);
    }

    [Then(@"o evento ""(.*)"" é publicado no barramento")]
    public void EntaoEventoPublicado(string nomeEvento)
    {
        _eventosPublicados
            .Should().Contain(e => e.GetType().Name == nomeEvento);
    }

    [When(@"o cliente reprova a OS com o motivo ""(.*)""")]
    public async Task QuandoClienteReprovaComMotivo(string motivo)
    {
        await _useCase.ReprovarAsync(_osExistente!.Id, motivo);
    }

    [Then(@"o evento ""(.*)"" é publicado com motivo ""(.*)""")]
    public void EntaoEventoPublicadoComMotivo(string nomeEvento, string motivo)
    {
        nomeEvento.Should().Be(nameof(OSReprovadaPeloCliente));
        var evt = _eventosPublicados.OfType<OSReprovadaPeloCliente>().Should().ContainSingle().Subject;
        evt.Motivo.Should().Be(motivo);
    }
}
