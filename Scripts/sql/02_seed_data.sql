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

-- ── Form / menü kayıtları (lokal DB ile senkron) ─────────────────────────────
-- IsMenu=false olanlar menüde görünmez; RBAC ve alt sayfa erişimi için gerekli.
INSERT INTO "Form" ("ID", "Ad", "IsMenu", "UstMenu_ID", "Sira", "SayfaURL", "Icon", "CreUser", "CreDate")
VALUES
    ( 1, 'Ürünler',             TRUE,  NULL, 11, '/urunler',             NULL, 1, NOW()),
    ( 2, 'Kategoriler',         TRUE,  NULL, 10, '/kategoriler',         NULL, 1, NOW()),
    ( 3, 'Birimler',            TRUE,  NULL,  9, '/birimler',            NULL, 1, NOW()),
    ( 7, 'Stok Giriş',          FALSE, 17,    3, '/stok-giris',          NULL, 1, NOW()),
    ( 8, 'Stok Çıkış',          FALSE, 17,    4, '/stok-cikis',          NULL, 1, NOW()),
    ( 9, 'Stok Hareketleri',    TRUE,  17,    2, '/stok-hareketleri',    NULL, 1, NOW()),
    (12, 'Kullanıcılar',        TRUE,  19,    7, '/kullanicilar',        NULL, 1, NOW()),
    (13, 'Kullanıcı Tipleri',   TRUE,  19,    8, '/kullanici-tipler',    NULL, 1, NOW()),
    (14, 'Ana Sayfa',           TRUE,  NULL,  1, '/',                    NULL, 1, NOW()),
    (15, 'Kullanıcı Yetki',     FALSE, NULL, 12, '/kullanici-yetki',     NULL, 1, NOW()),
    (16, 'Kullanıcı Tip Yetki', FALSE, NULL, 13, '/kullanici-tip-yetki', NULL, 1, NOW()),
    (17, 'Stok Yönetimi',       TRUE,  NULL,  1, NULL,                   NULL, 1, NOW()),
    (19, 'Kullanıcı Yönetimi',  TRUE,  NULL,  6, NULL,                   NULL, 1, NOW()),
    (26, 'Stok İşlemleri',      TRUE,  17,    5, '/stok-islemleri',      NULL, 1, NOW())
ON CONFLICT ("ID") DO UPDATE SET
    "Ad" = EXCLUDED."Ad",
    "IsMenu" = EXCLUDED."IsMenu",
    "UstMenu_ID" = EXCLUDED."UstMenu_ID",
    "Sira" = EXCLUDED."Sira",
    "SayfaURL" = EXCLUDED."SayfaURL",
    "Icon" = EXCLUDED."Icon",
    "DelUser" = NULL,
    "DelDate" = NULL,
    "UpdUser" = 1,
    "UpdDate" = NOW();

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
