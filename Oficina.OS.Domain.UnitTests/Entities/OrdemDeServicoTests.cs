using FluentAssertions;
using Oficina.OS.Domain.Entities;
using Oficina.OS.Domain.Enums;
using Oficina.OS.Domain.Exceptions;
using Xunit;

namespace Oficina.OS.Domain.UnitTests.Entities;

public class OrdemDeServicoTests
{
    private static OrdemDeServico CriarOS() => new(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Construtor_CriaOSNoStatusRecebida()
    {
        var os = CriarOS();
        os.Status.Should().Be(StatusOS.Recebida);
        os.ValorOrcamento.Should().Be(0);
        os.DataCriacao.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void AdicionarServico_QuandoRecebida_AumentaValorOrcamento()
    {
        var os = CriarOS();
        var servico = new Servico("Troca de óleo", 150m, 1m);

        os.AdicionarServico(servico);

        os.ItensServico.Should().HaveCount(1);
        os.ValorOrcamento.Should().Be(150m);
    }

    [Fact]
    public void AdicionarPeca_QuandoRecebida_AdicionaItem()
    {
        var os = CriarOS();
        var peca = new Peca("Filtro", 40m, 10);

        os.AdicionarPeca(peca, 2);

        os.ItensPeca.Should().HaveCount(1);
        os.ValorOrcamento.Should().Be(80m);
    }

    [Fact]
    public void AdicionarPeca_QuandoEmExecucao_LancaDomainException()
    {
        var os = CriarOS();
        os.MarcarDiagnosticoIniciado();
        os.MarcarAguardandoAprovacao(100m);
        os.MarcarEmExecucao();

        var peca = new Peca("Filtro", 40m, 10);
        var act = () => os.AdicionarPeca(peca, 1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AdicionarPeca_Duplicada_LancaDomainException()
    {
        var os = CriarOS();
        var peca = new Peca("Filtro", 40m, 10);
        os.AdicionarPeca(peca, 1);

        var act = () => os.AdicionarPeca(peca, 1);
        act.Should().Throw<DomainException>().WithMessage("*já*");
    }

    [Fact]
    public void MarcarDiagnosticoIniciado_QuandoRecebida_TransicionaParaEmDiagnostico()
    {
        var os = CriarOS();
        os.MarcarDiagnosticoIniciado();
        os.Status.Should().Be(StatusOS.EmDiagnostico);
    }

    [Fact]
    public void MarcarDiagnosticoIniciado_QuandoJaEmDiagnostico_LancaDomainException()
    {
        var os = CriarOS();
        os.MarcarDiagnosticoIniciado();

        var act = () => os.MarcarDiagnosticoIniciado();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarcarAguardandoAprovacao_QuandoEmDiagnostico_AtualizaValorEStatus()
    {
        var os = CriarOS();
        os.MarcarDiagnosticoIniciado();

        os.MarcarAguardandoAprovacao(250m);

        os.Status.Should().Be(StatusOS.AguardandoAprovacao);
        os.ValorOrcamento.Should().Be(250m);
    }

    [Fact]
    public void MarcarAguardandoAprovacao_ComValorZerado_LancaDomainException()
    {
        var os = CriarOS();
        os.MarcarDiagnosticoIniciado();

        var act = () => os.MarcarAguardandoAprovacao(0);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Aprovar_QuandoAguardandoAprovacao_NaoLancaException()
    {
        var os = CriarOS();
        os.MarcarDiagnosticoIniciado();
        os.MarcarAguardandoAprovacao(100m);

        var act = () => os.Aprovar();
        act.Should().NotThrow();
    }

    [Fact]
    public void Aprovar_QuandoRecebida_LancaDomainException()
    {
        var os = CriarOS();
        var act = () => os.Aprovar();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarcarEmExecucao_PreencheDataInicio()
    {
        var os = CriarOS();
        os.MarcarDiagnosticoIniciado();
        os.MarcarAguardandoAprovacao(100m);
        os.MarcarEmExecucao();

        os.Status.Should().Be(StatusOS.EmExecucao);
        os.DataInicioExecucao.Should().NotBeNull();
    }

    [Fact]
    public void MarcarFinalizada_PreencheDataFinalizacao()
    {
        var os = CriarOS();
        os.MarcarDiagnosticoIniciado();
        os.MarcarAguardandoAprovacao(100m);
        os.MarcarEmExecucao();
        os.MarcarFinalizada();

        os.Status.Should().Be(StatusOS.Finalizada);
        os.DataFinalizacao.Should().NotBeNull();
    }

    [Fact]
    public void TempoTotalExecucaoHoras_AposFinalizar_RetornaDuracao()
    {
        var os = CriarOS();
        os.MarcarDiagnosticoIniciado();
        os.MarcarAguardandoAprovacao(100m);
        os.MarcarEmExecucao();
        Thread.Sleep(10);
        os.MarcarFinalizada();

        os.TempoTotalExecucaoHoras.Should().NotBeNull();
        os.TempoTotalExecucaoHoras.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void Entregar_QuandoFinalizada_TransicionaParaEntregue()
    {
        var os = CriarOS();
        os.MarcarDiagnosticoIniciado();
        os.MarcarAguardandoAprovacao(100m);
        os.MarcarEmExecucao();
        os.MarcarFinalizada();
        os.Entregar();

        os.Status.Should().Be(StatusOS.Entregue);
    }

    [Fact]
    public void Reprovar_QuandoAguardandoAprovacao_VoltaParaRecebida()
    {
        var os = CriarOS();
        os.MarcarDiagnosticoIniciado();
        os.MarcarAguardandoAprovacao(100m);

        os.Reprovar();

        os.Status.Should().Be(StatusOS.Recebida);
    }

    [Fact]
    public void Cancelar_QuandoEmExecucao_LancaDomainException()
    {
        var os = CriarOS();
        os.MarcarDiagnosticoIniciado();
        os.MarcarAguardandoAprovacao(100m);
        os.MarcarEmExecucao();

        var act = () => os.Cancelar();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancelar_QuandoRecebida_TransicionaParaCancelada()
    {
        var os = CriarOS();
        os.Cancelar();
        os.Status.Should().Be(StatusOS.Cancelada);
    }
}
