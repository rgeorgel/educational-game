# Expedição Numérica

Jogo educacional de **matemática e lógica** para crianças e adolescentes, com progressão por níveis
(Iniciante → Médio → Avançado → Expert). O plano completo está em [docs/PLANO.md](docs/PLANO.md).

> "Expedição Numérica" é um nome provisório.

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

Acesse http://localhost:8080.

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

## Estrutura

```
backend/
  src/EduGame.Api/             endpoints HTTP, configuração
  src/EduGame.Domain/          entidades e regras (rating, níveis, XP) — sem dependências
  src/EduGame.Problems/        geradores e validadores de problemas
  src/EduGame.Infrastructure/  EF Core, migrations, seed de conteúdo
  tests/                       testes unitários e de integração
frontend/                      HTML, CSS e JS servidos pelo nginx
nginx/                         configuração do proxy reverso
docs/                          plano e documentação
```
