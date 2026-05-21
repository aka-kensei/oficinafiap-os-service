using FluentAssertions;
using Moq;
using Oficina.OS.Application.DTOs;
using Oficina.OS.Application.Interfaces;
using Oficina.OS.Application.UseCases;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.Exceptions;
using Oficina.OS.Domain.Repositories;
using Oficina.OS.Domain.ValueObjects;
using Xunit;

namespace Oficina.OS.Application.UnitTests.UseCases;

public class ClienteUseCaseTests
{
    private readonly Mock<IClienteRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly ClienteUseCase _sut;

    public ClienteUseCaseTests()
    {
        _sut = new ClienteUseCase(_repo.Object, _uow.Object);
    }

    [Fact]
    public async Task CriarAsync_ComDadosValidosECPFNovo_PersisteCliente()
    {
        var input = new CriarClienteInput("João", "529.982.247-25", "j@e.com", "11");
        _repo.Setup(r => r.ObterPorCpfAsync(It.IsAny<CPF>())).ReturnsAsync((Cliente?)null);

        var vm = await _sut.CriarAsync(input);

        vm.Nome.Should().Be("João");
        vm.Cpf.Should().Be("52998224725");
        _repo.Verify(r => r.AdicionarAsync(It.IsAny<Cliente>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_ComCPFInvalido_LancaDomainException()
    {
        var input = new CriarClienteInput("João", "111.111.111-11", "j@e.com", "11");

        var act = () => _sut.CriarAsync(input);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*CPF*");
    }

    [Fact]
    public async Task CriarAsync_ComCPFDuplicado_LancaDomainException()
    {
        var input = new CriarClienteInput("João", "529.982.247-25", "j@e.com", "11");
        var existente = new Cliente("Outro", CPF.Criar("52998224725")!, "o@e.com", "22");
        _repo.Setup(r => r.ObterPorCpfAsync(It.IsAny<CPF>())).ReturnsAsync(existente);

        var act = () => _sut.CriarAsync(input);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*existe*");
        _repo.Verify(r => r.AdicionarAsync(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task ObterPorCpfAsync_QuandoExiste_RetornaViewModel()
    {
        var cliente = new Cliente("João", CPF.Criar("52998224725")!, "j@e.com", "11");
        _repo.Setup(r => r.ObterPorCpfAsync(It.IsAny<CPF>())).ReturnsAsync(cliente);

        var vm = await _sut.ObterPorCpfAsync("52998224725");

        vm.Should().NotBeNull();
        vm!.Nome.Should().Be("João");
    }

    [Fact]
    public async Task AtualizarAsync_ClienteNaoExiste_RetornaNull()
    {
        _repo.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Cliente?)null);

        var vm = await _sut.AtualizarAsync(Guid.NewGuid(), new AtualizarClienteInput("X", "x@e.com", "11"));

        vm.Should().BeNull();
    }
}
