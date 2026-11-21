# Educa Game

Jogo educacional de **matemática e lógica** para crianças e adolescentes, com progressão por níveis
(Iniciante → Médio → Avançado → Expert). O plano completo está em [docs/PLANO.md](docs/PLANO.md).

## Status

- [x] **Fase 0 — Fundação:** Docker Compose, API .NET 10, Postgres, nginx, CI.
- [x] **Fase 1 — MVP jogável:**
  - Conta do responsável (cadastro, login, exclusão da conta) e perfis de jogador.
  - Matemática Iniciante (6 habilidades) e Lógica Iniciante (4 habilidades), com problemas gerados automaticamente.
  - Tipos de desafio: `numero`, `multipla-escolha` e `completar-sequencia`.
  - Sessões com feedback, dicas, explicação e "problema irmão" depois de um erro.
  - Rating por habilidade (estilo Elo), XP e estrelas por etapa; cada etapa desbloqueia a próxima.
  - Tela de progresso com o nível e a barra até o próximo nível.
- [x] **Fase 2 — Progressão completa:**
  - Níveis Médio e Avançado: 12 habilidades de Matemática e 10 de Lógica, em 10 novas regiões.
  - Teste de nivelamento adaptativo (10 perguntas) e Desafio do Guardião (12 problemas, 9 acertos para subir de nível).
  - Novos tipos de desafio: `verdadeiro-falso`, `ordenar`, `grade` (Sudoku 4×4/6×6 e quadrado mágico) e `fracao-visual`.
  - 15 medalhas, sequência de dias (pode faltar 1 dia) e gráfico dos últimos 14 dias.
  - Calibração automática da dificuldade de cada gerador com base nas respostas reais.
- [x] **Fase 3 — Expert e engajamento:**
  - Nível Expert: 5 habilidades de Matemática (sistemas, 2º grau, juros, Pitágoras, contagem) e 4 de Lógica
    (tabelas-verdade, cavaleiros e mentirosos, pensamento computacional e desafios de raciocínio escritos à mão).
  - Desafio do dia (1 problema por tópico, igual para todos do mesmo nível, XP em dobro) e revisão inteligente.
  - Avatar personalizável com acessórios e cores desbloqueados por XP (sem compras).
  - Área do responsável (relatório por perfil) e turmas de professor com código; o responsável escolhe quais perfis entram.
  - PWA instalável com funcionamento offline parcial, leitura em voz alta e configurações de acessibilidade
    (tema, texto grande, animações reduzidas).
- [x] **Alinhamento à BNCC:**
  - Cada habilidade do jogo está ligada às habilidades da BNCC relacionadas: Matemática do 1º ao 9º ano e algumas do Ensino Médio; na Lógica, também o complemento de Computação.
  - O mapeamento fica em `backend/src/EduGame.Infrastructure/Seeding/BnccCatalog.cs`.
  - Página pública "Currículo e BNCC" (`/jogar#/bncc`) e API `GET /api/curriculum`.
  - O relatório da turma mostra os códigos das habilidades que pedem atenção; o painel admin também mostra os códigos.
  - O mapeamento é uma referência e deve ser revisado por professores.
- [x] **Divulgação:**
  - "Compartilhar resultado" no desafio do dia, nas etapas com estrela e no Guardião: quadradinhos 🟩🟨🟥, sem apelido e sem respostas, mais o link.
  - Origem de cada cadastro: `utm_source`/`utm_medium`/`utm_campaign` ou `?ref=` no primeiro acesso; sem isso, o domínio do site de origem.
  - O painel admin mostra "De onde vêm as pessoas", com quantos jogaram e quantos voltaram.
  - Página `/professores`, no sitemap e no `llms.txt`.
- [x] **Para professores (primeira versão paga):**
  - **Tarefas da turma:** o professor pede uma etapa, com estrelas mínimas, prazo e recado.
    - O aluno vê a tarefa na tela inicial e pode jogar a etapa mesmo bloqueada no mapa.
    - O professor vê quem fez, quem tentou e quem não começou.
  - **Relatório da turma por habilidade**, com os códigos da BNCC.
  - **Planilhas (CSV):** de alunos e de habilidades, que abrem no Excel ou no Google Planilhas.
  - **Planos** Grátis, Professor Pro e Escola, com limites que só valem com `PLANS_ENFORCE=true`.
- [x] **Planos Professor Pro e Família** (escolha no site, sem pagamento por enquanto):
  - relatório da semana;
  - treino de reforço escolhido pelo responsável;
  - fichas para imprimir com gabarito;
  - até 8 perfis no Família.
- [x] **Pagamento pelo Stripe:** Checkout, portal do cliente e webhooks. Liga quando as chaves são configuradas.
- [ ] Fase 4 — Novos tópicos.

## Stack

| Camada    | Tecnologia                                            |
|-----------|-------------------------------------------------------|
| Frontend  | HTML, CSS e JavaScript puro (ES Modules, sem frameworks) servidos pelo nginx |
| Backend   | .NET 10 (ASP.NET Core Minimal APIs, EF Core)          |
| Banco     | PostgreSQL 17                                         |
| Execução  | Docker Compose                                        |

## Como rodar

Pré-requisito: Docker com Docker Compose (2.20 ou mais novo).

### Local (banco em container)

```bash
cp .env.example .env
docker compose up -d --build
```

Acesse http://localhost:8080 (ou a porta de `WEB_PORT`): a raiz é a landing page e o jogo fica em `/jogar`.

- O `.env` de exemplo já traz `COMPOSE_PROFILES=local-db`, que sobe o Postgres num container (só precisa de `DB_PASSWORD`).
- A API fica atrás do nginx em `/api` (ex.: http://localhost:8080/api/health).
- Documento OpenAPI: http://localhost:8080/api/openapi/v1.json
- O Postgres local fica exposto apenas em `127.0.0.1:5433` (configurável via `DB_LOCAL_PORT`).
- O frontend é montado como volume pelo `docker-compose.override.yml` (lido automaticamente só no ambiente local):
  edite HTML/CSS/JS e recarregue a página.

Para parar: `docker compose down` (adicione `-v` para apagar o banco local).

### Servidor (banco fora do Docker)

No `.env` do servidor, **não** defina `COMPOSE_PROFILES` e informe os dados do seu Postgres:

```bash
DB_HOST=host.docker.internal   # Postgres no próprio servidor; ou o IP/nome de outra máquina
DB_PORT=5432
DB_NAME=edugame
DB_USER=edugame
DB_PASSWORD=sua-senha
DB_SSL_MODE=Prefer             # opcional: Disable, Prefer, Require, VerifyCA, VerifyFull
WEB_PORT=8080
ASPNETCORE_ENVIRONMENT=Production
```

```bash
docker compose up -d --build
```

- Só sobem os containers `api` e `web`; o container do banco nem é criado.
- A senha pode ter qualquer caractere (inclusive `;` e `=`): a API monta a conexão campo a campo.
- `DB_HOST=host.docker.internal` aponta para um Postgres instalado no próprio servidor (já configurado no compose,
  inclusive em Linux). Esse Postgres precisa aceitar conexões vindas da rede do Docker
  (`listen_addresses` e `pg_hba.conf`).
- As migrations são aplicadas quando a API inicia. Se o usuário do banco não puder criar tabelas, use
  `DB_MIGRATE_ON_STARTUP=false` e aplique as migrations por fora.
- Se algo estiver errado (senha, host, porta), a API não sobe e o motivo aparece em `docker compose logs edugame-api`.
- O container `web` é uma imagem própria (`nginx/Dockerfile`) com o frontend e a configuração do nginx dentro,
  sem volumes: funciona em plataformas que não montam arquivos do repositório.

#### Coolify

- Use o tipo de recurso **Docker Compose** apontando para `docker-compose.yml` (o Coolify roda com `-f`, então o
  `docker-compose.override.yml` de desenvolvimento é ignorado).
- Cadastre as variáveis `DB_*` e `ASPNETCORE_ENVIRONMENT=Production` nas variáveis de ambiente do recurso e
  **não** defina `COMPOSE_PROFILES`.
- Em **Domains**, coloque o domínio no serviço **`web`** (não no `api`). O nginx escuta na porta 80 do container;
  se o Coolify pedir a porta, use `http://seu-dominio:80`.
- **Deploy com o mínimo de parada.** No Coolify, apps em Docker Compose não têm deploy sem parada (rolling update): os containers são parados e recriados. O projeto reduz essa janela assim:
  - **Saúde verificada a cada 1 segundo** enquanto o container sobe (`start_interval`, requer Docker 25 ou mais novo no servidor). O proxy volta a mandar tráfego assim que o serviço responde.
  - **O `web` não espera a API ficar saudável.** O site continua no ar enquanto a API reinicia.
  - **Durante o reinício, o nginx responde 503 com `Retry-After`** e o jogo tenta de novo sozinho, mostrando "Reconectando…". Leituras também são repetidas em caso de erro de rede.
  - **Service worker:** se o servidor responder 502/503/504, ele entrega a cópia guardada das páginas.
  - **Logins sobrevivem ao deploy:** as chaves do jogo ficam no banco e as do painel admin no volume `admin-keys`.
  - Para eliminar a parada de vez, seria preciso separar API e site em dois recursos "Dockerfile" no Coolify, que fazem rolling update.
- Depois de alterar domínio ou variáveis, clique em **Redeploy** (enquanto aparecer "Changes pending", a mudança
  ainda não foi aplicada e o proxy responde 503).

## Planos (famílias, professores e escolas)

Jogar é sempre grátis. Os planos são para os adultos que acompanham. A estratégia completa está em [docs/MONETIZACAO.md](docs/MONETIZACAO.md).

| Plano | Perfis | Turmas | Tarefas por turma | Planilhas | Fichas para imprimir | Relatório da semana e reforço |
|---|---|---|---|---|---|---|
| Grátis | 3 | 1 | 2 | — | — | — |
| Professor Pro | 3 | 20 | 50 | ✓ | ✓ | — |
| Família | 8 | 1 | 2 | — | ✓ | ✓ |
| Escola | 8 | 300 | 50 | ✓ | ✓ | ✓ |

A pessoa escolhe o plano na página **Planos** (`/jogar#/planos`):
- **Sem o Stripe configurado** (modo lançamento): a escolha é grátis e vale na hora.
- **Com o Stripe:** a pessoa assina pelo Checkout (veja "Pagamentos (Stripe)").

Enquanto `PLANS_ENFORCE=false` (padrão), ninguém é limitado e a escolha fica só registrada.
Ao ligar (`PLANS_ENFORCE=true` e Redeploy), cada conta tem só o que os seus planos liberam.

Escola (e qualquer plano, se precisar) é concedido à mão, com um comando no container da API
(no Coolify: **Terminal** do serviço `edugame-api`; localmente: `docker compose exec edugame-api …`):

```bash
dotnet EduGame.Api.dll plano listar
dotnet EduGame.Api.dll plano conceder professora@escola.com professor 365 "Pix 11/2026"
dotnet EduGame.Api.dll plano conceder coordenacao@escola.com escola        # sem data para acabar
dotnet EduGame.Api.dll plano remover professora@escola.com
```

As assinaturas ficam na tabela `subscriptions`. Pelo **painel admin** dá para fazer o mesmo sem terminal
e também dar **testes grátis** (7, 14, 30 dias ou outro prazo), sem a pessoa assinar nada (veja "Planos no painel" abaixo).

### Teste grátis de 7 dias

Com o Stripe ligado, Professor Pro e Família têm **7 dias grátis para testar, sem cartão**:
- na página de planos, cada um mostra "Testar 7 dias grátis" ao lado de "Assinar";
- uma vez por plano e por conta: quem já testou (pelo site ou pelo painel admin) ou já pagou aquele plano não testa de novo;
- o teste acaba sozinho e a conta volta ao que os outros planos liberam; durante o teste aparece "Teste grátis até dd/mm" e o botão "Assinar";
- assinar durante o teste encerra o teste daquele plano;
- no modo lançamento (sem Stripe) não há teste: a escolha do plano já é grátis.

O prazo fica em `Plans.TrialDays` (`backend/src/EduGame.Domain/Billing/Plans.cs`). As linhas ficam em `subscriptions` com `source = teste` e a nota "Teste grátis de 7 dias (site)".

## Pagamentos (Stripe)

Com o Stripe configurado, Professor Pro e Família são **assinados pelo Stripe Checkout**:
- Pagamento: cartão, e outros métodos que o Stripe permitir para assinaturas no Brasil.
- **Gerenciar assinatura** abre o portal do Stripe, para trocar o cartão, baixar recibos ou cancelar.
- Os avisos do Stripe (webhook) criam, atualizam e encerram o plano na tabela `subscriptions`, com `source = stripe`.
- Cancelou? O plano vale até o fim do período pago.
- `past_due` (pagamento atrasado) mantém o plano enquanto o Stripe tenta cobrar de novo.

Sem as variáveis abaixo, nada muda: os planos continuam no modo lançamento (escolha grátis).

**Configurar (use o modo de teste do Stripe primeiro):**

1. **Chave da API.** No Stripe, em *Developers → API keys*, copie a **Secret key** (`sk_test_…`).
2. **Variáveis no Coolify** (ou no `.env`):
   - `STRIPE_SECRET_KEY` = a chave.
   - `PUBLIC_BASE_URL` = endereço do site (ex.: `https://educa-game.seu-dominio`).

   Depois, faça **Redeploy**.
3. **Rode o comando de configuração** no **Terminal** do serviço `edugame-api` no Coolify (localmente: `docker compose exec edugame-api …`):

   ```bash
   dotnet EduGame.Api.dll stripe configurar
   ```

   Ele cria tudo no Stripe. Rodar de novo não duplica nada:
   - os produtos "Professor Pro" e "Família", com o preço mensal de `Plans.cs` (R$ 19,90 e R$ 9,90);
   - o webhook em `PUBLIC_BASE_URL/api/billing/webhook`, com a versão certa da API e os eventos;
   - o portal do cliente: cartão, recibos, cancelar no fim do período e trocar entre os planos.
4. **Copie o segredo do webhook.** O comando mostra o `whsec_…` uma única vez. Coloque em `STRIPE_WEBHOOK_SECRET` e faça **Redeploy**. O log da API passa a mostrar "Pagamentos pelo Stripe ligados".
5. **Teste:**
   - Assine com o cartão de teste `4242 4242 4242 4242` (qualquer data futura e qualquer CVC).
   - O plano aparece como "Assinatura ativa".
   - "Gerenciar assinatura" abre o portal; cancele por ali e veja o plano valer até o fim do período.

Outros comandos:

```bash
dotnet EduGame.Api.dll stripe status                        # o que está configurado
dotnet EduGame.Api.dll stripe preco familia 12,90           # muda o preço; quem já assina mantém o antigo
dotnet EduGame.Api.dll stripe configurar --recriar-webhook  # gera um webhook novo (e um novo whsec_)
```

- **Preços:** o app acha os preços pelo nome fixo (`educagame_professor_mensal` e `educagame_familia_mensal`), então não precisa configurar ids. `STRIPE_PRICE_PROFESSOR` e `STRIPE_PRICE_FAMILIA` são opcionais e só forçam um preço específico.
- **Teste local:** com a [Stripe CLI](https://docs.stripe.com/stripe-cli), `stripe listen --forward-to localhost:8880/api/billing/webhook` mostra um `whsec_…` para usar localmente.

**Indo para produção:**
- Troque `STRIPE_SECRET_KEY` pela chave **live** (`sk_live_…`) e rode `stripe configurar` de novo. Ele cria os produtos, preços e webhook do modo live e mostra o novo `whsec_…`.
- Ligue `PLANS_ENFORCE=true`.
- Dê um prazo para quem escolheu plano no lançamento: `dotnet EduGame.Api.dll plano encerrar-escolhas 30`. As escolhas grátis acabam em 30 dias, e essas pessoas veem o botão "Assinar".

Veja também [docs/MONETIZACAO.md](docs/MONETIZACAO.md) (CNPJ, nota fiscal, termos).

## E-mails e recuperação de senha

O jogo envia três e-mails, sempre para o adulto da conta:
- **Boas-vindas**, ao criar conta ou salvar o progresso de um convidado;
- **Esqueci minha senha**: link para criar uma senha nova. Vale por 2 horas e funciona uma vez só;
- **Senha alterada**, logo depois da troca.

Como funciona:
- Na tela de entrar, "Esqueci minha senha" leva a `/jogar#/esqueci`.
- A resposta é sempre a mesma, exista ou não conta com aquele e-mail. Assim ninguém descobre quem tem conta.
- No máximo um e-mail de senha a cada 2 minutos por conta.
- Os e-mails saem de uma fila em segundo plano, com novas tentativas se o provedor falhar. O site não fica esperando o SMTP.
- Ao criar a senha nova pelo link, a pessoa já entra, a conta é destravada e os outros aparelhos são desconectados em até 30 minutos.

**Configurar** (no `.env` ou nas variáveis do Coolify, depois **Redeploy**):

| Variável | Exemplo |
| --- | --- |
| `PUBLIC_BASE_URL` | `https://educa-game.seu-dominio` (obrigatório em produção: os links dos e-mails usam ele) |
| `SMTP_HOST` / `SMTP_PORT` | `smtp.resend.com` / `587` (465 = SSL) |
| `SMTP_USER` / `SMTP_PASSWORD` | usuário e senha (ou chave de API) do provedor |
| `EMAIL_FROM` | `Educa Game <nao-responda@seu-dominio>` |
| `EMAIL_REPLY_TO` | opcional, ex.: o e-mail de suporte |

Qualquer provedor SMTP serve. Para os e-mails não caírem no spam, verifique o domínio do remetente no provedor
(registros SPF, DKIM e DMARC no DNS).

Sem SMTP:
- em desenvolvimento, o e-mail completo aparece no log da API (`docker compose logs edugame-api`), com o link clicável;
- em produção, "Esqueci minha senha" avisa que está indisponível e o log da API mostra o que falta configurar.

## Termos de uso e privacidade

- `/termos` e `/privacidade` (arquivos `frontend/termos.html` e `frontend/privacidade.html`).
- Aparecem no cadastro, em "Salvar progresso", em Configurações e no rodapé das páginas.
- O responsável (nome e CPF) e o e-mail de contato aparecem só nessas duas páginas. O rodapé das páginas públicas tem apenas o link para o portfólio (georgel.ca).
- Os textos foram escritos para este jogo (LGPD, dados de crianças, CDC, Stripe), mas não são orientação jurídica: vale a revisão de um advogado antes de ligar a cobrança.

## Painel de super admin

Um painel para acompanhar o uso do jogo e gerenciar os planos das pessoas. Ele roda como um **serviço separado** (`edugame-admin`, pasta `admin/`):
- não usa o código do jogo nem a API do jogo;
- tem login próprio;
- só **lê** o banco, com uma exceção: a tabela `subscriptions` (planos).

Toda conexão do painel abre em modo somente leitura e cada consulta tem limite de 20 segundos. O `/health` do painel confirma esse modo.
Só as ações de plano abrem, de propósito, uma transação de leitura e escrita, e elas só mexem em `subscriptions`.

O que mostra:

- **Visão geral:**
  - contas, convidados e perfis;
  - pessoas ativas hoje, em 7 e em 30 dias;
  - acessos;
  - perguntas respondidas hoje e em 7 dias, comparadas com a semana anterior;
  - % de acertos e tempo médio por resposta;
  - convidados que salvaram a conta e partidas concluídas.
- **Gráficos da visão geral:**
  - perguntas respondidas por dia (14, 30 ou 90 dias) e por semana (12 semanas);
  - pessoas ativas por dia;
  - novos cadastros (contas × convidados);
  - respostas por horário do dia.
- **Usuários:** busca por e-mail ou apelido, filtro conta/convidado e filtro de plano (com plano, em teste, pagantes, sem plano). A coluna **Plano** mostra os planos ativos e até quando valem. Ao clicar em alguém, abre o detalhe: planos, perfis, níveis, últimos 30 dias e partidas recentes.
- **Ranking:** os 50 primeiros por XP, respostas, % de acertos (mínimo de 20 respostas) ou dias jogando. Períodos: sempre, 7, 30 ou 90 dias, com ou sem convidados.
- **Conteúdo:**
  - habilidades mais difíceis e mais praticadas;
  - etapas mais jogadas;
  - modos de jogo;
  - jogadores por nível;
  - medalhas.

### Planos no painel

No detalhe de uma conta, o cartão **Planos** mostra os planos ativos (e o histórico) e permite:
- **Teste grátis:** Professor Pro, Família ou Escola por 7, 14, 30 dias (ou outro prazo). A pessoa não assina nada; no fim do prazo o plano acaba sozinho. Na página de planos do jogo ela vê "Teste grátis até dd/mm" e, com o Stripe ligado, o botão "Assinar". Se assinar, o teste daquele plano é encerrado.
- **Concessão:** o mesmo, para cortesias, Pix ou escolas, com prazo ou sem data para acabar.
- **+7 dias / +30 dias** e **Encerrar agora** em testes, concessões e escolhas do lançamento.

Regras:
- Dar um plano que a conta já tem em teste ou concessão troca o anterior pelo novo (não duplica).
- Assinaturas **pagas pelo Stripe** só aparecem; para cancelar ou mudar, use o painel do Stripe.
- Convidados não recebem plano: a pessoa precisa salvar o progresso antes.
- Tudo fica registrado em `subscriptions` (`source = teste` ou `manual`, nota com o usuário do admin) e no log do painel.
- Lembre que, com `PLANS_ENFORCE=false`, todos já têm tudo liberado: os testes passam a fazer diferença quando a cobrança for ligada.

Os dias são contados no horário de Brasília (`Admin__TimeZone` muda isso). Os **acessos** vêm da tabela `daily_visits`, que o jogo preenche:
- conta 1 acesso cada vez que alguém abre o jogo, entra ou cria conta;
- só existem dados a partir da versão que criou essa tabela.

Convidados apagados por inatividade somem também das estatísticas.

### Como ativar

1. No `.env`, defina `ADMIN_USERNAME` e `ADMIN_PASSWORD`. A senha precisa ter 12 ou mais caracteres. Sem eles, o painel mostra como configurar e não deixa entrar.
2. Rode `docker compose up -d --build edugame-admin` e acesse http://localhost:8081 (a porta é `ADMIN_PORT`).

Proteções do login:
- usuário e senha são comparados em tempo constante;
- no máximo 8 tentativas a cada 5 minutos por IP;
- cookie próprio (`edugame.admin`, HttpOnly, SameSite Strict) que expira em 8 horas.

Por padrão a porta só abre em `127.0.0.1` (`ADMIN_BIND`).

**Recomendado no servidor:** um usuário do Postgres que só pode ler, informado em `ADMIN_DB_USER` e `ADMIN_DB_PASSWORD`:

```sql
CREATE ROLE edugame_leitura LOGIN PASSWORD 'troque-esta-senha';
GRANT CONNECT ON DATABASE edugame TO edugame_leitura;
GRANT USAGE ON SCHEMA public TO edugame_leitura;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO edugame_leitura;
ALTER DEFAULT PRIVILEGES FOR ROLE edugame IN SCHEMA public GRANT SELECT ON TABLES TO edugame_leitura;
-- Para gerenciar planos pelo painel (única escrita do painel):
GRANT INSERT, UPDATE ON subscriptions TO edugame_leitura;
```

Sem esse último `GRANT`, o painel continua funcionando, mas as ações de plano mostram um aviso pedindo a permissão.

No **Coolify**:
1. Cadastre `ADMIN_USERNAME` e `ADMIN_PASSWORD` (e, se quiser, `ADMIN_DB_USER` e `ADMIN_DB_PASSWORD`) nas variáveis do recurso.
2. Em **Domains**, coloque um **domínio separado** no serviço **`edugame-admin`**, porta `8080`. Exemplo: `http://admin.seu-dominio:8080`, que o Coolify publica com HTTPS.
3. Clique em **Redeploy**.

O painel não aparece em buscadores: envia `noindex`.

Para desenvolver sem Docker:

```bash
cd admin
Database__Host=127.0.0.1 Database__Port=5433 Database__Name=edugame Database__Username=edugame \
Database__Password=... Admin__Username=admin Admin__Password=uma-senha-longa \
dotnet run --project src/EduGame.Admin
```

## Desenvolvimento do backend

Pré-requisito: [.NET SDK 10](https://dotnet.microsoft.com/download).

```bash
docker compose up -d db          # sobe só o banco (precisa de COMPOSE_PROFILES=local-db no .env)
cd backend
dotnet run --project src/EduGame.Api --environment Development
dotnet test                      # testes de integração usam Testcontainers (precisa de Docker)
```

As migrations são aplicadas automaticamente quando a API inicia. Para criar uma nova:

```bash
cd backend
dotnet ef migrations add NomeDaMigration -p src/EduGame.Infrastructure -s src/EduGame.Api -o Migrations
```

## Como o jogo funciona (resumo técnico)

- **Conteúdo:** o currículo (tópicos, níveis, habilidades, regiões e etapas) fica em
  `backend/src/EduGame.Infrastructure/Seeding/ContentCatalog.cs` e é sincronizado com o banco toda vez que a API sobe.
- **Problemas:** cada habilidade tem um gerador em `backend/src/EduGame.Problems`. O servidor gera vários candidatos
  e escolhe o de dificuldade mais próxima do rating do jogador (70% no nível dele, 20% abaixo, 10% acima).
- **Respostas:** a resposta nunca vai para o navegador. O cliente envia a resposta e o servidor valida, atualiza
  rating e XP e devolve a explicação.
- **Progresso:** a barra do nível é a média do avanço de cada habilidade rumo ao domínio (cada uma vale no máximo 100%).
  Uma habilidade está dominada quando o rating chega ao início do próximo nível (1000 → Médio, 1500 → Avançado, 2000 → Expert).
- **Desafio do Guardião:** abre quando a barra do nível chega a 85% (`LevelProgress.GuardianThreshold`).
  A prova tem 12 problemas de todas as habilidades do nível.
  Acertar 9 ou mais promove o jogador; nunca se perde nível.
- **Nivelamento:** antes de começar um tópico, o jogador pode fazer um teste adaptativo de 10 perguntas (sem dicas).
  A estimativa começa pela série escolar, sobe a cada acerto e desce a cada erro, com passos cada vez menores.
- **Calibração:** um serviço em segundo plano (`Calibration:IntervalMinutes`, padrão 60) compara o resultado esperado
  com o real de cada gerador nos últimos 30 dias (mínimo de 50 respostas) e ajusta a dificuldade aos poucos (±50 por rodada, até ±300).
- **Jogar sem conta (convidado):** na tela inicial basta um apelido. É criada uma conta de convidado presa ao aparelho
  por um cookie de 1 ano (renovado a cada acesso). "Salvar progresso" transforma essa conta numa conta com e-mail e
  senha, mantendo tudo. Convidados não criam nem entram em turmas, e convidados sem acesso há mais de
  `Guests:RetentionDays` (padrão 180) dias são apagados automaticamente.
- **Fuso horário:** a sequência de dias e o desafio do dia usam `Game:TimeZone` (padrão `America/Sao_Paulo`).
- **Problemas curados:** além dos geradores, há problemas escritos à mão em
  `backend/src/EduGame.Problems/Curated/*.json` (validados por teste e copiados para o banco ao subir a API).
- **Turmas e privacidade:** o professor vê apenas apelido, avatar e progresso dos perfis que os responsáveis
  colocaram na turma com o código — nunca e-mail. O responsável pode tirar o perfil a qualquer momento.
- **Offline:** o service worker (`frontend/sw.js`) usa "rede primeiro": com internet tudo vem do servidor;
  sem internet, abre o app e os últimos dados vistos. Responder problemas exige conexão. Ao sair da conta,
  o cache de dados é apagado.

## Estrutura

```
backend/
  src/EduGame.Api/             endpoints HTTP, configuração
  src/EduGame.Domain/          entidades e regras (rating, níveis, XP), sem infraestrutura
  src/EduGame.Problems/        geradores e validadores de problemas (C# puro)
  src/EduGame.Infrastructure/  EF Core, migrations, seed de conteúdo
  tests/                       testes unitários e de integração
frontend/                      HTML, CSS e JS servidos pelo nginx (index.html = landing, jogar.html = jogo)
admin/                         painel de super admin: serviço .NET separado, lê o banco e só escreve planos (wwwroot = interface)
nginx/                         configuração do proxy reverso
docs/                          plano e documentação
```
