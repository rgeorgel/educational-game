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
- [ ] Fase 2 — Níveis Médio e Avançado, teste de nivelamento, Desafio do Guardião, medalhas.

## Stack

| Camada    | Tecnologia                                            |
|-----------|-------------------------------------------------------|
| Frontend  | HTML, CSS e JavaScript puro (ES Modules, sem frameworks) servidos pelo nginx |
| Backend   | .NET 10 (ASP.NET Core Minimal APIs, EF Core)          |
| Banco     | PostgreSQL 17                                         |
| Execução  | Docker Compose                                        |

## Como rodar

Pré-requisito: Docker com Docker Compose.

```bash
cp .env.example .env
docker compose up -d --build
```

Acesse http://localhost:8080, crie uma conta de responsável, crie um perfil e escolha uma trilha.

- A API fica atrás do nginx em `/api` (ex.: http://localhost:8080/api/health).
- Documento OpenAPI: http://localhost:8080/api/openapi/v1.json
- O Postgres fica exposto apenas em `127.0.0.1:5433` (configurável via `POSTGRES_PORT`).
- O frontend é montado como volume: edite HTML/CSS/JS e recarregue a página.

Para parar: `docker compose down` (adicione `-v` para apagar o banco).

## Desenvolvimento do backend

Pré-requisito: [.NET SDK 10](https://dotnet.microsoft.com/download).

```bash
docker compose up -d db          # sobe só o banco
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
- **Progresso:** a barra de nível mostra o avanço das habilidades do nível. Uma habilidade conta como dominada com rating ≥ 1000,
  e o jogador fica pronto para o próximo nível com 80% das habilidades dominadas. O Desafio do Guardião entra na Fase 2.

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
