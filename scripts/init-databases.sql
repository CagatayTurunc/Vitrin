-- ==============================================================
-- Vitrin - PostgreSQL Veritabanı Başlatma Scripti
-- Her servis için ayrı veritabanı oluşturur.
-- Bu script yalnızca ilk kez çalışır (volume boşsa).
-- ==============================================================

-- Auth servisi veritabanı (zaten POSTGRES_DB ile oluşturulmuş olabilir)
SELECT 'CREATE DATABASE vitrin_auth'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'vitrin_auth')\gexec

-- Product servisi veritabanı
SELECT 'CREATE DATABASE vitrin_product'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'vitrin_product')\gexec

-- Comment servisi veritabanı
SELECT 'CREATE DATABASE vitrin_comment'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'vitrin_comment')\gexec
