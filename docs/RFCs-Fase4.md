# Request for Comments (RFCs) — Fase 4

Os RFCs documentam decisões técnicas relevantes com justificativas formais, alternativas avaliadas e impactos esperados.

> Continuação da numeração da Fase 3 (RFC-001 a RFC-005).

---

## RFC-006 — Divisão em 3 Microsserviços

| Campo | Valor |
|---|---|
| ID | RFC-006 |
| Status | Aprovado |
| Data | Maio 2026 |
| Autores | Henrique Greco |
| Impacto | Alto — define a arquitetura distribuída inteira |

### Contexto
O monolito da Fase 3 reúne em um único deploy a gestão de cadastros (Cliente/Veículo/Catálogo), o ciclo de vida da Ordem de Serviço, o orçamento, o pagamento e a execução. A Fase 4 exige refatorar em pelo menos 3 microsserviços independentes, cada um com banco próprio.

### Justificativa da Escolha
Adotamos a divisão por **bounded context** (DDD), priorizando a coesão funcional sobre a coesão técnica:

1. **OS Service** — cadastros (Cliente, Veículo, Peça, Serviço) + ciclo de vida da OS + orquestrador da Saga. Justificativa: cadastros são consultados na abertura da OS, e a OS é o agregado central. Manter no mesmo serviço evita um 4º micro de "cadastros" que seria chamado o tempo todo.
2. **Billing Service** — orçamentos e pagamentos com integração Mercado Pago. Justificativa: pagamento tem fronteira clara (entrada via evento, saída via webhook MP) e código de domínio totalmente independente do resto.
3. **Execução Service** — fila de execução, diagnóstico, reparos, histórico de eventos. Justificativa: representa o "chão de fábrica" da oficina, com fluxo operacional próprio (mecânico interage diretamente) e dados de série temporal (event log) que beneficiam de banco NoSQL.

### Alternativas Avaliadas
- **5 microsserviços** (Cadastros, OS, Billing, Execução, Estoque) — granularidade excessiva para a equipe (1 dev) e para o domínio.
- **4 microsserviços** (OS, Catálogo, Billing, Execução) — Catálogo separado seria chamado em tempo síncrono pelo OS na abertura da OS, gerando acoplamento síncrono evitável.
- **3 microsserviços por camada técnica** (API, Worker, Saga) — anti-padrão; quebra DDD.
- **3 microsserviços por bounded context (escolhido)** — alinhado com DDD, com cada serviço dono de seu agregado e seu banco.

### Mapeamento Agregado → Microsserviço

| Microsserviço | Agregados | Banco |
|---|---|---|
| OS | Cliente, Veículo, Peça, Serviço, OrdemDeServico, SagaOS | SQL Server |
| Billing | Orcamento, Pagamento, ItemOrcamentoSnapshot | PostgreSQL |
| Execução | ExecucaoOS, EventoExecucao | MongoDB |

### Consequências
- ✅ Cada micro tem ~10-15 entidades — escopo gerenciável.
- ✅ Saga atravessa os 3 micros — demonstra o pattern com riqueza no vídeo.
- ⚠️ Cliente/Veículo são "denormalizados" em snapshot dentro do payload do evento `OSCriada` para que Billing e Execução não precisem consultar o OS Service.

---

## RFC-007 — Mercado Pago como Gateway de Pagamento

| Campo | Valor |
|---|---|
| ID | RFC-007 |
| Status | Aprovado |
| Data | Maio 2026 |
| Autores | Henrique Greco |
| Impacto | Médio — único gateway integrado |

### Contexto
A Fase 4 exige integração com Mercado Pago para o fluxo de pagamento (especificado no PDF do desafio).

### Justificativa da Escolha
O Mercado Pago foi a escolha **mandatória** (PDF da fase explicita). Mesmo se opcional, seria boa escolha:
- SDK oficial .NET (`mercadopago-sdk` 2.8.0) com cobertura completa de Preferences e Payments API
- Sandbox gratuito com Test Access Tokens
- Webhook configurável + ExternalReference para correlação
- Dominante no mercado brasileiro

### Fluxo de Integração
1. Billing recebe `OrcamentoSolicitado` (do OS Service via RabbitMQ)
2. Billing cria `Orcamento` local + chama `PreferenceClient.CreateAsync` no MP
3. MP retorna `init_point` (link) — Billing publica `OrcamentoGerado` com link
4. Cliente paga via link → MP envia POST para `/api/pagamentos/webhook`
5. Billing busca `GET /v1/payments/{id}` para detalhes e publica `PagamentoAprovado`/`PagamentoRecusado`

### Segurança do Webhook
- Em produção: validar `x-signature` header com HMAC-SHA256 da chave secreta MP
- Para escopo acadêmico/sandbox: webhook está aberto (`[AllowAnonymous]`). Documentado como dívida técnica.

### Idempotência
- `ObterPorMpPaymentIdAsync` antes de criar `Pagamento` — MP pode reenviar webhooks
- Sub-cenários de status intermediário (`pending`, `in_process`) apenas registram payload, aguardando notificação final

---

## RFC-008 — RabbitMQ como Broker de Mensageria

| Campo | Valor |
|---|---|
| ID | RFC-008 |
| Status | Aprovado |
| Data | Maio 2026 |
| Autores | Henrique Greco |
| Impacto | Alto — single broker para todos os micros |

### Contexto
Comunicação assíncrona entre microsserviços é requisito da Fase 4. A escolha do broker impacta operação, desenvolvimento local e features (delayed messages, dead-letter, etc).

### Justificativa da Escolha
**RabbitMQ 3.13** (chart Bitnami v14) provisionado via Terraform/Helm no namespace `messaging-ns` do cluster Kind.

- **Maturidade .NET** — MassTransit tem suporte first-class
- **Subida local rápida** — `helm install` no Kind funciona em segundos
- **Management UI** — `kubectl port-forward svc/rabbitmq 15672` dá observabilidade visual das exchanges/queues, útil para demo
- **Delayed messages** via plugin (ADR-006 usa `Schedule.Delay` que requer scheduler)
- **Dead-letter** automático para mensagens que falham N vezes
- Custo zero — open source

### Alternativas Avaliadas
- **Apache Kafka** — robusto para event sourcing/streaming, mas peso operacional alto para 3 micros e fluxo síncrono de Saga.
- **AWS SQS + SNS** — bom em ambiente AWS, mas acopla a um cloud provider (toda a Fase 3 já está fragmentada entre AWS e Azure, evita-se mais lock-in).
- **Azure Service Bus** — similar ao SQS, sessions sequenciais úteis para Saga, mas Kind local não integra trivialmente.
- **NATS** — leve e moderno, mas ecossistema .NET menos maduro.

### Configuração
- Helm Chart Bitnami 14.6.6
- Auth via secret K8s (`TF_VAR_rabbitmq_password` ou `terraform.tfvars` local)
- Métricas Prometheus habilitadas (porta 9419)
- Persistência via PVC 1Gi (mesma escala dos outros DBs)

### Consequências
- ✅ Operação simples — único broker, único deploy via Terraform
- ✅ Management UI permite ao avaliador inspecionar exchanges/queues durante a demo
- ⚠️ Em produção real, RabbitMQ exige cluster com pelo menos 3 nós para HA — fora do escopo acadêmico

---

## RFC-009 — Quality Gate: SonarCloud + Reqnroll BDD + 80% Coverage

| Campo | Valor |
|---|---|
| ID | RFC-009 |
| Status | Aprovado |
| Data | Maio 2026 |
| Autores | Henrique Greco |
| Impacto | Médio — bloqueia merge se falhar |

### Contexto
A Fase 4 exige cobertura mínima de 80% por serviço, validação de qualidade via SonarQube ou similar, e pelo menos um fluxo testado em BDD. A escolha da stack de testes impacta a velocidade de iteração e a coragem para refatorar.

### Justificativa da Escolha
- **xUnit + FluentAssertions + Moq** (manter padrão da Fase 3 para testes unitários)
- **Reqnroll 2.1** (sucessor open-source do SpecFlow, criado após mudança de licença em 2024) para BDD — sintaxe Gherkin, integração nativa com xUnit
- **dotnet-coverage** + **SonarCloud** (cloud, free tier para projetos públicos/privados acadêmicos) — quality gate bloqueia merge

### Pipeline CI/CD (por micro)
```
checkout → setup-dotnet → setup-java(17) → install dotnet-sonarscanner + dotnet-coverage
→ sonarscanner begin
→ dotnet build
→ dotnet test (Domain + Application)
→ dotnet-coverage collect (Solution-wide)
→ sonarscanner end
→ docker build + push GHCR
→ deploy K8s (gated em produção)
```

### Cenários BDD (por micro)
- **OS Service:** `criar OS para cliente existente`, `aprovar orçamento publica OSAprovadaPeloCliente`
- **Billing Service:** `receber OrcamentoSolicitado cria orçamento e publica OrcamentoGerado`
- **Execução Service:** `receber OSCriada adiciona OS na fila`, `propor orçamento publica OrcamentoPropostoPelaOficina`

### Quality Gate
- Cobertura ≥ 80% por solução
- Zero issues de **Bug** ou **Vulnerability** Severity≥Major
- ≤ 5% code duplication
- Code smells: ≤ A rating

### Alternativas Avaliadas
- **SonarQube self-hosted** — exigiria um pod adicional no Kind, banco PostgreSQL extra, e tempo de operação. SonarCloud (free para uso acadêmico) elimina essa dor.
- **SpecFlow** (versão original) — licença mudou em 2024 para comercial paga acima de uso pessoal. Reqnroll é o fork open-source.
- **Pact** para contract testing — útil mas overkill para 3 micros internos com namespace compartilhado.
- **Coverlet apenas** sem SonarCloud — atende requisito de cobertura mas não cobre "validação de qualidade do código" do PDF.
