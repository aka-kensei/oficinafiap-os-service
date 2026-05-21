namespace Oficina.OS.Domain.Enums;

/// <summary>
/// Estados da máquina de estado da Saga orquestrada pelo OS Service.
/// Persistida na tabela SagaOS (uma linha por OS).
/// </summary>
public enum SagaState
{
    Iniciada,
    AguardandoDiagnostico,
    EmDiagnostico,
    OrcamentoSolicitado,
    AguardandoAprovacaoCliente,
    AguardandoPagamento,
    EmExecucao,
    Finalizada,
    Concluida,
    Compensando,
    Cancelada,
    Falhou
}
