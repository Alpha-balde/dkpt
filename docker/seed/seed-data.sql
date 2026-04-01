-- ============================================
-- DKPT — Script d'import des données de seed
-- ============================================
-- 
-- Ce script importe les données de référence (settings + contribution_amounts).
-- Les tables doivent exister (créées par les migrations EF Core du backend au démarrage).
--
-- Usage (PowerShell, depuis la racine du projet) :
--   Get-Content "docker/seed/seed-data.sql" -Raw | docker compose exec -T db psql -U postgres -d dkpt
--
-- IMPORTANT : N'exécuter qu'une seule fois après le premier lancement.
-- ============================================

-- 1. Settings (singleton)
INSERT INTO "settings" ("id", "montant_cotisation_annuelle_par_defaut")
VALUES (1, '50000')
ON CONFLICT ("id") DO NOTHING;

-- 2. Contribution Amounts (montants par année)
INSERT INTO "contribution_amounts" ("year", "amount", "created_at", "updated_at") VALUES
(2024, 60000, '2026-01-02 17:37:25.047242+00', '2026-01-02 17:37:25.047242+00'),
(2025, 60000, '2026-01-02 17:37:25.047242+00', '2026-01-02 17:37:25.047242+00'),
(2026, 60000, '2026-01-02 17:37:25.047242+00', '2026-01-02 17:37:25.047242+00'),
(2027, 60000, '2026-01-02 17:37:25.047242+00', '2026-01-02 17:37:25.047242+00'),
(2028, 60000, '2026-01-04 17:43:04.40249+00', '2026-01-04 17:43:04.40249+00'),
(2029, 60000, '2026-02-05 12:54:23.840086+00', '2026-02-05 12:54:23.840086+00'),
(2030, 60000, '2026-02-05 12:54:44.385737+00', '2026-02-05 12:54:44.385737+00')
ON CONFLICT ("year") DO NOTHING;
