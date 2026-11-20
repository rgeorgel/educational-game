# Expedição Numérica

Jogo educacional de **matemática e lógica** para crianças e adolescentes, com progressão por níveis
(Iniciante → Médio → Avançado → Expert). O plano completo está em [docs/PLANO.md](docs/PLANO.md).

> "Expedição Numérica" é um nome provisório.

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

Acesse http://localhost:8080 (ou a porta de `WEB_PORT`).

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
- Depois de alterar domínio ou variáveis, clique em **Redeploy** (enquanto aparecer "Changes pending", a mudança
  ainda não foi aplicada e o proxy responde 503).

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
frontend/                      HTML, CSS e JS servidos pelo nginx
nginx/                         configuração do proxy reverso
docs/                          plano e documentação
```
