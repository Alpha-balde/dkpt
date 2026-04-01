#!/bin/sh
# ============================================
# DKPT — Script de seed automatique
# ============================================
# Attend que les tables existent (créées par les migrations EF Core)
# puis importe toutes les données SQL.
# Idempotent : ne réinsère pas si les données existent déjà.
# ============================================

set -e

PGCMD="psql -h db -U $POSTGRES_USER -d $POSTGRES_DB"

echo "=== DKPT Seed : attente du backend (migrations EF Core)... ==="

# Attendre que la table 'members' existe (= migrations terminées)
RETRIES=0
MAX_RETRIES=60
until $PGCMD -tAc "SELECT 1 FROM information_schema.tables WHERE table_name='members'" 2>/dev/null | grep -q 1; do
  RETRIES=$((RETRIES + 1))
  if [ $RETRIES -ge $MAX_RETRIES ]; then
    echo "ERREUR: Timeout - les tables n'ont pas ete creees apres ${MAX_RETRIES}s"
    exit 1
  fi
  echo "  En attente des migrations... ($RETRIES/${MAX_RETRIES}s)"
  sleep 1
done

echo "=== Tables detectees, import des donnees... ==="

# 1. Settings + Contribution Amounts
echo "[1/4] Settings & Contribution Amounts..."
$PGCMD -f /seed/seed-data.sql
echo "  OK"

# 2. Members
if [ -f /data/members_rows.sql ]; then
  COUNT=$($PGCMD -tAc "SELECT count(*) FROM members")
  if [ "$COUNT" = "0" ]; then
    echo "[2/4] Import membres..."
    $PGCMD -f /data/members_rows.sql
    echo "  OK"
  else
    echo "[2/4] Membres deja presents ($COUNT lignes) - skip"
  fi
else
  echo "[2/4] Fichier members_rows.sql non trouve - skip"
fi

# 3. Payments
if [ -f /data/payments_rows.sql ]; then
  COUNT=$($PGCMD -tAc "SELECT count(*) FROM payments")
  if [ "$COUNT" = "0" ]; then
    echo "[3/4] Import paiements..."
    $PGCMD -f /data/payments_rows.sql
    echo "  OK"
  else
    echo "[3/4] Paiements deja presents ($COUNT lignes) - skip"
  fi
else
  echo "[3/4] Fichier payments_rows.sql non trouve - skip"
fi

# 4. Verification
echo "[4/4] Verification..."
$PGCMD -c "SELECT 'members' as tbl, count(*) FROM members UNION ALL SELECT 'payments', count(*) FROM payments UNION ALL SELECT 'contributions', count(*) FROM contribution_amounts UNION ALL SELECT 'settings', count(*) FROM settings;"

echo ""
echo "=== Seed termine avec succes ! ==="
