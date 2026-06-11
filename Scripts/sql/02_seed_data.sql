-- QR Destekli Stok ve Barkod Yönetimi — başlangıç verileri (Form menü + varsayılan tipler)
-- Önkoşul: 01_create_tables.sql çalıştırılmış olmalıdır.
-- Admin kullanıcı uygulama ilk açılışında Program.cs → SeedAdminAsync ile oluşturulur.

BEGIN;

-- ── Kullanıcı tipleri ────────────────────────────────────────────────────────
INSERT INTO "KullaniciTip" ("ID", "Ad", "Aktif", "Varsayilan", "CreUser", "CreDate")
VALUES
    (1, 'Admin',   TRUE,  FALSE, 1, NOW()),
    (2, 'Kullanıcı', TRUE, TRUE,  1, NOW())
ON CONFLICT ("ID") DO NOTHING;

SELECT setval(
    pg_get_serial_sequence('"KullaniciTip"', 'ID'),
    GREATEST((SELECT COALESCE(MAX("ID"), 1) FROM "KullaniciTip"), 1)
);

-- ── Form / menü kayıtları ────────────────────────────────────────────────────
-- IsMenu=false olanlar yetki yönetimi alt sayfalarıdır (menüde görünmez, RBAC için gerekli).
INSERT INTO "Form" ("ID", "Ad", "IsMenu", "UstMenu_ID", "Sira", "SayfaURL", "Icon", "CreUser", "CreDate")
VALUES
    ( 1, 'Tanımlar',           TRUE,  NULL, 10, NULL,                'fa-solid fa-folder',         1, NOW()),
    ( 2, 'Birimler',           TRUE,   1,   11, '/birimler',         'fa-solid fa-ruler',          1, NOW()),
    ( 3, 'Kategoriler',        TRUE,   1,   12, '/kategoriler',      'fa-solid fa-tags',           1, NOW()),
    ( 4, 'Ürünler',            TRUE,   1,   13, '/urunler',          'fa-solid fa-box',            1, NOW()),
    ( 5, 'Stok',               TRUE,  NULL, 20, NULL,                'fa-solid fa-warehouse',      1, NOW()),
    ( 6, 'Stok İşlemleri',     TRUE,   5,   21, '/stok-islemleri',   'fa-solid fa-arrows-rotate',  1, NOW()),
    ( 7, 'Stok Giriş',         TRUE,   5,   22, '/stok-giris',       'fa-solid fa-arrow-down',     1, NOW()),
    ( 8, 'Stok Çıkış',         TRUE,   5,   23, '/stok-cikis',       'fa-solid fa-arrow-up',       1, NOW()),
    ( 9, 'Stok Hareketleri',   TRUE,   5,   24, '/stok-hareketleri', 'fa-solid fa-list',           1, NOW()),
    (10, 'Kullanıcı Yönetimi', TRUE,  NULL, 30, NULL,                'fa-solid fa-users',          1, NOW()),
    (11, 'Kullanıcılar',       TRUE,  10,   31, '/kullanicilar',     'fa-solid fa-user',           1, NOW()),
    (12, 'Kullanıcı Tipleri',  TRUE,  10,   32, '/kullanici-tipler', 'fa-solid fa-user-tag',       1, NOW()),
    (13, 'Kullanıcı Yetki',    FALSE, NULL, 90, '/kullanici-yetki',  'fa-solid fa-user-shield',    1, NOW()),
    (14, 'Kullanıcı Tip Yetki',FALSE, NULL, 91, '/kullanici-tip-yetki', 'fa-solid fa-shield-halved', 1, NOW())
ON CONFLICT ("ID") DO NOTHING;

SELECT setval(
    pg_get_serial_sequence('"Form"', 'ID'),
    GREATEST((SELECT COALESCE(MAX("ID"), 1) FROM "Form"), 1)
);

-- ── Örnek birimler (isteğe bağlı) ────────────────────────────────────────────
INSERT INTO "Birim" ("Ad", "Aktif", "CreUser", "CreDate")
SELECT v."Ad", TRUE, 1, NOW()
FROM (VALUES ('Adet'), ('Kg'), ('Litre'), ('Paket')) AS v("Ad")
WHERE NOT EXISTS (
    SELECT 1 FROM "Birim" b
    WHERE LOWER(b."Ad") = LOWER(v."Ad") AND b."DelUser" IS NULL
);

COMMIT;
