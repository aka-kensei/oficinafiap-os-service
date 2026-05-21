using FluentAssertions;
using MassTransit;
using Moq;
using Oficina.Contracts.Events;
using Oficina.OS.Application.DTOs;
using Oficina.OS.Application.Interfaces;
using Oficina.OS.Application.UseCases;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.Exceptions;
using Oficina.OS.Domain.Repositories;
using Oficina.OS.Domain.ValueObjects;
using Xunit;

namespace Oficina.OS.Application.UnitTests.UseCases;

public class OrdemDeServicoUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _osRepo = new();
    private readonly Mock<IClienteRepository> _clienteRepo = new();
    private readonly Mock<IVeiculoRepository> _veiculoRepo = new();
    private readonly Mock<IPecaRepository> _pecaRepo = new();
    private readonly Mock<IServicoRepository> _servicoRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IPublishEndpoint> _publish = new();
    private readonly OrdemDeServicoUseCase _sut;

    public OrdemDeServicoUseCaseTests()
    {
        _sut = new OrdemDeServicoUseCase(
            _osRepo.Object, _clienteRepo.Object, _veiculoRepo.Object,
            _pecaRepo.Object, _servicoRepo.Object, _uow.Object, _publish.Object);
    }

    [Fact]
    public async Task CriarAsync_ClienteNaoExiste_LancaDomainException()
    {
        _clienteRepo.Setup(r => r.ObterPorCpfAsync(It.IsAny<CPF>())).ReturnsAsync((Cliente?)null);

        var input = new CriarOSInput("52998224725", "ABC1234", Array.Empty<Guid>(), Array.Empty<ItemPecaInput>());
        var act = () => _sut.CriarAsync(input);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Cliente não cadastrado*");
    }

    [Fact]
    public async Task CriarAsync_VeiculoNaoExiste_LancaDomainException()
    {
        var cliente = new Cliente("J", CPF.Criar("52998224725")!, "j@e.com", "11");
        _clienteRepo.Setup(r => r.ObterPorCpfAsync(It.IsAny<CPF>())).ReturnsAsync(cliente);
        _veiculoRepo.Setup(r => r.ObterPorPlacaAsync(It.IsAny<Placa>())).ReturnsAsync((Veiculo?)null);

        var input = new CriarOSInput("52998224725", "ABC1234", Array.Empty<Guid>(), Array.Empty<ItemPecaInput>());
        var act = () => _sut.CriarAsync(input);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Veículo não cadastrado*");
    }

    [Fact]
    public async Task CriarAsync_VeiculoDeOutroCliente_LancaDomainException()
    {
        var cliente = new Cliente("J", CPF.Criar("52998224725")!, "j@e.com", "11");
        var veiculo = new Veiculo(Placa.Criar("ABC1234")!, "T", "Y", 2020, Guid.NewGuid()); // ClienteId diferente
        _clienteRepo.Setup(r => r.ObterPorCpfAsync(It.IsAny<CPF>())).ReturnsAsync(cliente);
        _veiculoRepo.Setup(r => r.ObterPorPlacaAsync(It.IsAny<Placa>())).ReturnsAsync(veiculo);

        var input = new CriarOSInput("52998224725", "ABC1234", Array.Empty<Guid>(), Array.Empty<ItemPecaInput>());
        var act = () => _sut.CriarAsync(input);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*pertence*");
    }

    [Fact]
    public async Task CriarAsync_FluxoFeliz_PersisteOSEPublicaOSCriada()
    {
        var clienteId = Guid.NewGuid();
        var cliente = new Cliente("João", CPF.Criar("52998224725")!, "j@e.com", "11");
        SetCliendeId(cliente, clienteId);

        var veiculo = new Veiculo(Placa.Criar("ABC1234")!, "Toyota", "Corolla", 2020, clienteId);

        _clienteRepo.Setup(r => r.ObterPorCpfAsync(It.IsAny<CPF>())).ReturnsAsync(cliente);
        _veiculoRepo.Setup(r => r.ObterPorPlacaAsync(It.IsAny<Placa>())).ReturnsAsync(veiculo);

        var input = new CriarOSInput("52998224725", "ABC1234", Array.Empty<Guid>(), Array.Empty<ItemPecaInput>());

        // Adiciono pelo menos 1 serviço pra ter valor > 0 (opcional pra o teste)
        var servicoId = Guid.NewGuid();
        var servico = new Servico("Troca de óleo", 150m, 1m);
        _servicoRepo.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(servico);
        input = input with { ServicosIds = new[] { servicoId } };

        var vm = await _sut.CriarAsync(input);

        vm.NomeCliente.Should().Be("João");
        _osRepo.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _publish.Verify(p => p.Publish(It.IsAny<OSCriada>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AprovarAsync_PublicaOSAprovadaPeloCliente()
    {
        var os = new OrdemDeServico(Guid.NewGuid(), Guid.NewGuid());
        os.MarcarDiagnosticoIniciado();
        os.MarcarAguardandoAprovacao(100m);
        _osRepo.Setup(r => r.ObterPorIdCompletaAsync(It.IsAny<Guid>())).ReturnsAsync(os);

        await _sut.AprovarAsync(os.Id);

        _publish.Verify(p => p.Publish(It.IsAny<OSAprovadaPeloCliente>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReprovarAsync_PublicaOSReprovadaPeloCliente()
    {
        var os = new OrdemDeServico(Guid.NewGuid(), Guid.NewGuid());
        os.MarcarDiagnosticoIniciado();
        os.MarcarAguardandoAprovacao(100m);
        _osRepo.Setup(r => r.ObterPorIdCompletaAsync(It.IsAny<Guid>())).ReturnsAsync(os);

        await _sut.ReprovarAsync(os.Id, "Caro demais");

        _publish.Verify(p => p.Publish(
            It.Is<OSReprovadaPeloCliente>(e => e.Motivo == "Caro demais"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EntregarAsync_OSNaoExiste_RetornaNull()
    {
        _osRepo.Setup(r => r.ObterPorIdCompletaAsync(It.IsAny<Guid>())).ReturnsAsync((OrdemDeServico?)null);

        var vm = await _sut.EntregarAsync(Guid.NewGuid());

        vm.Should().BeNull();
    }

    // Helper para definir o Id privado do Cliente via reflection (apenas para teste).
    private static void SetCliendeId(Cliente cliente, Guid id)
    {
        typeof(Cliente).GetProperty(nameof(Cliente.Id))!.SetValue(cliente, id);
    }
}
