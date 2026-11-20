#!/usr/bin/env bash
# Substitui as tabelas do jogo num banco (ex.: o do servidor) por um backup completo.
#
# Uso:
#   scripts/db/restaurar-backup-no-servidor.sh HOST PORTA USUARIO BANCO ARQUIVO.dump
#
# Exemplo:
#   scripts/db/restaurar-backup-no-servidor.sh 127.0.0.1 15432 meu_usuario meu_banco backups/meu-backup.dump
#
# O que faz:
#   1. pede a senha do banco (digitada por você; não fica salva em lugar nenhum);
#   2. mostra a versão e quantas contas existem hoje no destino;
#   3. faz um backup do destino em backups/ (por segurança);
#   4. pede confirmação ("SIM");
#   5. apaga as tabelas do jogo (scripts/db/apagar-tabelas-do-jogo.sql);
#   6. restaura o backup completo e confere os números.
#
# Pare o app no Coolify antes de rodar e inicie de novo depois.
# Variáveis opcionais: PG_BIN (pasta do psql/pg_restore), PG_VERSAO_MINIMA (padrão 180000 = Postgres 18).

set -euo pipefail

if [[ $# -ne 5 ]]; then
  sed -n '2,8p' "$0"
  exit 1
fi

HOST=$1 PORTA=$2 USUARIO=$3 BANCO=$4 DUMP=$5
BIN=${PG_BIN:-/opt/homebrew/opt/postgresql@18/bin}
VERSAO_MINIMA=${PG_VERSAO_MINIMA:-180000}
RAIZ=$(cd "$(dirname "$0")/../.." && pwd)

[[ -f "$DUMP" ]] || { echo "Arquivo de backup não encontrado: $DUMP"; exit 1; }
[[ -x "$BIN/psql" && -x "$BIN/pg_restore" && -x "$BIN/pg_dump" ]] || { echo "psql/pg_restore/pg_dump não encontrados em $BIN (defina PG_BIN)."; exit 1; }

read -r -s -p "Senha do banco ($USUARIO@$HOST:$PORTA/$BANCO): " SENHA
echo
export PGHOST=$HOST PGPORT=$PORTA PGUSER=$USUARIO PGDATABASE=$BANCO PGPASSWORD=$SENHA PGCONNECT_TIMEOUT=15
export PGSSLMODE=${PGSSLMODE:-prefer}
unset SENHA

echo "==> Conectando..."
VERSAO=$("$BIN/psql" -tAc "select current_setting('server_version_num')")
echo "    PostgreSQL $("$BIN/psql" -tAc "select current_setting('server_version')") em $HOST:$PORTA, banco $BANCO"
if (( VERSAO < VERSAO_MINIMA )); then
  echo "PARADO: versão $VERSAO menor que $VERSAO_MINIMA. Isto não parece o servidor (o banco local é 17)."
  exit 1
fi
CONTAS=$("$BIN/psql" -tAc "select case when to_regclass('public.users') is null then 'nenhuma (tabela não existe)' else (select count(*)::text from users) end")
echo "    Contas hoje no destino: $CONTAS"

mkdir -p "$RAIZ/backups"
ANTES="$RAIZ/backups/destino-antes-$(date +%Y%m%d-%H%M%S).dump"
echo "==> Backup do destino antes de mexer: $ANTES"
"$BIN/pg_dump" -Fc --no-owner --no-acl -f "$ANTES"

echo
echo "Isto vai APAGAR as tabelas do jogo em $BANCO@$HOST e restaurar $(basename "$DUMP")."
read -r -p "Digite SIM para continuar: " CONFIRMA
[[ "$CONFIRMA" == "SIM" ]] || { echo "Cancelado. Nada foi alterado."; exit 1; }

echo "==> Apagando as tabelas do jogo..."
"$BIN/psql" -q -v versao_minima="$VERSAO_MINIMA" -f "$RAIZ/scripts/db/apagar-tabelas-do-jogo.sql"

echo "==> Restaurando o backup..."
"$BIN/pg_restore" --no-owner --no-acl --exit-on-error -d "$BANCO" "$DUMP"

echo "==> Conferência:"
"$BIN/psql" -c "select (select count(*) from users) as contas, (select count(*) from player_profiles) as perfis, (select count(*) from game_sessions) as sessoes, (select count(*) from attempts) as respostas;"
"$BIN/psql" -c "select su.slug as topico, s.slug as habilidade, m.rating from skill_mastery m join skills s on s.id = m.skill_id join subjects su on su.id = s.subject_id where m.attempts_count > 0 order by su.slug, s.id;"

echo
echo "Pronto. Inicie o app no Coolify. (Backup anterior do destino: $ANTES)"
