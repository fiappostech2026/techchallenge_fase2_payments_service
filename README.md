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

| Direção | Evento | Exchange no RabbitMQ | Fila (queue) |
|---|---|---|---|
| Recebe (consome) | `OrderPlacedEvent` | `order-placed-event` | `payments-order-placed-event` |
| Envia (publica) | `PaymentProcessedEvent` | `payment-processed-event` | (consumido por `catalog-payment-processed-event` e `notifications-payment-processed-event`) |

O nome da fila é prefixado com o nome do serviço (`payments-`, via `SetEndpointNameFormatter`)
para que dois serviços independentes que consomem o mesmo tipo de evento não acabem
compartilhando uma única fila física — se isso acontecesse, as mensagens seriam divididas entre
eles em vez de cada serviço receber todas.

A biblioteca usada para falar com o RabbitMQ é o **MassTransit**, um "tradutor" que evita ter
que lidar com os detalhes técnicos do protocolo do RabbitMQ na mão. A serialização das mensagens
usa `UseRawJsonSerializer(RawSerializerOptions.All, isDefault: true)` em vez do envelope padrão do
MassTransit, porque cada serviço define sua própria cópia local de cada evento (em seu próprio
namespace), então a correspondência é feita pela estrutura do JSON, não pelo nome técnico da classe.

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
├── azure-pipelines.yml             # pipeline de CI/CD no Azure DevOps (ver seção 7)
├── .dockerignore                   # o que NÃO entra na imagem Docker (bin/, obj/, k8s/...)
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
| `RabbitMQ__Port` | Porta do RabbitMQ (vazio = porta padrão) | vazio |
| `RabbitMQ__UseSsl` | `true` para conexão segura (TLS), exigida por serviços como o CloudAMQP | `false` |
| `RabbitMQ__Username` | Usuário de acesso ao RabbitMQ | `guest` (se não informado) |
| `RabbitMQ__Password` | Senha de acesso ao RabbitMQ | `guest` (se não informado) |

> No Docker/Kubernetes, essas variáveis são escritas com **duplo underscore**
> (`RABBITMQ__HOST`), que é a forma como o .NET entende "seção : chave" vindo de variáveis de
> ambiente do sistema operacional.

> **Nenhuma senha fica no código.** O `appsettings.json` só guarda o endereço e o vhost locais
> (`localhost` e `/`). Usuário e senha sempre chegam por variável de ambiente; se nenhum for
> informado, o serviço usa `guest`/`guest`, o padrão de um RabbitMQ local de desenvolvimento.

---

## 6. Deploy no Kubernetes

Os manifestos estão na pasta `/k8s` deste repositório:

| Arquivo | O que faz |
|---|---|
| `deployment.yaml` | Sobe o container deste serviço, reiniciando sozinho se cair. Lê todas as configurações do RabbitMQ do Secret `payments-secret` (`envFrom`). Faz atualização gradual (*RollingUpdate* com `maxSurge: 1` e `maxUnavailable: 0`: o Pod novo sobe antes do antigo sair), só é considerado pronto quando `/healthz` (porta 8080) responde (*readinessProbe*) e tem limites de CPU/memória definidos |
| `service.yaml` | Dá o nome de rede `payments-api` para outros serviços acharem este |
| `secret.yaml.example` | **Modelo** do Secret `payments-secret` (host, vhost, porta, SSL, usuário e senha do RabbitMQ), sem nenhum valor real |

> **Por que não existe `secret.yaml` no Git?** Senhas não devem ficar no repositório. O arquivo
> `k8s/secret.yaml` está no `.gitignore`: cada pessoa cria o seu localmente a partir do modelo.
> Em produção (AKS), quem cria o Secret é a pipeline (ver seção 7).

Para aplicar localmente (ex: Docker Desktop ou minikube):

```bash
cp k8s/secret.yaml.example k8s/secret.yaml   # depois edite e preencha os valores reais
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/service.yaml -f k8s/deployment.yaml
```

> Localmente, o `deployment.yaml` usa a imagem `fcg-payments-api:latest` com
> `imagePullPolicy: IfNotPresent` — o cluster usa a imagem já carregada localmente (ex: via
> `minikube image load`) em vez de tentar baixar de um registry. Na pipeline, essa imagem é
> trocada automaticamente pela versão publicada no ACR.

> **Pré-requisito:** o RabbitMQ precisa já estar rodando no cluster com o nome de Service
> `rabbitmq` — ele é definido no repositório
> [`FCG.Orchestration`](../FCG.Orchestration).

---

## 7. CI/CD com Azure DevOps

O arquivo `azure-pipelines.yml` automatiza o caminho "código → produção". Toda vez que algo
entra na branch `main`, a pipeline roda três etapas, em ordem:

| Etapa | O que faz |
|---|---|
| **1. Build & Test** | Compila a solução e roda os testes automatizados, publicando o resultado e a cobertura de código. Também roda em **Pull Requests**, para validar antes do merge (nos PRs, a pipeline para aqui) |
| **2. Imagem** | Gera a imagem Docker, faz um *scan* de vulnerabilidades com o **Trivy** (por enquanto só reporta, não bloqueia) e envia a imagem para o Azure Container Registry como `<acr>.azurecr.io/fcg-payments-api:<BuildId>` — cada execução tem uma tag única |
| **3. Deploy no AKS** | Cria/atualiza o Secret `payments-secret` no cluster, aplica `service.yaml` e `deployment.yaml` já com a imagem nova e espera o rollout terminar. Se os Pods novos não ficarem prontos, faz **rollback automático** para a versão anterior |

**Nenhuma credencial fica no Git.** Os valores do Secret vêm do *Variable Group*
`fcg-shared-secrets` do Azure DevOps (em *Pipelines > Library*), com as variáveis
`rabbitmq-host`, `rabbitmq-vhost`, `rabbitmq-port`, `rabbitmq-use-ssl`, `rabbitmq-user` e
`rabbitmq-password`.

> **Segurança da imagem:** o container roda com um usuário **sem privilégios de root** (o
> usuário padrão `app` da imagem oficial do .NET, dono só da pasta `logs/`), e o
> `.dockerignore` evita que arquivos desnecessários (como `bin/`, `obj/` e a pasta `k8s/`)
> entrem na imagem.

---

## 8. Testes

```bash
dotnet test FCG.Payments.Tests
```

A cobertura de código é coletada com o `coverlet.collector` (na pipeline, via
`--collect "XPlat Code Coverage"`).

## 9. Limitações conhecidas

- A aprovação/rejeição do pagamento é **simulada por sorteio aleatório** — não há integração
  com gateway de pagamento real.
- Eventos que falham na validação são descartados com um log de aviso — não há retry automático
  nem fila de mensagens mortas (*dead-letter queue*) configurada.
