# Architecture Decision Records (ADRs) — Fase 4

Os ADRs documentam as decisões arquiteturais permanentes tomadas durante a evolução do sistema **OficinaFiap** para arquitetura distribuída com microsserviços. Cada ADR descreve contexto, opções consideradas e a decisão final.

> Continuação da numeração da Fase 3 (ADR-001 a ADR-005).

---

## ADR-006 — Saga Pattern Orquestrado

| Campo | Valor |
|---|---|
| ID | ADR-006 |
| Status | Aceito |
| Data | Maio 2026 |
| Autores | Henrique Greco |
| Repositório | aka-kensei/oficinafiap-os-service |

### Contexto
A refatoração para microsserviços (Fase 4) exige uma estratégia para coordenar transações distribuídas que atravessam múltiplos serviços: **abertura da OS → diagnóstico → orçamento → pagamento → execução → finalização**. Cada etapa vive em um microsserviço diferente, e cada uma pode falhar exigindo compensação (estorno de estoque, cancelamento de cobrança, remoção da fila, etc.).

### Decisão
Implementar o **Saga Pattern Orquestrado** (orchestration), com o **OS Service** hospedando a máquina de estado (`OSSagaStateMachine`) que comanda as transições. Os demais serviços (Billing, Execução) reagem aos eventos publicados pelo orquestrador e publicam eventos de domínio que o orquestrador consome para avançar o estado.

### Opções Consideradas
- **Coreografia (event-driven puro)** — cada serviço reage aos eventos dos outros sem coordenador central. Mais desacoplado, mas o fluxo global fica implícito no código distribuído, dificultando o debug e a demo no vídeo de 15 minutos.
- **Two-Phase Commit distribuído** — inviável em microsserviços modernos (locks longos, sem suporte robusto no RabbitMQ).
- **Saga Orquestrada (escolhida)** — orquestrador central facilita rastreamento, debug e demonstração visual da transação distribuída. Saga state pode ser consultada via `GET /api/sagas/{osId}` — ponto único de verdade.

### Consequências
- ✅ Fluxo da transação distribuída fica explícito numa única state machine — facilita revisão arquitetural, testes e demonstração no vídeo.
- ✅ Compensações são definidas em um único lugar (`OSSagaStateMachine`).
- ✅ Timeout do Saga (24h sem pagamento → `OSCancelada`) é implementado via `Schedule` do MassTransit — declarativo.
- ⚠️ O orquestrador (OS Service) se torna ponto de acoplamento — mitigado pelo fato de já ser dono dos cadastros e da OS.
- ⚠️ Se o OS Service ficar indisponível, novos eventos ficam parados na fila até o orquestrador voltar — aceitável (a Saga retoma do último estado persistido).

---

## ADR-007 — MassTransit como Framework de Mensageria

| Campo | Valor |
|---|---|
| ID | ADR-007 |
| Status | Aceito |
| Data | Maio 2026 |
| Autores | Henrique Greco |
| Repositório | aka-kensei/oficinafiap-os-service (+ billing, execucao) |

### Contexto
A Fase 4 exige mensageria assíncrona entre microsserviços (RabbitMQ). O ecossistema .NET oferece várias opções de abstração sobre AMQP. A escolha impacta produtividade, manutenibilidade e features avançadas (Saga, Outbox, scheduling).

### Decisão
Adotar **MassTransit 8.3** como camada de abstração sobre RabbitMQ, em todos os 3 microsserviços. Para o OS Service usar adicionalmente `MassTransit.EntityFrameworkCore` para persistir o Saga state.

### Opções Consideradas
- **RabbitMQ.Client (oficial, baixo nível)** — controle máximo, mas exige boilerplate manual para retry, outbox, saga state, type routing. Inviável para o escopo de TCC com prazo apertado.
- **EasyNetQ** — mais simples que MassTransit, mas sem suporte nativo a Saga State Machine.
- **NServiceBus** — comercial, com licença paga acima de certos volumes.
- **MassTransit (escolhido)** — open-source, padrão de mercado em .NET para microsserviços, com Saga State Machine via Automatonymous, Outbox Pattern integrado com EF Core, scheduling de timeouts (`Schedule.Delay`), retry policies declarativas, e telemetria nativa via `Activity` (compatível com Datadog/OpenTelemetry).

### Consequências
- ✅ State machine declarativa torna o fluxo da Saga legível como código (`Initially → During(state) → When(event) → ThenAsync(...)`).
- ✅ Outbox EF Core resolve a falha clássica "evento publicado mas TX rolledback" no OS e Billing.
- ✅ Propagação automática do W3C trace context — Datadog APM mostra traces atravessando os 3 micros sem código adicional.
- ⚠️ Adiciona dependência de framework — mitigado pelo padrão sendo dominante no ecossistema .NET.

---

## ADR-008 — Polyglot Persistence (SQL Server + PostgreSQL + MongoDB)

| Campo | Valor |
|---|---|
| ID | ADR-008 |
| Status | Aceito |
| Data | Maio 2026 |
| Autores | Henrique Greco |
| Repositório | aka-kensei/oficinafiap-* (os, billing, execucao) |

### Contexto
A Fase 4 exige pelo menos um banco SQL e um NoSQL no conjunto de microsserviços, com cada serviço dono do seu próprio banco. A escolha por serviço deve ser técnica, não apenas para cumprir requisito formal.

### Decisão
Adotar persistência heterogênea (polyglot persistence) com:
- **OS Service** → **SQL Server (AWS RDS)** — reaproveita o RDS provisionado na Fase 3, EF Core 8 maduro, ACID estrito necessário para integridade referencial entre Cliente/Veículo/OS/Itens/Estoque.
- **Billing Service** → **PostgreSQL 16** — relacional para integridade transacional de orçamentos+pagamentos, com `jsonb` nativo ideal para guardar payloads completos do webhook Mercado Pago.
- **Execução Service** → **MongoDB 7** — schemaless ajusta-se ao histórico append-only de eventos (`EventoExecucao.Payload` é dicionário arbitrário) e à fila de trabalho que tem leituras por status simples.

### Opções Consideradas
- **Tudo em SQL Server** — cumpriria requisito mínimo (NoSQL via JSON column), mas perderia o argumento técnico de "operação corporativa" exigido pelo PDF.
- **Redis no lugar do Mongo** — bom para cache/fila, mas frágil de defender como persistência primária no vídeo de demo.
- **Cassandra/DynamoDB** — overkill operacional para o escopo acadêmico.

### Consequências
- ✅ Cada banco resolve um problema diferente do domínio — argumento técnico forte para o PDF de entrega.
- ✅ Em produção, Postgres iria pra RDS e Mongo pra Atlas — caminho de upgrade documentado.
- ✅ Mostra domínio de múltiplas tecnologias relevantes no mercado.
- ⚠️ Operação tripla — mitigado pelo escopo acadêmico (todos rodam no cluster Kind local).
- ⚠️ Time precisa conhecer 3 stacks — mitigado pela documentação de cada README.

---

## ADR-009 — Estado da Saga Persistido no OS Service

| Campo | Valor |
|---|---|
| ID | ADR-009 |
| Status | Aceito |
| Data | Maio 2026 |
| Autores | Henrique Greco |
| Repositório | aka-kensei/oficinafiap-os-service |

### Contexto
A Saga orquestrada (ADR-006) tem estado persistido (atual estágio + histórico de eventos consumidos). Era preciso decidir onde esse estado vive: serviço orquestrador dedicado, dentro do OS Service, ou distribuído.

### Decisão
Persistir o Saga state na tabela `SagasOS` dentro do banco do **OS Service**. A classe `Oficina.Billing.Application.Sagas.SagaOS` implementa `MassTransit.SagaStateMachineInstance` + `ISagaVersion`, e o MassTransit usa `EntityFrameworkRepository<SagaOS>` com `ConcurrencyMode.Optimistic` para garantir consistência via concurrency token.

### Opções Consideradas
- **Microsserviço orquestrador dedicado** (4º micro só pro Saga). Mais "puro" para microsserviços, mas estoura o mínimo (PDF pede 3) e adiciona infra desnecessária.
- **Estado distribuído (cada micro guarda seu pedaço)** — mais coreográfico, mas contraria a decisão de Saga orquestrada (ADR-006).
- **OS Service como host (escolhido)** — Saga vive perto da OrdemDeServico no mesmo banco, permitindo TX local entre OS + Saga state quando necessário.

### Consequências
- ✅ Endpoint `GET /api/sagas/{osId}` retorna estado atual + último evento consumido + timestamps — ponto único de observabilidade.
- ✅ Atomicidade entre atualizar OS e atualizar Saga é trivial (mesma TX SQL Server).
- ✅ Não há serviço adicional para operar.
- ⚠️ OS Service tem mais responsabilidades — aceitável dado seu papel central no domínio.

---

## ADR-010 — Namespace Compartilhado para Contratos de Eventos

| Campo | Valor |
|---|---|
| ID | ADR-010 |
| Status | Aceito |
| Data | Maio 2026 |
| Autores | Henrique Greco |
| Repositório | todos os 3 micros |

### Contexto
MassTransit roteia mensagens por URN derivado do `Type.FullName` (namespace + nome). Se cada micro define o evento `OrcamentoSolicitado` em namespace próprio (`Oficina.OS.Application.Events`, `Oficina.Billing.Application.Events`, etc), as exchanges no RabbitMQ não coincidem e o publisher não atinge o consumer.

### Decisão
Todos os eventos compartilhados entre micros vivem no namespace `Oficina.Contracts.Events` em **cada** repositório (cópias idênticas, sem biblioteca compartilhada). O `Type.FullName` fica idêntico (`Oficina.Contracts.Events.OrcamentoSolicitado`), e o MassTransit roteia naturalmente pela mesma exchange.

### Opções Consideradas
- **Biblioteca compartilhada (`Oficina.Contracts.dll` como NuGet)** — solução "padrão indústria", mas cria acoplamento cross-repo e exige publicação de NuGet privado. Inadequado para escopo acadêmico onde cada repo é independente.
- **Atributo `[MessageUrn]` em cada evento** — funciona, mas espalha string mágicas pelo código e exige disciplina manual.
- **Namespace compartilhado, definições duplicadas (escolhido)** — pragmático. Cada repo é self-contained. Risco de drift entre cópias é mitigado por testes BDD de integração + validação de quality gate.

### Consequências
- ✅ Cada repositório é 100% independente — clone + build funcionam sem dependências externas.
- ✅ Routing RabbitMQ funciona sem configuração adicional.
- ⚠️ Risco de drift se um micro evoluir o contrato sem atualizar os outros. Mitigação: testes de contrato no CI (futuro) ou Pact.

---

## ADR-011 — Outbox Pattern via MassTransit (heterogêneo por banco)

| Campo | Valor |
|---|---|
| ID | ADR-011 |
| Status | Aceito |
| Data | Maio 2026 |
| Autores | Henrique Greco |
| Repositório | os-service, billing-service, execucao-service |

### Contexto
Sem Outbox Pattern, há risco de: (a) atualizar o banco e o serviço cair antes de publicar o evento (perda silenciosa), ou (b) publicar o evento e a TX falhar no commit (evento "fantasma"). Em sistemas distribuídos isso quebra a consistência.

### Decisão
- **OS Service** e **Billing Service** → **EF Core Outbox** (MassTransit grava o evento na mesma TX do banco; um worker assíncrono envia ao RabbitMQ e marca como enviado).
- **Execução Service (MongoDB)** → **InMemoryOutbox** + **retry policies** (3 tentativas com backoff). MongoDB não suporta o padrão Outbox SQL do MassTransit, e o uso de transações multi-collection no Mongo seria custoso.

### Opções Consideradas
- **Sem outbox** — simples, mas inaceitável para integridade da Saga.
- **Outbox manual (tabela própria + worker)** — reinventa a roda; MassTransit já oferece.
- **Outbox MassTransit + Mongo transactions** — Mongo 4+ suporta TX, mas o MassTransit não tem provider Mongo para outbox. Desenvolveria provider custom, complexo demais.
- **EF Outbox onde for possível, InMemory + retry no Execução (escolhido)** — pragmático, alinhado com o que cada banco oferece.

### Consequências
- ✅ OS e Billing: atomicidade garantida entre persistência e publicação.
- ⚠️ Execução: em caso de crash entre Mongo insert e RabbitMQ publish, o evento pode ser perdido. Aceitável porque os eventos do Execução não são compensáveis (são informativos) e o handler é idempotente.
- ✅ Custo cognitivo baixo — `cfg.UseInMemoryOutbox(ctx)` vs `o.UsePostgres()` é uma linha de configuração.
