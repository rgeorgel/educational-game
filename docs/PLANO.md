# Plano do Jogo Educacional de Matemática e Lógica

> Documento de planejamento — versão 0.1 (19/11/2026)
> Status: proposta inicial, aberta a decisões (ver seção 16).

---

## 1. Visão geral

Um jogo web para crianças e adolescentes (aprox. 7 a 17 anos) praticarem **matemática** e **lógica** de forma progressiva. O jogador começa em problemas simples e, conforme demonstra domínio, desbloqueia desafios mais difíceis e complexos. O progresso é sempre visível e organizado em quatro níveis:

| Nível        | Ícone sugerido | Ideia central                                     |
|--------------|----------------|---------------------------------------------------|
| **Iniciante**| 🌱 Semente     | Fundamentos, números pequenos, padrões visuais    |
| **Médio**    | 🌿 Broto       | Operações combinadas, frações, raciocínio em etapas|
| **Avançado** | 🌳 Árvore      | Álgebra, proporção, lógica formal simples         |
| **Expert**   | 🏔️ Montanha    | Problemas abertos, multi-etapas, desafios olímpicos|

### Objetivos

1. **Aprender fazendo**: muitos problemas curtos com feedback imediato.
2. **Progressão justa**: a dificuldade acompanha o desempenho real do jogador, não a idade.
3. **Motivação saudável**: recompensar esforço e consistência, sem punir erros.
4. **Extensível**: a arquitetura já nasce preparada para novos tópicos (português, ciências, inglês…), mesmo que agora só existam Matemática e Lógica.

### Fora do escopo (por enquanto)

- Outros tópicos além de Matemática e Lógica.
- Multiplayer em tempo real.
- Aplicativo nativo (o jogo será web responsivo; pode virar PWA depois).

---

## 2. Público e premissas

- **Faixa ampla de idade** → o nível é definido pelo desempenho. Na criação do perfil, o jogador (ou responsável) informa a série/ano escolar apenas para escolher o **ponto de partida** (um teste de nivelamento curto ajusta isso).
- **Uso em casa e na escola** → considerar desde o início o papel de **responsável/professor** acompanhando o progresso (pode entrar numa fase posterior).
- **Dispositivos**: celular, tablet e computador. Interface pensada primeiro para toque.
- **Idioma**: português do Brasil (deixar textos em arquivos de tradução para facilitar outros idiomas depois).

---

## 3. Conceito do jogo

### Tema sugerido: "Expedição"

O jogador é um explorador que atravessa um mapa com regiões. Cada **região** corresponde a uma **trilha de habilidade** (ex.: "Vale da Adição", "Floresta das Frações", "Torre da Lógica"). Cada região tem **etapas** (fases), e no fim de cada nível há um **Desafio do Guardião** (prova de nível).

Nomes alternativos para o jogo: *Trilha Lógica*, *Mente em Jogo*, *Expedição Numérica*, *Números & Enigmas*.

> O tema é só uma "casca" visual. Se preferir algo mais neutro para adolescentes (ex.: estética de "laboratório" ou "missões espaciais"), a estrutura continua a mesma.

### Loop principal de jogo

```
Escolher região/etapa → Sessão de 5 a 10 problemas → Feedback a cada resposta
      ↑                                                        ↓
Desbloquear novas etapas ← Ganhar XP / estrelas ← Resumo da sessão (acertos, tempo, dicas)
```

- **Sessões curtas** (3–5 minutos): ideal para crianças e para criar hábito.
- **Feedback imediato**: certo/errado + explicação curta do raciocínio.
- **Dicas progressivas**: 1ª dica = orientação geral; 2ª = passo intermediário; 3ª = mostra o caminho. Usar dicas reduz o XP ganho, mas nunca bloqueia.
- **Erro vira aprendizado**: depois de errar, aparece um problema "irmão" (mesmo tipo, números diferentes) para tentar de novo.

---

## 4. Sistema de progressão

A progressão funciona em **três camadas**, do mais fino ao mais visível:

### 4.1 Habilidades (camada interna)

Cada problema pertence a uma **habilidade** (ex.: `soma-ate-100`, `fracoes-equivalentes`, `sequencias-numericas`). Para cada jogador e habilidade guardamos uma **pontuação de domínio** (rating), inspirada no sistema Elo:

- Cada problema tem uma **dificuldade** (também um número).
- Acertou um problema difícil para você → rating sobe bastante. Errou um fácil → cai um pouco.
- A atualização considera também uso de dicas e tempo (com peso leve — velocidade não deve ser o foco para crianças pequenas).

Fórmula de partida (simples e ajustável):

```
esperado = 1 / (1 + 10^((dificuldade - rating) / 400))
resultado = 1 (acerto sem dica) | 0,6 (acerto com dica) | 0 (erro)
novoRating = rating + K * (resultado - esperado)      // K = 32 no início, cai para 16 com mais respostas
```

### 4.2 Etapas e regiões (camada do jogo)

- Uma etapa é concluída com **1 a 3 estrelas** (ex.: ≥60%, ≥80%, 100% de acertos).
- Concluir etapas desbloqueia as próximas e novas regiões.
- O jogador pode **repetir** etapas para melhorar estrelas (revisão espaçada natural).

### 4.3 Níveis gerais (camada visível)

O **nível geral** do jogador em cada tópico (Matemática, Lógica) é derivado do domínio das habilidades daquele nível:

| Nível     | Faixa de rating | Para subir de nível                                                        |
|-----------|-----------------|----------------------------------------------------------------------------|
| Iniciante | 0 – 999         | ≥ 80% das habilidades do nível com rating ≥ 1000 **e** passar no Desafio do Guardião |
| Médio     | 1000 – 1499     | idem, com rating ≥ 1500                                                    |
| Avançado  | 1500 – 1999     | idem, com rating ≥ 2000                                                    |
| Expert    | 2000+           | nível máximo — conteúdo de desafio contínuo e ranking opcional             |

- **Matemática e Lógica têm níveis independentes** (um aluno pode ser Avançado em Lógica e Médio em Matemática).
- **Nunca se perde nível.** O rating pode oscilar, mas o nível conquistado fica. Se o rating cair muito numa habilidade, ela é sinalizada como "precisa de revisão".

### 4.4 Tela de progresso

Deve mostrar, de forma clara:

- Nível atual em cada tópico, com **barra de progresso até o próximo nível** (ex.: "Médio — 65% para Avançado").
- Mapa das habilidades com cores: 🔒 bloqueada, ⚪ não iniciada, 🟡 praticando, 🟢 dominada, 🟠 precisa revisão.
- Histórico: problemas resolvidos, sequência de dias, tempo de prática, evolução semanal (gráfico simples).
- Conquistas/medalhas obtidas.

### 4.5 Dificuldade adaptativa na sessão

Ao montar uma sessão, o servidor escolhe problemas com dificuldade **perto do rating do jogador** (zona de desenvolvimento proximal):

- ~70% dos problemas na faixa `rating ± 100` (desafio adequado),
- ~20% um pouco abaixo (confiança e revisão),
- ~10% um pouco acima (desafio extra).

Meta: taxa de acerto em torno de **70–85%** — difícil o bastante para aprender, fácil o bastante para não frustrar.

---

## 5. Conteúdo por nível

Referência: competências da BNCC, sem amarrar o jogo a séries específicas.

### 5.1 Matemática

| Nível | Habilidades sugeridas |
|---|---|
| **Iniciante** | Contagem e comparação de números; adição e subtração até 100; dezenas e unidades; noção de multiplicação (grupos iguais); tabuada até 5; formas geométricas; medidas simples (horas, dinheiro) |
| **Médio** | Tabuada completa; divisão com e sem resto; expressões com as 4 operações e ordem de operações; frações (equivalência, comparação, soma com mesmo denominador); números decimais; porcentagem simples (10%, 25%, 50%); perímetro e área de retângulos; problemas de 2 etapas |
| **Avançado** | Números inteiros (negativos); potências e raízes; MMC e MDC; frações com denominadores diferentes; razão e proporção, regra de três; equações do 1º grau; ângulos, triângulos, área de figuras compostas; média, moda e mediana; probabilidade simples |
| **Expert** | Sistemas de equações; equações do 2º grau; funções e gráficos; porcentagem composta/juros; geometria (Pitágoras, semelhança); combinatória (contagem, permutações); problemas multi-etapas e no estilo OBMEP |

### 5.2 Lógica

| Nível | Habilidades sugeridas |
|---|---|
| **Iniciante** | Padrões visuais (cores, formas); sequências simples (+1, +2, alternadas); "qual é o diferente?"; classificação por atributos; labirintos simples; noções de maior/menor/igual |
| **Médio** | Sequências numéricas com regras compostas; quadrados mágicos 3x3; Sudoku 4x4; problemas de "verdadeiro ou falso"; tabelas lógicas simples (2 pessoas × 2 atributos); balanças (pesos equivalentes) |
| **Avançado** | Sudoku 6x6/9x9 fácil; puzzles de grade (3×3 atributos, estilo "quem mora onde"); proposições e conectivos (e, ou, não, se…então); diagramas de Venn; criptoaritmética simples; sequências de figuras com rotação/espelhamento |
| **Expert** | Lógica proposicional (tabelas-verdade, implicação, contrapositiva); problemas de "cavaleiros e mentirosos"; puzzles de otimização (menor caminho, travessia do rio); algoritmos (ordenar passos, "pensamento computacional"); desafios olímpicos de raciocínio |

---

## 6. Tipos de desafio (mecânicas)

Variar a forma de responder mantém o jogo interessante e permite avaliar habilidades diferentes:

| Tipo | Descrição | Exemplo |
|---|---|---|
| `numero` | Digitar um número (teclado numérico próprio na tela) | 7 × 8 = ? |
| `multipla-escolha` | 3–4 opções, distratores baseados em erros comuns | ½ + ⅓ = ? (opções: 2/5, 5/6, 1/6…) |
| `verdadeiro-falso` | Avaliar uma afirmação | "Todo quadrado é retângulo." |
| `ordenar` | Arrastar itens na ordem certa | Ordene: 0,5 · ⅓ · 0,25 |
| `completar-sequencia` | Achar o próximo elemento | 2, 6, 18, 54, ? |
| `grade` | Preencher grade (Sudoku, quadrado mágico) | Sudoku 4×4 |
| `arrastar-soltar` | Associar pares, montar balança | Equilibre a balança |
| `fracao-visual` | Pintar partes de uma figura | Pinte ¾ do círculo |

Modos de jogo extras (fases posteriores):

- **Desafio diário**: 1 problema especial por dia, igual para todos.
- **Contra o relógio**: opcional, só para quem gosta (nunca obrigatório).
- **Revisão inteligente**: sessão montada com habilidades marcadas "precisa revisão".
- **Desafio do Guardião**: prova de nível com problemas mistos de todas as habilidades do nível.

---

## 7. Geração de problemas

Recomendação: **modelo híbrido**.

### 7.1 Problemas gerados por templates (maioria em Matemática)

Cada template define parâmetros, restrições, como calcular a resposta e como estimar a dificuldade:

```json
{
  "id": "mat.multiplicacao.tabuada",
  "skill": "tabuada-completa",
  "type": "numero",
  "params": { "a": { "min": 2, "max": 10 }, "b": { "min": 2, "max": 10 } },
  "prompt": "Quanto é {a} × {b}?",
  "answer": "a * b",
  "difficulty": "800 + (a + b) * 20",
  "hints": [
    "Pense em {a} grupos de {b}.",
    "{a} × {b} = {b} + {b} + … ({a} vezes)."
  ],
  "explanation": "{a} × {b} = {answer}"
}
```

- Infinitos problemas, sem repetição decorada.
- Geração com **semente (seed)** → o mesmo problema pode ser reproduzido para auditoria/depuração.
- Implementados como classes C# (`IProblemGenerator`) para lógica mais complexa, e templates JSON para casos simples.

### 7.2 Problemas curados (maioria em Lógica e nível Expert)

Puzzles de lógica e problemas abertos de qualidade são difíceis de gerar automaticamente. Ficam num **banco de problemas** no Postgres, com dificuldade inicial definida manualmente.

### 7.3 Calibração automática

Com o tempo, a dificuldade de cada problema/template é **recalibrada** com base nas respostas reais (o mesmo Elo, mas do ponto de vista do problema). Um job periódico no backend faz isso.

### 7.4 Validação no servidor

A resposta correta **nunca vai para o navegador** antes do jogador responder. O cliente envia a resposta, o servidor valida, calcula XP/rating e devolve o resultado + explicação. Isso evita trapaça e mantém o progresso confiável.

---

## 8. Gamificação e motivação

**Usar:**
- XP por problema (bônus por dificuldade, menos XP quando usa dica).
- Estrelas por etapa; medalhas por marcos ("100 problemas", "7 dias seguidos", "Mestre das Frações").
- Sequência de dias (*streak*) com "dia de folga" permitido para não gerar ansiedade.
- Avatar/personagem personalizável com itens desbloqueados por XP (sem compras).
- Mensagens que valorizam **esforço e estratégia** ("Você tentou de novo e acertou!") e não só talento.

**Evitar:**
- Vidas/corações que bloqueiam o jogo ao errar.
- Rankings públicos obrigatórios (deixar opcional e, se existir, só entre amigos/turma).
- Moedas compráveis, anúncios, loot boxes, pressão de tempo obrigatória.
- Notificações insistentes.

---

## 9. Experiência do usuário e acessibilidade

- Interface limpa, botões grandes (mínimo 44×44 px), fontes legíveis (ex.: *Atkinson Hyperlegible* ou *Lexend*).
- **Teclado numérico na tela** para respostas — evita o teclado do celular e erros de digitação.
- Suporte a teclado e leitor de tela (HTML semântico, `aria-live` para feedback).
- Alto contraste, modo escuro, opção de reduzir animações (`prefers-reduced-motion`).
- Não depender só de cor (certo/errado também com ícone e texto).
- Opção de **leitura em voz alta** do enunciado (Web Speech API) para crianças em alfabetização.
- Notação matemática: começar com HTML/CSS puro para frações e potências; se precisar de algo mais elaborado no nível Expert, MathML (suportado nativamente nos navegadores atuais).

---

## 10. Arquitetura técnica

### 10.1 Visão geral

```
┌─────────────────────── docker compose ───────────────────────┐
│                                                               │
│   ┌──────────────┐     /api/*      ┌──────────────┐           │
│   │    web       │ ──────────────► │     api      │           │
│   │  (nginx)     │                 │  (.NET 10)   │           │
│   │ HTML/CSS/JS  │                 │ Minimal APIs │           │
│   └──────┬───────┘                 └──────┬───────┘           │
│          │ :8080 (exposto)                │                   │
│                                    ┌──────▼───────┐           │
│                                    │      db      │           │
│                                    │ (Postgres)   │           │
│                                    └──────────────┘           │
└───────────────────────────────────────────────────────────────┘
```

- O **nginx** serve os arquivos estáticos e faz *proxy reverso* de `/api` para o backend → front e API na **mesma origem** (sem CORS, cookies simples).
- Alternativa mais simples: a própria API servir os estáticos (`UseStaticFiles`). Recomendo o nginx para manter front e back separados e com cache de estáticos eficiente.

### 10.2 Frontend (HTML, CSS e JavaScript puro)

Sem frameworks e sem etapa de build obrigatória — tudo nativo do navegador:

- **ES Modules** (`<script type="module">`) para organizar o código.
- **Web Components** (`customElements`) para peças reutilizáveis: `<numeric-keypad>`, `<problem-card>`, `<progress-bar>`, `<skill-map>`, `<level-badge>`.
- **Roteador por hash** simples (`#/mapa`, `#/sessao/123`, `#/progresso`) — ~50 linhas.
- **Estado** num pequeno *store* com padrão observer (`subscribe`/`notify`).
- **`fetch`** encapsulado num módulo `api.js` (tratamento de erro, autenticação, JSON).
- **CSS** com *custom properties* (tema claro/escuro, cores por nível), layout com Grid/Flexbox, `@layer` para organizar estilos, animações em CSS.
- **SVG inline** para ícones, mapa e figuras geométricas; **Canvas** só se algum puzzle precisar.
- Opcional depois: Service Worker + manifest para virar **PWA** (instalável, cache offline).

Estrutura sugerida:

```
frontend/
├── index.html
├── css/
│   ├── tokens.css        # cores, fontes, espaçamentos (custom properties)
│   ├── base.css
│   ├── components.css
│   └── pages.css
├── js/
│   ├── main.js           # bootstrap + roteador
│   ├── router.js
│   ├── store.js
│   ├── api.js
│   ├── pages/            # mapa.js, sessao.js, progresso.js, perfil.js, login.js
│   ├── components/       # web components
│   └── problem-types/    # renderizador por tipo de desafio (numero.js, grade.js…)
├── assets/               # svg, fontes, sons
└── i18n/pt-BR.json
```

> Cada tipo de desafio tem um **renderizador** no front e um **validador** no back, ligados pelo campo `type`. Adicionar uma nova mecânica = criar esse par.

### 10.3 Backend (.NET 10)

- **ASP.NET Core Minimal APIs**, organizadas por *feature* (endpoints agrupados com `MapGroup`).
- **EF Core 10 + Npgsql** para acesso ao Postgres, com **migrations** aplicadas no start em desenvolvimento.
- **Autenticação**: ASP.NET Core Identity com cookie `HttpOnly` + `SameSite=Strict` (mais simples e seguro que JWT, já que front e API estão na mesma origem).
- **Validação** de entrada com `FluentValidation` ou validação nativa; erros no formato **ProblemDetails**.
- **OpenAPI** nativo (`Microsoft.AspNetCore.OpenApi`) para documentar a API.
- **Health checks** (`/health`) para o Docker.
- **Logs estruturados** (`ILogger` + console JSON; Serilog opcional).
- **Rate limiting** nativo nos endpoints de login e resposta.
- **Background service** para recalibrar dificuldades e calcular estatísticas.

Estrutura sugerida:

```
backend/
├── EduGame.sln
├── src/
│   ├── EduGame.Api/              # Program.cs, endpoints, auth, config
│   │   └── Features/
│   │       ├── Auth/
│   │       ├── Profiles/
│   │       ├── Sessions/         # montar sessão, responder problema
│   │       ├── Progress/         # níveis, habilidades, histórico
│   │       └── Content/          # tópicos, regiões, etapas
│   ├── EduGame.Domain/           # entidades, regras de rating, níveis, XP (sem dependências)
│   ├── EduGame.Problems/         # geradores e validadores de problemas
│   └── EduGame.Infrastructure/   # DbContext, migrations, repositórios, seeds
└── tests/
    ├── EduGame.Domain.Tests/
    ├── EduGame.Problems.Tests/   # testa cada gerador: resposta correta, faixas, dicas
    └── EduGame.Api.Tests/        # testes de integração (WebApplicationFactory + Testcontainers)
```

### 10.4 Banco de dados (PostgreSQL)

Modelo inicial (simplificado):

```
users                (id, email, password_hash, role[player|guardian|teacher|admin], created_at)
player_profiles      (id, user_id, nickname, avatar, birth_year, school_grade, created_at)
subjects             (id, slug, name)                         -- matematica, logica (futuro: portugues…)
levels               (id, subject_id, slug, name, order, min_rating)   -- iniciante…expert
skills               (id, subject_id, level_id, slug, name, description, order)
skill_prerequisites  (skill_id, prerequisite_skill_id)
regions              (id, subject_id, level_id, name, order)
stages               (id, region_id, name, order, skill_ids[], problem_count)
problem_templates    (id, skill_id, type, generator_key, config jsonb, base_difficulty, rating, active)
curated_problems     (id, skill_id, type, content jsonb, answer jsonb, difficulty, rating, active)
game_sessions        (id, profile_id, stage_id, mode, started_at, finished_at, stars, xp_earned)
session_problems     (id, session_id, template_id | curated_id, seed, difficulty, order)
attempts             (id, session_problem_id, answer jsonb, is_correct, hints_used, time_ms, created_at)
skill_mastery        (profile_id, skill_id, rating, attempts_count, correct_count, last_practiced_at, status)
subject_progress     (profile_id, subject_id, current_level_id, xp, level_reached_at)
achievements         (id, slug, name, description, criteria jsonb)
profile_achievements (profile_id, achievement_id, earned_at)
daily_activity       (profile_id, date, problems_solved, minutes, xp)    -- streak e gráficos
```

Notas:
- `jsonb` para conteúdo de problemas/templates → flexível para novos tipos sem migrations a cada mecânica.
- **`subjects` é a chave da extensibilidade**: novos tópicos entram como dados + geradores, sem mudar a estrutura.
- Um `user` pode ter **vários perfis** (ex.: responsável com dois filhos).
- Índices em `attempts(created_at)`, `skill_mastery(profile_id)`, `game_sessions(profile_id, started_at)`.

### 10.5 API (primeira versão)

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/auth/register` | Criar conta |
| POST | `/api/auth/login` · `/logout` | Sessão por cookie |
| GET | `/api/me` | Usuário e perfis |
| POST | `/api/profiles` | Criar perfil de jogador |
| GET | `/api/subjects` | Tópicos disponíveis |
| GET | `/api/subjects/{slug}/map?profileId=` | Regiões, etapas e estado (bloqueada/estrelas) |
| POST | `/api/placement` | Iniciar teste de nivelamento |
| POST | `/api/sessions` | Iniciar sessão (etapa ou modo) → retorna 1º problema |
| GET | `/api/sessions/{id}/next` | Próximo problema (sem a resposta) |
| POST | `/api/sessions/{id}/problems/{pid}/hint` | Pedir dica |
| POST | `/api/sessions/{id}/problems/{pid}/answer` | Responder → correto?, explicação, XP, rating |
| POST | `/api/sessions/{id}/finish` | Resumo: estrelas, XP, medalhas, subida de nível |
| GET | `/api/profiles/{id}/progress` | Níveis, % para o próximo, mapa de habilidades |
| GET | `/api/profiles/{id}/history?from=&to=` | Atividade diária para gráficos |
| GET | `/api/profiles/{id}/achievements` | Medalhas |

### 10.6 Docker Compose

```yaml
services:
  db:
    image: postgres:17-alpine
    environment:
      POSTGRES_DB: edugame
      POSTGRES_USER: edugame
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?defina no .env}
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U edugame -d edugame"]
      interval: 5s
      retries: 10

  api:
    build: ./backend
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Development}
      ConnectionStrings__Default: Host=db;Database=edugame;Username=edugame;Password=${POSTGRES_PASSWORD}
    depends_on:
      db:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 10s

  web:
    image: nginx:1.27-alpine
    volumes:
      - ./frontend:/usr/share/nginx/html:ro
      - ./nginx/default.conf:/etc/nginx/conf.d/default.conf:ro
    ports:
      - "8080:80"
    depends_on:
      - api

volumes:
  pgdata:
```

- `backend/Dockerfile` *multi-stage*: `mcr.microsoft.com/dotnet/sdk:10.0` para build e `mcr.microsoft.com/dotnet/aspnet:10.0` para runtime (rodando como usuário não-root).
- Em desenvolvimento, o volume do `frontend` permite editar HTML/CSS/JS e só recarregar a página. Um `compose.override.yml` pode rodar a API com `dotnet watch`.
- Opcional: adicionar **pgAdmin** ou **Adminer** no compose de dev.
- Segredos em `.env` (fora do git), com um `.env.example` versionado.

---

## 11. Privacidade e segurança (público infantil)

Por envolver menores de idade, a **LGPD** (art. 14) exige cuidado especial:

- **Coletar o mínimo**: apelido, ano de nascimento e série. Nada de nome completo, foto real, localização ou e-mail da criança.
- A conta é criada pelo **responsável** (e-mail do adulto); a criança usa um perfil dentro dessa conta. Consentimento explícito do responsável no cadastro.
- Nenhum chat livre entre jogadores; apelidos com filtro de palavras impróprias.
- Sem anúncios e sem rastreadores de terceiros.
- Responsável pode **exportar e apagar** os dados.
- Senhas com hash (Identity já usa PBKDF2), HTTPS em produção, cookies `HttpOnly`/`Secure`, rate limiting, cabeçalhos de segurança no nginx (CSP, etc.).

---

## 12. Extensibilidade para novos tópicos

Para adicionar, no futuro, Português, Ciências etc., o desenho já prevê:

1. Novo registro em `subjects` + seus `levels`, `skills`, `regions`, `stages` (via seed/migration de dados).
2. Novos geradores/validadores no projeto `EduGame.Problems` (ou problemas curados no banco).
3. Novos tipos de desafio, se necessário (ex.: `completar-frase`), com renderizador no front.
4. A tela de progresso e o sistema de níveis funcionam **automaticamente** para qualquer tópico, porque são dirigidos por dados.

Recomendação: já na fase 1, **não "chumbar"** "matemática" ou "lógica" em lugar nenhum do código — sempre usar o `subject_id`/`slug`.

---

## 13. Roadmap sugerido

### Fase 0 — Fundação (1–2 semanas)
- Repositório, `docker-compose`, Dockerfile da API, nginx, Postgres.
- Projeto .NET com health check, EF Core, primeira migration.
- Página inicial estática servida pelo nginx chamando `/api/health`.
- CI simples (build + testes) — GitHub Actions.

### Fase 1 — MVP jogável (3–5 semanas)
- Cadastro/login do responsável e criação de perfil.
- Matemática **Iniciante** completa (~6 habilidades com geradores) e Lógica **Iniciante** (~4 habilidades).
- Tipos de desafio: `numero`, `multipla-escolha`, `completar-sequencia`.
- Sessão de problemas com feedback, dicas e explicação.
- Rating por habilidade, XP, estrelas por etapa.
- Tela de progresso com nível e barra para o próximo nível.

### Fase 2 — Progressão completa (4–6 semanas)
- Conteúdo dos níveis **Médio** e **Avançado**.
- Teste de nivelamento e Desafio do Guardião.
- Mapa visual de regiões, medalhas, streak, histórico com gráfico.
- Tipos `ordenar`, `verdadeiro-falso`, `grade`, `fracao-visual`.
- Calibração automática de dificuldade.

### Fase 3 — Expert e engajamento (4+ semanas)
- Conteúdo **Expert**, problemas curados de lógica.
- Desafio diário, revisão inteligente, avatar personalizável.
- Painel do responsável/professor (turmas, relatórios).
- PWA (offline parcial), leitura em voz alta, acessibilidade revisada.

### Fase 4 — Novos tópicos
- Primeiro tópico novo usando a mesma estrutura.

---

## 14. Qualidade e testes

- **Geradores de problemas** são a parte mais crítica: testes automatizados gerando milhares de problemas por template e verificando que (a) a resposta está correta, (b) os números ficam nas faixas, (c) não há divisão por zero, frações não simplificadas onde não deveria, etc.
- **Regras de domínio** (rating, níveis, XP, estrelas) com testes unitários.
- **API** com testes de integração usando Postgres real via Testcontainers.
- **Frontend**: começar com testes manuais + checklist; depois, testes E2E com Playwright para os fluxos principais (login → sessão → progresso).
- **Testes com usuários reais** (crianças de idades diferentes) desde o MVP: observar onde travam, o que acham chato, se entendem as dicas.

---

## 15. Métricas para acompanhar

- Taxa de acerto por habilidade e por faixa de dificuldade (meta 70–85%).
- Tempo médio por problema e por sessão.
- Uso de dicas (dicas demais = problema mal calibrado ou enunciado confuso).
- Retenção: jogadores que voltam em D1, D7, D30.
- Tempo médio para subir de nível.
- Problemas com taxa de erro anormal (possível bug no gerador ou enunciado ambíguo).

---

## 16. Decisões em aberto

1. **Nome e tema visual** do jogo (Expedição? Espaço? Laboratório?).
2. **Conta**: só via responsável, ou permitir que adolescentes (13+) criem a própria conta?
3. **Escola**: o painel de professor/turma entra no MVP ou depois?
4. **Ranking**: ter ou não? Se sim, só entre amigos/turma?
5. **Hospedagem** de produção (VPS com Docker, cloud gerenciada…) e domínio.
6. **Arte e sons**: criar próprios, usar bibliotecas livres (ex.: ícones e sons com licença CC0) ou contratar.
7. **Offline**: quanto do jogo deve funcionar sem internet (impacta a decisão de validar respostas só no servidor).

---

## 17. Próximos passos imediatos

1. Validar este plano e decidir os itens da seção 16 que afetam o MVP (2, 3 e 7).
2. Montar a **Fase 0**: estrutura de pastas, `docker-compose.yml`, API .NET 10 com health check, Postgres e nginx servindo uma página.
3. Escrever os primeiros **3 geradores de problemas** (soma, subtração, sequências) com testes, para validar o formato.
4. Fazer um protótipo de tela da sessão de problemas (HTML/CSS) e testar com uma criança.
