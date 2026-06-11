-- QR Destekli Stok ve Barkod Yönetimi — tam veritabanı kurulumu
--
-- Kullanım (psql) — önce scripts/sql klasörüne geçin:
--   CREATE DATABASE "StokVeBarkodYonetimiDB" ENCODING 'UTF8';
--   cd scripts/sql
--   psql -U postgres -d StokVeBarkodYonetimiDB -f 00_kurulum.sql
--
-- Alternatif (proje kökünden, tek tek):
--   psql -U postgres -d StokVeBarkodYonetimiDB -f scripts/sql/01_create_tables.sql
--   psql -U postgres -d StokVeBarkodYonetimiDB -f scripts/sql/02_seed_data.sql

\echo '>> Tablolar oluşturuluyor...'
\ir 01_create_tables.sql

\echo '>> Başlangıç verileri yükleniyor...'
\ir 02_seed_data.sql

\echo '>> Kurulum tamamlandı.'
\echo '>> Uygulamayı başlatın; admin@sistem.com / Admin@123 otomatik oluşturulur.'
