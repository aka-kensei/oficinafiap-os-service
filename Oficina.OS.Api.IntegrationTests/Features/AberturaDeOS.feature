#language: pt-BR
Funcionalidade: Abertura de Ordem de Serviço
  Como atendente da oficina
  Quero abrir uma OS para um cliente existente
  Para que a Saga distribuída seja iniciada e o veículo entre na fila de execução

  Cenário: Abrir OS para cliente e veículo cadastrados publica OSCriada
    Dado que existe um cliente cadastrado com CPF "52998224725"
    E que o cliente possui um veículo de placa "ABC1234"
    E que existe um serviço "Troca de óleo" no catálogo com preço 150
    Quando uma OS é aberta para esse cliente com o serviço de "Troca de óleo"
    Então a OS é persistida com status "Recebida"
    E o evento "OSCriada" é publicado no barramento com o CPF e a placa corretos
    E o orçamento da OS deve ser 150

  Cenário: Aprovar OS publica OSAprovadaPeloCliente
    Dado que existe uma OS no status "AguardandoAprovacao"
    Quando o cliente aprova a OS
    Então o evento "OSAprovadaPeloCliente" é publicado no barramento

  Cenário: Reprovar OS publica OSReprovadaPeloCliente com motivo
    Dado que existe uma OS no status "AguardandoAprovacao"
    Quando o cliente reprova a OS com o motivo "Preço acima do esperado"
    Então o evento "OSReprovadaPeloCliente" é publicado com motivo "Preço acima do esperado"
