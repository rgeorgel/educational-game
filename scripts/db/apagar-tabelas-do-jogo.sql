-- Apaga TODAS as tabelas do Expedição Numérica (conteúdo e progresso) do banco conectado.
-- Uso: antes de restaurar um backup completo (pg_restore sem --clean), para os IDs ficarem iguais aos do backup.
--
--   psql -h 127.0.0.1 -p 15432 -U USUARIO -d BANCO -f scripts/db/apagar-tabelas-do-jogo.sql
--
-- Trava de segurança: só roda em Postgres 18 ou mais novo (o servidor). O banco local do
-- docker-compose é 17, então rodar contra ele por engano para aqui sem apagar nada.
-- Para mudar a versão mínima: -v versao_minima=170000
--
-- Tabelas que não são do jogo (se existirem no mesmo banco) não são tocadas.

\set ON_ERROR_STOP on

\if :{?versao_minima}
\else
  \set versao_minima 180000
\endif

SELECT current_setting('server_version_num')::int >= :versao_minima AS versao_ok,
       current_setting('server_version') AS versao,
       current_database() AS banco
\gset

\if :versao_ok
  \echo 'Conectado em' :banco '(PostgreSQL' :versao ') - apagando as tabelas do jogo...'
\else
  \echo 'PARADO: este banco é PostgreSQL' :versao '- não parece o servidor. Confira o túnel e a porta (-p).'
  \quit
\endif

BEGIN;

DROP TABLE IF EXISTS
    "__EFMigrationsHistory",
    attempts,
    classroom_members,
    classrooms,
    curated_problems,
    daily_activity,
    data_protection_keys,
    game_sessions,
    levels,
    player_profiles,
    problem_templates,
    profile_achievements,
    regions,
    role_claims,
    roles,
    session_problems,
    skill_mastery,
    skills,
    stage_progress,
    stages,
    subject_progress,
    subjects,
    user_claims,
    user_logins,
    user_roles,
    user_tokens,
    users
CASCADE;

COMMIT;

\echo 'Pronto. Agora restaure o backup com pg_restore (sem --clean).'
