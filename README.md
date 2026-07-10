# FCG.Payments — Microsserviço de Pagamentos

Este repositório contém o **microsserviço de Pagamentos** do sistema FIAP Cloud Games (FCG).
Ele é uma das quatro peças independentes que formam o sistema — para entender como ele se
conecta com as outras, veja o repositório
[`FCG.Orchestration`](../FCG.Orchestration),
que explica o projeto como um todo em linguagem simples.

Este README explica só a parte deste serviço, também em linguagem simples, assumindo que quem
lê não tem experiência técnica.

---

## 1. O que este serviço faz

Quando alguém compra um jogo na plataforma, este serviço é o responsável por **processar
(simular) o pagamento** dessa compra.

Ele não tem uma tela, nem um endereço de internet que você acessa direto pelo navegador. Ele
funciona nos bastidores: fica esperando avisos (chamados de **eventos**) de que uma compra foi
feita, processa o pagamento sozinho, e avisa o resto do sistema do resultado. Esse tipo de
programa, que roda em segundo plano sem interface, é chamado de **Worker Service**.

### O fluxo, passo a passo

1. O microsserviço de Catálogo avisa: "um usuário quer comprar o jogo X" — esse aviso se chama
   `OrderPlacedEvent` e contém o ID do pedido, do usuário, do jogo e o preço.
2. Este serviço **recebe** esse aviso (dizemos que ele "consome o evento").
3. Ele confere se os dados do aviso são válidos (ex: nenhum campo vazio ou inválido).
4. Ele decide se o pagamento foi aprovado ou não. **Nesta versão, essa decisão é simulada por
   sorteio** (50% de chance para cada lado) — não existe integração com um banco ou gateway de
   pagamento real.
5. Ele publica um novo aviso, `PaymentProcessedEvent`, contando o resultado (`Approved` ou
   `Rejected`), para quem quiser saber (Catálogo e Notificações).

```
OrderPlacedEvent  ──►  [validação]  ──►  [sorteio: aprova ou rejeita]  ──►  PaymentProcessedEvent
```

---

## 2. Como as peças se conectam (RabbitMQ)

Este serviço não fala diretamente com nenhum outro. Ele troca mensagens através do
**RabbitMQ**, um "correio" compartilhado entre todos os microsserviços (veja a explicação
completa no README do repositório de orquestração).

| Direção | Evento | Nome da fila/exchange no RabbitMQ |
|---|---|---|
| Recebe (consome) | `OrderPlacedEvent` | `order-placed-event` |
| Envia (publica) | `PaymentProcessedEvent` | `payment-processed-event` |

A biblioteca usada para falar com o RabbitMQ é o **MassTransit**, um "tradutor" que evita ter
que lidar com os detalhes técnicos do protocolo do RabbitMQ na mão.

---

## 3. Estrutura de pastas

```
FCG.Payments/
├── FCG.Payments.Domain/          # regras de negócio (não sabe nada sobre RabbitMQ/Web)
│   ├── Dto/                       # formato dos eventos (OrderPlacedEvent, PaymentProcessedEvent)
│   ├── Enums/                     # PaymentStatus (Approved, Rejected)
│   ├── Interfaces/IService/       # contratos dos serviços
│   ├── Services/                  # PaymentService — decide aprovar/rejeitar
│   └── Validators/                # validação dos eventos recebidos (FluentValidation)
├── FCG.Payments.Worker/          # ponto de entrada do programa (roda em background)
│   ├── Consumers/                 # "ouvintes" dos eventos do RabbitMQ
│   ├── Extensions/                # configuração (RabbitMQ, logs, injeção de dependência)
│   ├── Middleware/                # tratamento de erros
│   └── Program.cs                 # arquivo que liga tudo e inicia o serviço
├── FCG.Payments.Tests/            # testes automatizados
├── k8s/                            # manifestos de deploy no Kubernetes (ver seção 6)
└── Dockerfile                      # receita para empacotar este serviço em um container
```

---

## 4. Como rodar sozinho (sem os outros microsserviços)

Você pode rodar este serviço isolado, desde que tenha um RabbitMQ disponível — útil para testar
sem precisar subir o projeto inteiro.

### Opção A — via .NET direto (para quem tem o SDK instalado)

```bash
dotnet run --project FCG.Payments.Worker
```

### Opção B — via Docker

```bash
docker build -t fcg-payments-api:latest .
docker run --rm -e RABBITMQ__HOST=host.docker.internal fcg-payments-api:latest
```

> Para rodar o sistema completo (com RabbitMQ e o serviço de Notificações juntos, prontos para
> o fluxo de compra funcionar de ponta a ponta), use o `docker-compose.yml` do repositório
> [`FCG.Orchestration`](../FCG.Orchestration) —
> é o caminho recomendado.

---

## 5. Variáveis de ambiente

| Variável | Descrição | Valor padrão (local) |
|---|---|---|
| `RabbitMQ__Host` | Endereço do servidor RabbitMQ | `localhost` |
| `RabbitMQ__VirtualHost` | "Vhost" (espaço isolado) do RabbitMQ a usar | `/` |
| `RabbitMQ__Username` | Usuário de acesso ao RabbitMQ | `guest` |
| `RabbitMQ__Password` | Senha de acesso ao RabbitMQ | `guest` |

> No Docker/Kubernetes, essas variáveis são escritas com **duplo underscore**
> (`RABBITMQ__HOST`), que é a forma como o .NET entende "seção : chave" vindo de variáveis de
> ambiente do sistema operacional.

---

## 6. Deploy no Kubernetes

Os manifestos estão na pasta `/k8s` deste repositório:

| Arquivo | O que faz |
|---|---|
| `deployment.yaml` | Sobe o container deste serviço, reiniciando sozinho se cair |
| `service.yaml` | Dá o nome de rede `payments-api` para outros serviços acharem este |
| `configmap.yaml` | Guarda o endereço e vhost do RabbitMQ (não são segredos) |
| `secret.yaml` | Guarda usuário/senha do RabbitMQ (dado sensível) |

Para aplicar:

```bash
kubectl apply -f k8s/
```

> **Pré-requisito:** o RabbitMQ precisa já estar rodando no cluster com o nome de Service
> `rabbitmq` — ele é definido no repositório
> [`FCG.Orchestration`](../FCG.Orchestration).

---

## 7. Testes

```bash
dotnet test FCG.Payments.Tests
```

## 8. Limitações conhecidas

- A aprovação/rejeição do pagamento é **simulada por sorteio aleatório** — não há integração
  com gateway de pagamento real.
- Eventos que falham na validação são descartados com um log de aviso — não há retry automático
  nem fila de mensagens mortas (*dead-letter queue*) configurada.
