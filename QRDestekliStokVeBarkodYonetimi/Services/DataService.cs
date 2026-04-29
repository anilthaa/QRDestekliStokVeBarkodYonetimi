using System.Linq;
using Dapper;
using Microsoft.AspNetCore.Components.Authorization;
using Npgsql;
using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    public class DataService : DBClass
    {
        private readonly AuthenticationStateProvider _stateProvider;

        public DataService(string connectionString, AuthenticationStateProvider stateProvider) : base(connectionString)
        {
            _stateProvider = stateProvider;
        }

        #region Kategori

        public async Task<ItemKategori[]> GetKategori(int ID = 0)
        {
            return (await SQLQueryAsync<ItemKategori>($@"SELECT ""ID"", ""ResimYolu"",""Ad"", ""Aciklama"", ""Aktif"" FROM ""Kategori""         
        where ""DelUser"" is null 
        {(ID > 0 ? $@"and ""ID"" = {ID}" : "")} 
        ORDER BY ""ID"" ")).ToArray();
        }

        public async Task<DataResult<int>> SetKategori(ItemKategori item)
        {
            if (item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                item.ID = await SQLExecuteScalar<int>(@"INSERT INTO ""Kategoriler"" (""ResimYolu"", ""Ad"", ""Aciklama"", ""Aktif"", ""CreUser"", ""CreDate"")  VALUES (@ResimYolu, @Ad, @Aciklama, @Aktif, @CreUser, now()) 
                 RETURNING ""ID""", item);
            }
            else if (!item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""Kategori"" 
                     SET ""ResimYolu"" = @ResimYolu, ""Ad"" = @Ad, ""Aciklama"" = @Aciklama, ""Aktif"" = @Aktif, ""UpdUser""=@UpdUser, ""UpdDate""=now()
                     WHERE ""ID"" = @ID", item);
            }
            else if (!item.ID.Equals(0) && !item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""Kategori"" 
                     SET ""DelUser"" = @DelUser, ""DelDate"" = now() 
                     WHERE ""ID"" = @ID", item);
            }
            return new() { Data = item.ID };
        }

        #endregion

        #region Birim

        public async Task<ItemBirim[]> GetBirim(int ID = 0)
        {
            return (await SQLQueryAsync<ItemBirim>($@"SELECT ""ID"", ""Ad"", ""Aktif"", ""CreUser"", ""CreDate"", ""UpdUser"", ""UpdDate"", ""DelUser"", ""DelDate""
        FROM ""Birimler""
        WHERE ""DelUser"" IS NULL
        {(ID > 0 ? $@"AND ""ID"" = {ID}" : "")}
        ORDER BY ""Ad"" ")).ToArray();
        }

        public async Task<DataResult<int>> SetBirim(ItemBirim item)
        {
            if (item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                item.ID = await SQLExecuteScalar<int>(@"INSERT INTO ""Birimler"" (""Ad"", ""Aktif"", ""CreUser"", ""CreDate"")
                 VALUES (@Ad, @Aktif, @CreUser, now())
                 RETURNING ""ID""", item);
            }
            else if (!item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""Birimler""
                     SET ""Ad"" = @Ad, ""Aktif"" = @Aktif, ""UpdUser"" = @UpdUser, ""UpdDate"" = now()
                     WHERE ""ID"" = @ID", item);
            }
            else if (!item.ID.Equals(0) && !item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""Birimler""
                     SET ""DelUser"" = @DelUser, ""DelDate"" = now()
                     WHERE ""ID"" = @ID", item);
            }
            return new() { Data = item.ID };
        }

        #endregion

        #region Urun

        public async Task<ItemUrun[]> GetUrun(int ID = 0)
        {
            return (await SQLQueryAsync<ItemUrun>($@"SELECT ""ID"", ""UrunKodu"", ""Kategori_ID"", ""BarkodNo"", ""ResimYolu"", ""Ad"", ""Aciklama"", ""Birim_ID"", ""Stok"", ""KritikStokSeviyesi"", ""CreUser"", ""CreDate"", ""UpdUser"", ""UpdDate"", ""DelUser"", ""DelDate""
        FROM ""Urunler""
        WHERE ""DelUser"" IS NULL
        {(ID > 0 ? $@"AND ""ID"" = {ID}" : "")}
        ORDER BY ""ID"" ")).ToArray();
        }

        public async Task<DataResult<int>> SetUrun(ItemUrun item)
        {
            if (item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                item.ID = await SQLExecuteScalar<int>(@"INSERT INTO ""Urunler"" (""UrunKodu"", ""Kategori_ID"", ""BarkodNo"", ""ResimYolu"", ""Ad"", ""Aciklama"", ""Birim_ID"", ""Stok"", ""KritikStokSeviyesi"", ""CreUser"", ""CreDate"")
                 VALUES (@UrunKodu, @Kategori_ID, @BarkodNo, @ResimYolu, @Ad, @Aciklama, @Birim_ID, @Stok, @KritikStokSeviyesi, @CreUser, now())
                 RETURNING ""ID""", item);
            }
            else if (!item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""Urunler""
                     SET ""UrunKodu"" = @UrunKodu, ""Kategori_ID"" = @Kategori_ID, ""BarkodNo"" = @BarkodNo, ""ResimYolu"" = @ResimYolu,
                         ""Ad"" = @Ad, ""Aciklama"" = @Aciklama, ""Birim_ID"" = @Birim_ID, ""Stok"" = @Stok,
                         ""KritikStokSeviyesi"" = @KritikStokSeviyesi, ""UpdUser"" = @UpdUser, ""UpdDate"" = now()
                     WHERE ""ID"" = @ID", item);
            }
            else if (!item.ID.Equals(0) && !item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""Urunler""
                     SET ""DelUser"" = @DelUser, ""DelDate"" = now()
                     WHERE ""ID"" = @ID", item);
            }
            return new() { Data = item.ID };
        }

        #endregion

        #region StokHareketleri

        public async Task<ItemStokHareketleri[]> GetStokHareketleri(int urunId = 0)
        {
            return (await SQLQueryAsync<ItemStokHareketleri>($@"
                SELECT sh.""ID"", sh.""Urun_ID"", sh.""HareketTipi"", sh.""Miktar"", sh.""Not"",
                       sh.""CreUser"", sh.""CreDate"", sh.""UpdUser"", sh.""UpdDate"", sh.""DelUser"", sh.""DelDate"",
                       u.""Ad"" AS ""UrunAd"", u.""UrunKodu""
                FROM ""StokHareketleri"" sh
                LEFT JOIN ""Urunler"" u ON u.""ID"" = sh.""Urun_ID""
                WHERE sh.""DelUser"" IS NULL
                {(urunId > 0 ? $@"AND sh.""Urun_ID"" = {urunId}" : "")}
                ORDER BY sh.""CreDate"" DESC")).ToArray();
        }

        /// <summary>
        /// Stok hareketi kaydeder ve ürün stok miktarını günceller.
        /// HareketTipi: 1=Giriş, 2=Çıkış
        /// </summary>
        public async Task<DataResult<int>> SetStokHareketi(int urunId, short hareketTipi, decimal miktar, string? not, int creUser)
        {
            if (urunId <= 0)
                return new() { SonucKodu = -1, SonucAciklama = "Geçersiz ürün." };
            if (miktar <= 0)
                return new() { SonucKodu = -1, SonucAciklama = "Miktar sıfırdan büyük olmalıdır." };

            // Çıkış ise yeterli stok var mı?
            if (hareketTipi == 2)
            {
                var mevcutStok = await SQLExecuteScalar<decimal>(
                    @"SELECT ""Stok"" FROM ""Urunler"" WHERE ""ID"" = @ID AND ""DelUser"" IS NULL",
                    new { ID = urunId });

                if (mevcutStok < miktar)
                    return new() { SonucKodu = -2, SonucAciklama = $"Yetersiz stok. Mevcut stok: {mevcutStok:N2}" };
            }

            // Stok hareketi ekle
            var hareketId = await SQLExecuteScalar<int>(
                @"INSERT INTO ""StokHareketleri"" (""Urun_ID"", ""HareketTipi"", ""Miktar"", ""Not"", ""CreUser"", ""CreDate"")
                  VALUES (@Urun_ID, @HareketTipi, @Miktar, @Not, @CreUser, now())
                  RETURNING ""ID""",
                new { Urun_ID = urunId, HareketTipi = hareketTipi, Miktar = miktar, Not = not, CreUser = creUser });

            // Ürün stok güncelle
            var stokDegisim = hareketTipi == 1 ? miktar : -miktar;
            await SQLExecute(
                @"UPDATE ""Urunler""
                  SET ""Stok"" = ""Stok"" + @Degisim, ""UpdUser"" = @UpdUser, ""UpdDate"" = now()
                  WHERE ""ID"" = @ID",
                new { Degisim = stokDegisim, UpdUser = creUser, ID = urunId });

            return new() { Data = hareketId };
        }

        public async Task<ItemUrun?> GetUrunByBarkod(string barkodNo)
        {
            if (string.IsNullOrWhiteSpace(barkodNo)) return null;
            return await SQLQueryFirstOrDefaultAsync<ItemUrun>(
                @"SELECT ""ID"", ""UrunKodu"", ""Kategori_ID"", ""BarkodNo"", ""ResimYolu"", ""Ad"", ""Aciklama"",
                         ""Birim_ID"", ""Stok"", ""KritikStokSeviyesi"", ""CreUser"", ""CreDate"",
                         ""UpdUser"", ""UpdDate"", ""DelUser"", ""DelDate""
                  FROM ""Urunler""
                  WHERE ""BarkodNo"" = @BarkodNo AND ""DelUser"" IS NULL",
                new { BarkodNo = barkodNo.Trim() });
        }

        public async Task<DashboardOzet> GetDashboardOzet()
        {
            var ozet = await SQLQueryFirstOrDefaultAsync<DashboardOzet>(@"
                SELECT
                    (SELECT COUNT(*) FROM ""Urunler""         WHERE ""DelUser"" IS NULL) AS ""ToplamUrun"",
                    (SELECT COUNT(*) FROM ""Kategoriler""     WHERE ""DelUser"" IS NULL) AS ""ToplamKategori"",
                    (SELECT COUNT(*) FROM ""Kullanicilar""    WHERE ""DelUser"" IS NULL) AS ""ToplamKullanici"",
                    (SELECT COUNT(*) FROM ""StokHareketleri"" WHERE ""DelUser"" IS NULL) AS ""ToplamHareket"",
                    (SELECT COUNT(*) FROM ""StokHareketleri"" WHERE ""DelUser"" IS NULL AND DATE(""CreDate"") = CURRENT_DATE) AS ""BugunkuHareket"",
                    (SELECT COUNT(*) FROM ""Urunler""         WHERE ""DelUser"" IS NULL AND ""Stok"" <= ""KritikStokSeviyesi"") AS ""KritikStokSayisi"",
                    (SELECT COALESCE(SUM(""Miktar""),0) FROM ""StokHareketleri"" WHERE ""DelUser"" IS NULL AND ""HareketTipi"" = 1 AND DATE(""CreDate"") = CURRENT_DATE) AS ""BugunkuGiris"",
                    (SELECT COALESCE(SUM(""Miktar""),0) FROM ""StokHareketleri"" WHERE ""DelUser"" IS NULL AND ""HareketTipi"" = 2 AND DATE(""CreDate"") = CURRENT_DATE) AS ""BugunkuCikis""
            ");
            return ozet ?? new DashboardOzet();
        }

        public async Task<ItemUrun[]> GetKritikStokUrunler()
        {
            return (await SQLQueryAsync<ItemUrun>(@"
                SELECT ""ID"", ""UrunKodu"", ""Kategori_ID"", ""BarkodNo"", ""ResimYolu"", ""Ad"", ""Aciklama"",
                       ""Birim_ID"", ""Stok"", ""KritikStokSeviyesi"", ""CreUser"", ""CreDate"",
                       ""UpdUser"", ""UpdDate"", ""DelUser"", ""DelDate""
                FROM ""Urunler""
                WHERE ""DelUser"" IS NULL AND ""Stok"" <= ""KritikStokSeviyesi""
                ORDER BY ""Stok"" ASC
                LIMIT 10")).ToArray();
        }

        public async Task<ItemStokHareketleri[]> GetSonHareketler(int topN = 10)
        {
            return (await SQLQueryAsync<ItemStokHareketleri>($@"
                SELECT sh.""ID"", sh.""Urun_ID"", sh.""HareketTipi"", sh.""Miktar"", sh.""Not"",
                       sh.""CreUser"", sh.""CreDate"", sh.""UpdUser"", sh.""UpdDate"", sh.""DelUser"", sh.""DelDate"",
                       u.""Ad"" AS ""UrunAd"", u.""UrunKodu""
                FROM ""StokHareketleri"" sh
                LEFT JOIN ""Urunler"" u ON u.""ID"" = sh.""Urun_ID""
                WHERE sh.""DelUser"" IS NULL
                ORDER BY sh.""CreDate"" DESC
                LIMIT {topN}")).ToArray();
        }

        #endregion

        #region KullaniciTip

        public async Task<ItemKullaniciTip[]> GetKullaniciTip(int ID = 0)
        {
            return (await SQLQueryAsync<ItemKullaniciTip>($@"SELECT ""ID"", ""Ad"", ""Aktif"", ""CreUser"", ""CreDate"", ""UpdUser"", ""UpdDate"", ""DelUser"", ""DelDate""
        FROM ""KullaniciTip""
        WHERE ""DelUser"" IS NULL
        {(ID > 0 ? $@"AND ""ID"" = {ID}" : "")}
        ORDER BY ""Ad"" ")).ToArray();
        }

        public async Task<DataResult<int>> SetKullaniciTip(ItemKullaniciTip item)
        {
            if (item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                item.ID = await SQLExecuteScalar<int>(@"INSERT INTO ""KullaniciTip"" (""Ad"", ""Aktif"", ""CreUser"", ""CreDate"")
                 VALUES (@Ad, @Aktif, @CreUser, now())
                 RETURNING ""ID""", item);
            }
            else if (!item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""KullaniciTip"" 
                     SET ""Ad"" = @Ad, ""Aktif"" = @Aktif, ""UpdUser"" = @UpdUser, ""UpdDate"" = now()
                     WHERE ""ID"" = @ID", item);
            }
            else if (!item.ID.Equals(0) && !item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""KullaniciTip""
                     SET ""DelUser"" = @DelUser, ""DelDate"" = now()
                     WHERE ""ID"" = @ID", item);
            }
            return new() { Data = item.ID };
        }

        #endregion

        #region Kullanici

        /// <summary>
        /// Aktif (silinmemiş) kullanıcıları döner. ID > 0 ise tek kullanıcıyı filtreler.
        /// </summary>
        public async Task<ItemKullanicilar[]> GetKullanici(int ID = 0)
        {
            return (await SQLQueryAsync<ItemKullanicilar>($@"SELECT ""ID"", ""KullaniciTip_ID"", ""Ad"", ""Soyad"", ""Eposta"", ""Sifre"", ""Aktif"", ""CreUser"", ""CreDate"", ""UpdUser"", ""UpdDate"", ""DelUser"", ""DelDate""
        FROM ""Kullanicilar"" 
        WHERE ""DelUser"" IS NULL 
        {(ID > 0 ? $@"AND ""ID"" = {ID}" : "")} 
        ORDER BY ""ID"" ")).ToArray();
        }

        /// <summary>
        /// E-posta adresine göre aktif kullanıcıyı döner.
        /// </summary>
        public async Task<ItemKullanicilar?> GetKullaniciByEposta(string eposta)
        {
            if (string.IsNullOrWhiteSpace(eposta)) return null;

            return await SQLQueryFirstOrDefaultAsync<ItemKullanicilar>(
                @"SELECT * FROM ""Kullanicilar""
                  WHERE LOWER(""Eposta"") = LOWER(@Eposta) AND ""DelUser"" IS NULL",
                new { Eposta = eposta.Trim() });
        }

        /// <summary>
        /// Kullanıcı Insert / Update / Soft-Delete.
        /// </summary>
        public async Task<DataResult<int>> SetKullanici(ItemKullanicilar item)
        {
            if (item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                if (string.IsNullOrWhiteSpace(item.Eposta))
                    return new() { SonucKodu = -1, SonucAciklama = "E-posta zorunludur." };
                if (string.IsNullOrWhiteSpace(item.Sifre))
                    return new() { SonucKodu = -1, SonucAciklama = "Şifre zorunludur." };

                var epostaVar = await SQLExecuteScalar<int>(
                    @"SELECT COUNT(*) FROM ""Kullanicilar""
                      WHERE LOWER(""Eposta"") = LOWER(@Eposta) AND ""DelUser"" IS NULL",
                    new { item.Eposta });

                if (epostaVar > 0)
                    return new() { SonucKodu = -1, SonucAciklama = "Bu e-posta ile kayıtlı bir kullanıcı zaten mevcut." };

                item.Sifre = PasswordHasher.Hash(item.Sifre);

                item.ID = await SQLExecuteScalar<int>(
                    @"INSERT INTO ""Kullanicilar"" (""KullaniciTip_ID"", ""Ad"", ""Soyad"", ""Eposta"", ""Sifre"", ""Aktif"", ""CreUser"", ""CreDate"")
                      VALUES (@KullaniciTip_ID, @Ad, @Soyad, @Eposta, @Sifre, @Aktif, @CreUser, now())
                      RETURNING ""ID""", item);
            }
            else if (!item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                if (!string.IsNullOrWhiteSpace(item.Sifre) && !IsHashed(item.Sifre))
                    item.Sifre = PasswordHasher.Hash(item.Sifre);

                await SQLExecute(@"UPDATE ""Kullanicilar""
                     SET ""KullaniciTip_ID"" = @KullaniciTip_ID,
                         ""Ad""              = @Ad,
                         ""Soyad""           = @Soyad,
                         ""Eposta""          = @Eposta,
                         ""Sifre""           = COALESCE(NULLIF(@Sifre, ''), ""Sifre""),
                         ""Aktif""           = @Aktif,
                         ""UpdUser""         = @UpdUser,
                         ""UpdDate""         = now()
                     WHERE ""ID"" = @ID", item);
            }
            else if (!item.ID.Equals(0) && !item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""Kullanicilar""
                     SET ""DelUser"" = @DelUser, ""DelDate"" = now() 
                     WHERE ""ID"" = @ID", item);
            }
            return new() { Data = item.ID };
        }

        public async Task<DataResult<ItemKullanicilar>> LoginKullanici(string eposta, string sifre)
        {
            if (string.IsNullOrWhiteSpace(eposta) || string.IsNullOrWhiteSpace(sifre))
                return new() { SonucKodu = -1, SonucAciklama = "E-posta ve şifre zorunludur." };

            var user = await GetKullaniciByEposta(eposta);
            if (user is null)
                return new() { SonucKodu = -1, SonucAciklama = "E-posta veya şifre hatalı." };

            if (user.Aktif == false)
                return new() { SonucKodu = -2, SonucAciklama = "Hesabınız pasif durumda. Lütfen yönetici ile iletişime geçin." };

            if (!PasswordHasher.Verify(sifre, user.Sifre))
                return new() { SonucKodu = -1, SonucAciklama = "E-posta veya şifre hatalı." };

            return new() { Data = user, SonucKodu = 0 };
        }

        public async Task<DataResult<int>> RegisterKullanici(string ad, string soyad, string eposta, string sifre, int kullaniciTipId = 0)
        {
            if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(soyad))
                return new() { SonucKodu = -1, SonucAciklama = "Ad ve soyad zorunludur." };
            if (string.IsNullOrWhiteSpace(eposta))
                return new() { SonucKodu = -1, SonucAciklama = "E-posta zorunludur." };
            if (string.IsNullOrWhiteSpace(sifre) || sifre.Length < 6)
                return new() { SonucKodu = -1, SonucAciklama = "Şifre en az 6 karakter olmalıdır." };

            var item = new ItemKullanicilar
            {
                ID = 0,
                KullaniciTip_ID = kullaniciTipId,
                Ad = ad.Trim(),
                Soyad = soyad.Trim(),
                Eposta = eposta.Trim(),
                Sifre = sifre,
                Aktif = true,
                CreUser = 0,
                DelUser = 0
            };

            return await SetKullanici(item);
        }

        public async Task<DataResult<int>> SifreDegistir(int kullaniciId, string eskiSifre, string yeniSifre)
        {
            if (kullaniciId <= 0)
                return new() { SonucKodu = -1, SonucAciklama = "Geçersiz kullanıcı." };
            if (string.IsNullOrWhiteSpace(yeniSifre) || yeniSifre.Length < 6)
                return new() { SonucKodu = -1, SonucAciklama = "Yeni şifre en az 6 karakter olmalıdır." };

            var user = (await GetKullanici(kullaniciId)).FirstOrDefault();
            if (user is null)
                return new() { SonucKodu = -1, SonucAciklama = "Kullanıcı bulunamadı." };

            if (!PasswordHasher.Verify(eskiSifre ?? string.Empty, user.Sifre))
                return new() { SonucKodu = -1, SonucAciklama = "Mevcut şifre hatalı." };

            var yeniHash = PasswordHasher.Hash(yeniSifre);
            await SQLExecute(
                @"UPDATE ""Kullanicilar""
                  SET ""Sifre"" = @Sifre, ""UpdUser"" = @UpdUser, ""UpdDate"" = now()
                  WHERE ""ID"" = @ID",
                new { ID = kullaniciId, Sifre = yeniHash, UpdUser = kullaniciId });

            return new() { Data = kullaniciId };
        }

        private static bool IsHashed(string value) =>
            !string.IsNullOrEmpty(value) &&
            value.Count(c => c == '.') == 2 &&
            int.TryParse(value.Split('.', 2)[0], out _);

        #endregion

        #region Form (Menü)

        /// <summary>
        /// Aktif form/sayfa kayıtlarını sırasına göre getirir.
        /// </summary>
        public async Task<ItemForm[]> GetFormlar()
        {
            return (await SQLQueryAsync<ItemForm>(@"
                SELECT ""ID"", ""Ad"", ""IsMenu"", ""UstMenu_ID"", ""Sira"", ""SayfaURL"", ""Icon"",
                       ""CreUser"", ""CreDate"", ""UpdUser"", ""UpdDate"", ""DelUser"", ""DelDate""
                FROM ""Form""
                WHERE ""DelUser"" IS NULL AND ""IsMenu"" = true
                ORDER BY ""Sira"", ""Ad""")).ToArray();
        }

        /// <summary>
        /// Tüm formları döner (yetki yönetim sayfaları için).
        /// </summary>
        public async Task<ItemForm[]> GetTumFormlar()
        {
            return (await SQLQueryAsync<ItemForm>(@"
                SELECT ""ID"", ""Ad"", ""IsMenu"", ""UstMenu_ID"", ""Sira"", ""SayfaURL"", ""Icon"",
                       ""CreUser"", ""CreDate"", ""UpdUser"", ""UpdDate"", ""DelUser"", ""DelDate""
                FROM ""Form""
                WHERE ""DelUser"" IS NULL
                ORDER BY ""Sira"", ""Ad""")).ToArray();
        }

        #endregion

        #region Yetki

        // ── Kullanıcı bazlı yetki ──────────────────────────────────────

        /// <summary>
        /// Belirli kullanıcının tüm form yetkilerini döner.
        /// </summary>
        public async Task<ItemKullaniciDetay[]> GetKullaniciFormYetki(int kullaniciId)
        {
            return (await SQLQueryAsync<ItemKullaniciDetay>(@"
                SELECT fy.""ID"", fy.""Kullanici_ID"", fy.""Form_ID"", fy.""Yetki"",
                       fy.""CreUser"", fy.""CreDate"", fy.""UpdUser"", fy.""UpdDate"",
                       fy.""DelUser"", fy.""DelDate""
                FROM ""FormYetki"" fy
                WHERE fy.""DelUser"" IS NULL AND fy.""Kullanici_ID"" = @KullaniciId
                ORDER BY fy.""Form_ID""",
                new { KullaniciId = kullaniciId })).ToArray();
        }

        /// <summary>
        /// Kullanıcının belirli bir formdaki etkin yetkisini döner.
        /// Kullanıcı bazlı ve tip bazlı yetkilerden büyük olanı (MAX) geçerlidir.
        /// </summary>
        public async Task<int> GetKullaniciFormYetkiSeviyesi(int kullaniciId, int formId)
        {
            var kullaniciBazli = await SQLExecuteScalar<int?>(@"
                SELECT ""Yetki"" FROM ""FormYetki""
                WHERE ""DelUser"" IS NULL AND ""Kullanici_ID"" = @KullaniciId AND ""Form_ID"" = @FormId
                LIMIT 1",
                new { KullaniciId = kullaniciId, FormId = formId });

            var tipBazli = await SQLExecuteScalar<int?>(@"
                SELECT tfy.""Yetki""
                FROM ""KullaniciTipFormYetki"" tfy
                JOIN ""Kullanicilar"" k ON k.""ID"" = @KullaniciId
                WHERE tfy.""DelUser"" IS NULL
                  AND tfy.""KullaniciTip_ID"" = k.""KullaniciTip_ID""
                  AND tfy.""Form_ID"" = @FormId
                LIMIT 1",
                new { KullaniciId = kullaniciId, FormId = formId });

            return Math.Max(kullaniciBazli ?? 0, tipBazli ?? 0);
        }

        /// <summary>
        /// Kullanıcının erişebildiği (etkin yetki >= minYetki) formların ID listesini döner.
        /// Etkin yetki = MAX(kullanıcı bazlı, tip bazlı).
        /// </summary>
        public async Task<int[]> GetKullaniciErisebilirFormIdler(int kullaniciId, int minYetki = 1)
        {
            // Kullanıcı bazlı yeterli yetki
            var kullaniciBazli = (await SQLQueryAsync<int>(@"
                SELECT ""Form_ID"" FROM ""FormYetki""
                WHERE ""DelUser"" IS NULL AND ""Kullanici_ID"" = @KullaniciId AND ""Yetki"" >= @MinYetki",
                new { KullaniciId = kullaniciId, MinYetki = minYetki })).ToHashSet();

            // Tip bazlı yeterli yetki
            var tipBazli = await SQLQueryAsync<int>(@"
                SELECT tfy.""Form_ID""
                FROM ""KullaniciTipFormYetki"" tfy
                JOIN ""Kullanicilar"" k ON k.""KullaniciTip_ID"" = tfy.""KullaniciTip_ID""
                WHERE tfy.""DelUser"" IS NULL AND k.""ID"" = @KullaniciId AND tfy.""Yetki"" >= @MinYetki",
                new { KullaniciId = kullaniciId, MinYetki = minYetki });

            // Her iki kaynaktan gelenler birleşir (union): her iki kaynaktan gelen yeterli yetki geçerlidir
            foreach (var id in tipBazli) kullaniciBazli.Add(id);
            return kullaniciBazli.ToArray();
        }

        /// <summary>
        /// Kullanıcı-form yetkisini set eder (insert veya update). Yetki=0 ise kaydı siler.
        /// </summary>
        public async Task SetKullaniciFormYetki(int kullaniciId, int formId, int yetki, int islemKullanici)
        {
            var mevcutId = await SQLExecuteScalar<int?>(@"
                SELECT ""ID"" FROM ""FormYetki""
                WHERE ""Kullanici_ID"" = @KullaniciId AND ""Form_ID"" = @FormId AND ""DelUser"" IS NULL
                LIMIT 1",
                new { KullaniciId = kullaniciId, FormId = formId });

            if (mevcutId.HasValue)
            {
                if (yetki == 0)
                {
                    await SQLExecute(@"UPDATE ""FormYetki"" SET ""DelUser"" = @Del, ""DelDate"" = now() WHERE ""ID"" = @ID",
                        new { Del = islemKullanici, ID = mevcutId.Value });
                }
                else
                {
                    await SQLExecute(@"UPDATE ""FormYetki"" SET ""Yetki"" = @Yetki, ""UpdUser"" = @Upd, ""UpdDate"" = now() WHERE ""ID"" = @ID",
                        new { Yetki = yetki, Upd = islemKullanici, ID = mevcutId.Value });
                }
            }
            else if (yetki > 0)
            {
                await SQLExecute(@"INSERT INTO ""FormYetki"" (""Kullanici_ID"", ""Form_ID"", ""Yetki"", ""CreUser"", ""CreDate"")
                                   VALUES (@KullaniciId, @FormId, @Yetki, @Cre, now())",
                    new { KullaniciId = kullaniciId, FormId = formId, Yetki = yetki, Cre = islemKullanici });
            }
        }

        // ── Kullanıcı tipi bazlı yetki ─────────────────────────────────

        /// <summary>
        /// Belirli kullanıcı tipinin tüm form yetkilerini döner.
        /// </summary>
        public async Task<ItemKullaniciTipDetay[]> GetKullaniciTipFormYetki(int kullaniciTipId)
        {
            return (await SQLQueryAsync<ItemKullaniciTipDetay>(@"
                SELECT tfy.""ID"", tfy.""KullaniciTip_ID"", tfy.""Form_ID"", tfy.""Yetki"",
                       tfy.""CreUser"", tfy.""CreDate"", tfy.""UpdUser"", tfy.""UpdDate"",
                       tfy.""DelUser"", tfy.""DelDate""
                FROM ""KullaniciTipFormYetki"" tfy
                WHERE tfy.""DelUser"" IS NULL AND tfy.""KullaniciTip_ID"" = @TipId
                ORDER BY tfy.""Form_ID""",
                new { TipId = kullaniciTipId })).ToArray();
        }

        /// <summary>
        /// Kullanıcı tipi-form yetkisini set eder (insert veya update). Yetki=0 ise kaydı siler.
        /// </summary>
        public async Task SetKullaniciTipFormYetki(int kullaniciTipId, int formId, int yetki, int islemKullanici)
        {
            var mevcutId = await SQLExecuteScalar<int?>(@"
                SELECT ""ID"" FROM ""KullaniciTipFormYetki""
                WHERE ""KullaniciTip_ID"" = @TipId AND ""Form_ID"" = @FormId AND ""DelUser"" IS NULL
                LIMIT 1",
                new { TipId = kullaniciTipId, FormId = formId });

            if (mevcutId.HasValue)
            {
                if (yetki == 0)
                {
                    await SQLExecute(@"UPDATE ""KullaniciTipFormYetki"" SET ""DelUser"" = @Del, ""DelDate"" = now() WHERE ""ID"" = @ID",
                        new { Del = islemKullanici, ID = mevcutId.Value });
                }
                else
                {
                    await SQLExecute(@"UPDATE ""KullaniciTipFormYetki"" SET ""Yetki"" = @Yetki, ""UpdUser"" = @Upd, ""UpdDate"" = now() WHERE ""ID"" = @ID",
                        new { Yetki = yetki, Upd = islemKullanici, ID = mevcutId.Value });
                }
            }
            else if (yetki > 0)
            {
                await SQLExecute(@"INSERT INTO ""KullaniciTipFormYetki"" (""KullaniciTip_ID"", ""Form_ID"", ""Yetki"", ""CreUser"", ""CreDate"")
                                   VALUES (@TipId, @FormId, @Yetki, @Cre, now())",
                    new { TipId = kullaniciTipId, FormId = formId, Yetki = yetki, Cre = islemKullanici });
            }
        }

        #endregion
    }
}
