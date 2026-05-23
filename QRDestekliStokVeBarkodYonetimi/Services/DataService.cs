using System.Linq;
using System.Security.Cryptography;
using Dapper;
using Npgsql;
using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    public class DataService : DBClass, IYetkiDataAccess
    {
        private readonly EmailService _email;
        private readonly SifreDegistirDogrulamaService _sifreDogrulama;
        private readonly KullaniciHesapOnayTokenService _hesapOnayToken;

        public DataService(
            string connectionString,
            EmailService email,
            SifreDegistirDogrulamaService sifreDogrulama,
            KullaniciHesapOnayTokenService hesapOnayToken) : base(connectionString)
        {
            _email = email;
            _sifreDogrulama = sifreDogrulama;
            _hesapOnayToken = hesapOnayToken;
        }

        #region Kategori

        public async Task<ItemKategori[]> GetKategori(int ID = 0)
        {
            return (await SQLQueryAsync<ItemKategori>($@"SELECT ""ID"", ""ResimYolu"",""Ad"", ""Aciklama"", ""Aktif"" FROM ""Kategoriler""         
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
                await SQLExecute(@"UPDATE ""Kategoriler"" 
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
        FROM ""Birim""
        WHERE ""DelUser"" IS NULL
        {(ID > 0 ? $@"AND ""ID"" = {ID}" : "")}
        ORDER BY ""Ad"" ")).ToArray();
        }

        public async Task<DataResult<int>> SetBirim(ItemBirim item)
        {
            if (item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                item.ID = await SQLExecuteScalar<int>(@"INSERT INTO ""Birim"" (""Ad"", ""Aktif"", ""CreUser"", ""CreDate"")
                 VALUES (@Ad, @Aktif, @CreUser, now())
                 RETURNING ""ID""", item);
            }
            else if (!item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""Birim""
                     SET ""Ad"" = @Ad, ""Aktif"" = @Aktif, ""UpdUser"" = @UpdUser, ""UpdDate"" = now()
                     WHERE ""ID"" = @ID", item);
            }
            else if (!item.ID.Equals(0) && !item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""Birim""
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

        /// <summary>
        /// Dashboard özet sayılarını yalnızca talep edilen modüller için sorgular (yetki ile uyumlu yükleme).
        /// </summary>
        public async Task<DashboardOzet> GetDashboardOzetAsync(
            bool urun,
            bool kategori,
            bool kullanici,
            bool stokHareketleri,
            bool stokGiris,
            bool stokCikis)
        {
            var ozet = new DashboardOzet();

            Task<DashboardOzet> tUrun = Task.FromResult(new DashboardOzet());
            Task<DashboardOzet> tStok = Task.FromResult(new DashboardOzet());
            Task<int> tKat = Task.FromResult(0);
            Task<int> tKul = Task.FromResult(0);
            Task<decimal> tGiris = Task.FromResult(0m);
            Task<decimal> tCikis = Task.FromResult(0m);

            if (urun)
            {
                tUrun = SQLQueryFirstOrDefaultAsync<DashboardOzet>(@"
                    SELECT
                        (SELECT COUNT(*) FROM ""Urunler"" WHERE ""DelUser"" IS NULL) AS ""ToplamUrun"",
                        (SELECT COUNT(*) FROM ""Urunler"" WHERE ""DelUser"" IS NULL AND ""Stok"" <= ""KritikStokSeviyesi"") AS ""KritikStokSayisi""");
            }

            if (stokHareketleri)
            {
                tStok = SQLQueryFirstOrDefaultAsync<DashboardOzet>(@"
                    SELECT
                        (SELECT COUNT(*) FROM ""StokHareketleri"" WHERE ""DelUser"" IS NULL) AS ""ToplamHareket"",
                        (SELECT COUNT(*) FROM ""StokHareketleri"" WHERE ""DelUser"" IS NULL AND DATE(""CreDate"") = CURRENT_DATE) AS ""BugunkuHareket""");
            }

            if (kategori)
                tKat = SQLExecuteScalar<int>(@"SELECT COUNT(*)::int FROM ""Kategoriler"" WHERE ""DelUser"" IS NULL");

            if (kullanici)
                tKul = SQLExecuteScalar<int>(@"SELECT COUNT(*)::int FROM ""Kullanicilar"" WHERE ""DelUser"" IS NULL");

            if (stokGiris)
            {
                tGiris = SQLExecuteScalar<decimal>(
                    @"SELECT COALESCE(SUM(""Miktar""),0) FROM ""StokHareketleri""
                      WHERE ""DelUser"" IS NULL AND ""HareketTipi"" = 1 AND DATE(""CreDate"") = CURRENT_DATE");
            }

            if (stokCikis)
            {
                tCikis = SQLExecuteScalar<decimal>(
                    @"SELECT COALESCE(SUM(""Miktar""),0) FROM ""StokHareketleri""
                      WHERE ""DelUser"" IS NULL AND ""HareketTipi"" = 2 AND DATE(""CreDate"") = CURRENT_DATE");
            }

            await Task.WhenAll(tUrun, tStok, tKat, tKul, tGiris, tCikis);

            if (urun)
            {
                var urRow = await tUrun;
                if (urRow is not null)
                {
                    ozet.ToplamUrun = urRow.ToplamUrun;
                    ozet.KritikStokSayisi = urRow.KritikStokSayisi;
                }
            }

            if (stokHareketleri)
            {
                var stRow = await tStok;
                if (stRow is not null)
                {
                    ozet.ToplamHareket = stRow.ToplamHareket;
                    ozet.BugunkuHareket = stRow.BugunkuHareket;
                }
            }

            ozet.ToplamKategori = await tKat;
            ozet.ToplamKullanici = await tKul;
            ozet.BugunkuGiris = await tGiris;
            ozet.BugunkuCikis = await tCikis;

            return ozet;
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
            return (await SQLQueryAsync<ItemKullaniciTip>($@"SELECT ""ID"", ""Ad"", ""Aktif"", ""Varsayilan"", ""CreUser"", ""CreDate"", ""UpdUser"", ""UpdDate"", ""DelUser"", ""DelDate""
        FROM ""KullaniciTip""
        WHERE ""DelUser"" IS NULL
        {(ID > 0 ? $@"AND ""ID"" = {ID}" : "")}
        ORDER BY ""Ad"" ")).ToArray();
        }

        public async Task<DataResult<int>> SetKullaniciTip(ItemKullaniciTip item)
        {
            if (item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                item.ID = await SQLExecuteScalar<int>(@"INSERT INTO ""KullaniciTip"" (""Ad"", ""Aktif"", ""Varsayilan"", ""CreUser"", ""CreDate"")
                 VALUES (@Ad, @Aktif, @Varsayilan, @CreUser, now())
                 RETURNING ""ID""", item);

                if (item.Varsayilan)
                    await VarsayilanTekKaydiSenkronizeEtAsync(item.ID);
            }
            else if (!item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""KullaniciTip"" 
                     SET ""Ad"" = @Ad, ""Aktif"" = @Aktif, ""Varsayilan"" = @Varsayilan, ""UpdUser"" = @UpdUser, ""UpdDate"" = now()
                     WHERE ""ID"" = @ID", item);

                if (item.Varsayilan)
                    await VarsayilanTekKaydiSenkronizeEtAsync(item.ID);
            }
            else if (!item.ID.Equals(0) && !item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""KullaniciTip""
                     SET ""DelUser"" = @DelUser, ""DelDate"" = now()
                     WHERE ""ID"" = @ID", item);
            }
            return new() { Data = item.ID };
        }

        /// <summary>
        /// Silinmemiş tüm satırlarda yalnızca <paramref name="varsayilanKayitId"/> için Varsayilan=true olur.
        /// </summary>
        private async Task VarsayilanTekKaydiSenkronizeEtAsync(int varsayilanKayitId)
        {
            await SQLExecute(
                @"UPDATE ""KullaniciTip"" SET ""Varsayilan"" = (""ID"" = @KeepId) WHERE ""DelUser"" IS NULL",
                new { KeepId = varsayilanKayitId });
        }

        /// <summary>Aktif ve varsayılan işaretli kullanıcı tipi ID'si (yeni kayıt için).</summary>
        public async Task<int?> GetVarsayilanKullaniciTipIdAsync()
        {
            return await SQLQueryFirstOrDefaultAsync<int?>(
                @"SELECT ""ID"" FROM ""KullaniciTip""
                  WHERE ""Varsayilan"" = true AND ""Aktif"" = true AND ""DelUser"" IS NULL
                  ORDER BY ""ID"" LIMIT 1");
        }

        #endregion

        #region Kullanici

        /// <summary>
        /// Aktif (silinmemiş) kullanıcıları döner. ID > 0 ise tek kullanıcıyı filtreler.
        /// </summary>
        public async Task<ItemKullanicilar[]> GetKullanici(int ID = 0)
        {
            return (await SQLQueryAsync<ItemKullanicilar>($@"SELECT ""ID"", ""KullaniciTip_ID"", ""Ad"", ""Soyad"", ""Eposta"", ""Sifre"", ""ProfilResmi"", ""Aktif"", ""CreUser"", ""CreDate"", ""UpdUser"", ""UpdDate"", ""DelUser"", ""DelDate""
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
                    item.Sifre = PasswordHasher.Hash(Guid.NewGuid().ToString("N") + Random.Shared.NextInt64());

                var epostaVar = await SQLExecuteScalar<int>(
                    @"SELECT COUNT(*) FROM ""Kullanicilar""
                      WHERE LOWER(""Eposta"") = LOWER(@Eposta) AND ""DelUser"" IS NULL",
                    new { item.Eposta });

                if (epostaVar > 0)
                    return new() { SonucKodu = -1, SonucAciklama = "Bu e-posta ile kayıtlı bir kullanıcı zaten mevcut." };

                item.Sifre = PasswordHasher.Hash(item.Sifre);

                item.ID = await SQLExecuteScalar<int>(
                    @"INSERT INTO ""Kullanicilar"" (""KullaniciTip_ID"", ""Ad"", ""Soyad"", ""Eposta"", ""Sifre"", ""ProfilResmi"", ""Aktif"", ""CreUser"", ""CreDate"")
                      VALUES (@KullaniciTip_ID, @Ad, @Soyad, @Eposta, @Sifre, @ProfilResmi, @Aktif, @CreUser, now())
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
                         ""ProfilResmi""     = @ProfilResmi,
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

            if (kullaniciTipId <= 0)
            {
                var varsayilanId = await GetVarsayilanKullaniciTipIdAsync();
                if (!varsayilanId.HasValue || varsayilanId.Value <= 0)
                    return new()
                    {
                        SonucKodu = -1,
                        SonucAciklama =
                            "Varsayılan kullanıcı tipi tanımlı değil veya pasif. Kayıt şu an mümkün değil; lütfen yöneticiye başvurun."
                    };
                kullaniciTipId = varsayilanId.Value;
            }

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

        /// <summary>
        /// Profil şifre değişimi: eski şifre doğruysa OTP üretilir, kayıtlı e-postaya gönderilir;
        /// onay için hash bekletilir.
        /// </summary>
        public async Task<DataResult<int>> ProfilSifreDegisimKoduGonder(int kullaniciId, string eskiSifre,
            string yeniSifre, CancellationToken ct = default)
        {
            if (kullaniciId <= 0)
                return new() { SonucKodu = -1, SonucAciklama = "Geçersiz kullanıcı." };
            if (string.IsNullOrWhiteSpace(yeniSifre) || yeniSifre.Length < 6)
                return new() { SonucKodu = -1, SonucAciklama = "Yeni şifre en az 6 karakter olmalıdır." };

            var user = (await GetKullanici(kullaniciId)).FirstOrDefault();
            if (user is null)
                return new() { SonucKodu = -1, SonucAciklama = "Kullanıcı bulunamadı." };

            if (string.IsNullOrWhiteSpace(user.Eposta))
                return new() { SonucKodu = -1, SonucAciklama = "Kayıtlı e-posta adresiniz bulunamadı." };

            if (!PasswordHasher.Verify(eskiSifre ?? string.Empty, user.Sifre))
                return new() { SonucKodu = -1, SonucAciklama = "Mevcut şifre hatalı." };

            var yeniHash = PasswordHasher.Hash(yeniSifre);
            var kod = _sifreDogrulama.OlusturVeKaydet(kullaniciId, yeniHash);
            var ad = (user.Ad ?? string.Empty).Trim();

            var body =
                $@"<div style='font-family:Segoe UI, Arial, sans-serif; max-width:520px; margin:0 auto;'>
                    <h2 style='color:#1f6feb; margin-bottom:0.5rem;'>Şifre Değişikliği Doğrulama</h2>
                    <p>Merhaba <b>{ad}</b>,</p>
                    <p>Hesabınızdaki şifreyi profil üzerinden değiştirmek için doğrulama kodunuz:</p>
                    <div style='font-size:1.8rem; font-weight:700; letter-spacing:8px;
                                background:#f5f7fa; padding:1rem; text-align:center;
                                border-radius:8px; color:#1f3a93; margin:1rem 0;'>{kod}</div>
                    <p style='color:#666; font-size:0.9rem;'>Bu kod 10 dakika geçerlidir.
                       Bu işlemi siz yapmadıysanız bu maili dikkate almayın.</p>
                </div>";

            var mailOk = await _email.SendAsync(user.Eposta.Trim(), "Şifre Değişikliği Doğrulama Kodu", body,
                ct);
            if (!mailOk)
            {
                _sifreDogrulama.Iptal(kullaniciId);
                return new()
                {
                    SonucKodu = -1,
                    SonucAciklama =
                        "Doğrulama maili gönderilemedi. SMTP ayarlarınızı kontrol edin veya tekrar deneyin."
                };
            }

            return new() { Data = kullaniciId };
        }

        /// <summary>Profilde şifre OTP akışında bekleyen kodu iptal eder.</summary>
        public void ProfilSifreDegisimKoduIptal(int kullaniciId) =>
            _sifreDogrulama.Iptal(kullaniciId);

        /// <summary>OTP doğrulandığında bekleyen yeni şifre hash'ini veritabanına yazar.</summary>
        public async Task<DataResult<int>> ProfilSifreDegisimKoduOnayla(int kullaniciId, string? kod)
        {
            if (kullaniciId <= 0)
                return new() { SonucKodu = -1, SonucAciklama = "Geçersiz kullanıcı." };

            var user = (await GetKullanici(kullaniciId)).FirstOrDefault();
            if (user is null)
                return new() { SonucKodu = -1, SonucAciklama = "Kullanıcı bulunamadı." };

            var (ok, yeniHash) = _sifreDogrulama.Dogrula(kullaniciId, kod);
            if (!ok || string.IsNullOrEmpty(yeniHash))
                return new()
                {
                    SonucKodu = -1,
                    SonucAciklama = "Kod hatalı veya süresi dolmuş. Lütfen yeni kod gönderin."
                };

            await SQLExecute(
                @"UPDATE ""Kullanicilar""
                  SET ""Sifre"" = @Sifre, ""UpdUser"" = @UpdUser, ""UpdDate"" = now()
                  WHERE ""ID"" = @ID",
                new { ID = kullaniciId, Sifre = yeniHash, UpdUser = kullaniciId });

            return new() { Data = kullaniciId };
        }

        /// <summary>
        /// E-posta sıfırlama token'ı doğrulandıktan sonra şifreyi günceller (eski şifre kontrolü yok).
        /// </summary>
        public async Task<DataResult<int>> SifreSifirlaTokenIle(int kullaniciId, string yeniSifre)
        {
            if (kullaniciId <= 0)
                return new() { SonucKodu = -1, SonucAciklama = "Geçersiz kullanıcı." };
            if (string.IsNullOrWhiteSpace(yeniSifre) || yeniSifre.Length < 6)
                return new() { SonucKodu = -1, SonucAciklama = "Yeni şifre en az 6 karakter olmalıdır." };

            var user = (await GetKullanici(kullaniciId)).FirstOrDefault();
            if (user is null)
                return new() { SonucKodu = -1, SonucAciklama = "Kullanıcı bulunamadı." };
            if (user.Aktif == false)
                return new() { SonucKodu = -2, SonucAciklama = "Hesabınız pasif durumda. Şifre sıfırlanamaz." };

            var yeniHash = PasswordHasher.Hash(yeniSifre);
            await SQLExecute(
                @"UPDATE ""Kullanicilar""
                  SET ""Sifre"" = @Sifre, ""UpdUser"" = @UpdUser, ""UpdDate"" = now()
                  WHERE ""ID"" = @ID",
                new { ID = kullaniciId, Sifre = yeniHash, UpdUser = kullaniciId });

            return new() { Data = kullaniciId };
        }

        /// <summary>
        /// Pasif kullanıcıya hesap onay e-postası gönderir.
        /// </summary>
        public async Task<DataResult<int>> KullaniciOnayMailiGonder(int kullaniciId, string siteBaseUri)
        {
            if (kullaniciId <= 0)
                return new() { SonucKodu = -1, SonucAciklama = "Geçersiz kullanıcı." };

            var user = (await GetKullanici(kullaniciId)).FirstOrDefault();
            if (user is null)
                return new() { SonucKodu = -1, SonucAciklama = "Kullanıcı bulunamadı." };
            if (user.Aktif == true)
                return new() { SonucKodu = -1, SonucAciklama = "Kullanıcı zaten aktif." };
            if (string.IsNullOrWhiteSpace(user.Eposta))
                return new() { SonucKodu = -1, SonucAciklama = "Kullanıcının e-posta adresi tanımlı değil." };

            var token = _hesapOnayToken.OlusturKaydet(user.ID);
            var link = $"{siteBaseUri.TrimEnd('/')}/api/auth/confirm-account?token={Uri.EscapeDataString(token)}";
            var html =
                $"<p>Merhaba {System.Net.WebUtility.HtmlEncode(user.Ad)},</p>" +
                "<p>QR Destekli Stok Yönetimi sistemine bir hesap oluşturuldu.</p>" +
                "<p>Hesabınızı onaylamak ve giriş için geçici şifrenizi almak üzere aşağıdaki bağlantıya tıklayın. " +
                "Bağlantı 72 saat geçerlidir.</p>" +
                $"<p><a href=\"{link}\">Hesabımı onayla</a></p>" +
                "<p>Bu hesabı siz oluşturmadıysanız bu e-postayı yok sayabilirsiniz.</p>";

            var gonderildi = await _email.SendAsync(user.Eposta, "Hesap onayı", html);
            if (!gonderildi)
                return new() { SonucKodu = -1, SonucAciklama = "Onay e-postası gönderilemedi. SMTP ayarlarını kontrol edin." };

            return new() { Data = user.ID, SonucAciklama = "Onay e-postası gönderildi." };
        }

        /// <summary>
        /// Onay token'ı ile hesabı aktifleştirir ve geçici şifreyi e-posta ile gönderir.
        /// </summary>
        public async Task<DataResult<int>> KullaniciHesapOnayla(string? token)
        {
            if (!_hesapOnayToken.TryConsume(token, out var kullaniciId))
                return new() { SonucKodu = -1, SonucAciklama = "Onay bağlantısı geçersiz veya süresi dolmuş." };

            var user = (await GetKullanici(kullaniciId)).FirstOrDefault();
            if (user is null)
                return new() { SonucKodu = -1, SonucAciklama = "Kullanıcı bulunamadı." };
            if (string.IsNullOrWhiteSpace(user.Eposta))
                return new() { SonucKodu = -1, SonucAciklama = "Kullanıcının e-posta adresi tanımlı değil." };

            if (user.Aktif == true)
                return new() { SonucKodu = 0, SonucAciklama = "Hesabınız zaten onaylanmış. Giriş yapabilirsiniz." };

            var geciciSifre = GeciciSifreOlustur();
            var hash = PasswordHasher.Hash(geciciSifre);

            await SQLExecute(
                @"UPDATE ""Kullanicilar""
                  SET ""Sifre"" = @Sifre, ""Aktif"" = true, ""UpdUser"" = @UpdUser, ""UpdDate"" = now()
                  WHERE ""ID"" = @ID",
                new { ID = kullaniciId, Sifre = hash, UpdUser = kullaniciId });

            var html =
                $"<p>Merhaba {System.Net.WebUtility.HtmlEncode(user.Ad)},</p>" +
                "<p>Hesabınız onaylandı. Aşağıdaki geçici şifre ile giriş yapabilirsiniz:</p>" +
                $"<p style=\"font-size:1.1rem;font-weight:700;letter-spacing:1px;\">{System.Net.WebUtility.HtmlEncode(geciciSifre)}</p>" +
                "<p>Güvenliğiniz için giriş yaptıktan sonra profil sayfasından şifrenizi değiştirmenizi öneririz.</p>";

            var gonderildi = await _email.SendAsync(user.Eposta, "Hesabınız onaylandı — geçici şifre", html);
            if (!gonderildi)
            {
                return new()
                {
                    SonucKodu = -1,
                    SonucAciklama =
                        "Hesap onaylandı ancak geçici şifre e-postası gönderilemedi. Lütfen yöneticinizle iletişime geçin."
                };
            }

            return new() { Data = kullaniciId, SonucAciklama = "Hesabınız onaylandı. Geçici şifreniz e-posta adresinize gönderildi." };
        }

        private static string GeciciSifreOlustur()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            Span<char> buffer = stackalloc char[12];
            Span<byte> random = stackalloc byte[12];
            RandomNumberGenerator.Fill(random);
            for (var i = 0; i < buffer.Length; i++)
                buffer[i] = chars[random[i] % chars.Length];
            return new string(buffer);
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
                FROM ""KullaniciDetay"" fy
                WHERE fy.""DelUser"" IS NULL AND fy.""Kullanici_ID"" = @KullaniciId
                ORDER BY fy.""Form_ID""",
                new { KullaniciId = kullaniciId })).ToArray();
        }

        /// <summary>
        /// Kullanıcının belirli bir formdaki etkin yetkisini döner.
        ///
        /// Etkin Yetki Öncelik Kuralı:
        ///   1. Kullanıcıya özel yetki tanımlanmışsa → o geçerlidir (tip yetkisini override eder).
        ///   2. Kullanıcıya özel yetki yoksa          → tip yetkisi kullanılır.
        ///   3. Her ikisi de yoksa                    → 0 (Gizli / Erişim Yok).
        /// </summary>
        public async Task<int> GetKullaniciFormYetkiSeviyesi(int kullaniciId, int formId)
        {
            // Kullanıcıya özel yetki var mı?
            var kullaniciBazli = await SQLExecuteScalar<int?>(@"
                SELECT ""Yetki"" FROM ""KullaniciDetay""
                WHERE  ""DelUser""      IS NULL
                  AND  ""Kullanici_ID"" = @KullaniciId
                  AND  ""Form_ID""      = @FormId
                LIMIT 1",
                new { KullaniciId = kullaniciId, FormId = formId });

            // Kullanıcıya özel yetki tanımlıysa doğrudan döner (tip yetkisine bakılmaz).
            if (kullaniciBazli.HasValue)
                return kullaniciBazli.Value;

            // Yoksa tip bazlı yetkiye bak.
            var tipBazli = await SQLExecuteScalar<int?>(@"
                SELECT tfy.""Yetki""
                FROM   ""KullaniciTipDetay"" tfy
                JOIN   ""Kullanicilar""      k   ON k.""ID"" = @KullaniciId
                WHERE  tfy.""DelUser""        IS NULL
                  AND  tfy.""KullaniciTip_ID"" = k.""KullaniciTip_ID""
                  AND  tfy.""Form_ID""          = @FormId
                LIMIT 1",
                new { KullaniciId = kullaniciId, FormId = formId });

            return tipBazli ?? 0;
        }

        /// <summary>
        /// Kullanıcının etkin yetkisi >= minYetki olan formların ID listesini döner.
        ///
        /// Etkin yetki öncelik kuralı:
        ///   Kullanıcıya özel yetki tanımlıysa o geçerlidir; yoksa tip yetkisi kullanılır.
        /// </summary>
        public async Task<int[]> GetKullaniciErisebilirFormIdler(int kullaniciId, int minYetki = 1)
        {
            // Kullanıcıya özel tüm form yetkileri (yetki değerinden bağımsız)
            var kullaniciBazli = (await SQLQueryAsync<(int FormId, int Yetki)>(@"
                SELECT ""Form_ID"" AS FormId, ""Yetki""
                FROM   ""KullaniciDetay""
                WHERE  ""DelUser"" IS NULL AND ""Kullanici_ID"" = @KullaniciId",
                new { KullaniciId = kullaniciId }))
                .ToDictionary(r => r.FormId, r => r.Yetki);

            // Tip bazlı tüm form yetkileri
            var tipBazli = (await SQLQueryAsync<(int FormId, int Yetki)>(@"
                SELECT tfy.""Form_ID"" AS FormId, tfy.""Yetki""
                FROM   ""KullaniciTipDetay"" tfy
                JOIN   ""Kullanicilar""      k ON k.""KullaniciTip_ID"" = tfy.""KullaniciTip_ID""
                WHERE  tfy.""DelUser"" IS NULL AND k.""ID"" = @KullaniciId",
                new { KullaniciId = kullaniciId }))
                .ToDictionary(r => r.FormId, r => r.Yetki);

            var result = new HashSet<int>();

            // Tip bazlı formları işle; kullanıcıya özel kayıt varsa onu uygula
            foreach (var (formId, tipYetki) in tipBazli)
            {
                int etkinYetki = kullaniciBazli.TryGetValue(formId, out var kYetki) ? kYetki : tipYetki;
                if (etkinYetki >= minYetki) result.Add(formId);
            }

            // Tip bazlı kaydı olmayan ama kullanıcıya özel yetki tanımlı formları ekle
            foreach (var (formId, kYetki) in kullaniciBazli)
            {
                if (!tipBazli.ContainsKey(formId) && kYetki >= minYetki)
                    result.Add(formId);
            }

            return result.ToArray();
        }

        /// <summary>
        /// README sözleşmesi: Kullanıcının belirli bir form için efektif yetkisini hesaplar.
        ///
        /// Etkin Yetki Öncelik Kuralı:
        /// 1. KullaniciDetay'da kullanıcıya özel kayıt varsa → o geçerlidir (tip yetkisini override eder).
        /// 2. Kullanıcıya özel yetki yoksa → KullaniciTipDetay'daki tip yetkisi kullanılır.
        /// 3. Her ikisi de yoksa → 0 (Gizli / Erişim Yok).
        ///
        /// <paramref name="kullaniciTipId"/> performans için sağlanabilir; 0 geçilirse
        /// kullanıcının tipi veritabanından otomatik okunur.
        /// </summary>
        public async Task<int> GetUserFormYetkiAsync(int userId, int kullaniciTipId, int formId)
        {
            // ── 1. Kullanıcıya özel yetki (override) ─────────────────────────
            var kullaniciBazli = await SQLExecuteScalar<int?>(@"
 SELECT ""Yetki"" FROM ""KullaniciDetay""
 WHERE ""DelUser"" IS NULL
 AND ""Kullanici_ID"" = @KullaniciId
 AND ""Form_ID"" = @FormId
 LIMIT 1",
            new { KullaniciId = userId, FormId = formId });

            // Kullanıcıya özel yetki tanımlıysa doğrudan döner.
            if (kullaniciBazli.HasValue)
                return kullaniciBazli.Value;

            // ── 2. Tip bazlı yetkiye bak ──────────────────────────────────────
            int tipId = kullaniciTipId;
            if (tipId <= 0)
            {
                tipId = await SQLExecuteScalar<int>(@"
 SELECT ""KullaniciTip_ID"" FROM ""Kullanicilar""
 WHERE ""ID"" = @Id AND ""DelUser"" IS NULL
 LIMIT 1",
                new { Id = userId });
            }

            if (tipId <= 0) return 0;

            var tipBazli = await SQLExecuteScalar<int?>(@"
 SELECT ""Yetki"" FROM ""KullaniciTipDetay""
 WHERE ""DelUser"" IS NULL
 AND ""KullaniciTip_ID"" = @TipId
 AND ""Form_ID"" = @FormId
 LIMIT 1",
            new { TipId = tipId, FormId = formId });

            return tipBazli ?? 0;
        }

        /// <summary>
        /// Kullanıcı-form yetkisini set eder (insert veya update).
        ///
        /// Yetki değerleri: 0=Gizli, 1=Okuma, 2=Yazma, -1=Tip yatkısına dön (kaydı siler).
        ///
        /// Gizli (0) değeri bir override kaydı olarak saklanır; bu sayede tip yetkisi Yazma
        /// olsa bile kullanıcı bazlı Gizli yetkisi doğru biçimde uygulanır.
        /// Tip kalıtımına dönmek için yetki = -1 (KalitimYok) gönderilmelidir.
        /// </summary>
        public async Task SetKullaniciFormYetki(int kullaniciId, int formId, int yetki, int islemKullanici)
        {
            await EnsureFormYazmaYetkisiAsync(islemKullanici, "/kullanici-yetki");

            var mevcutId = await SQLExecuteScalar<int?>(@"
                SELECT ""ID"" FROM ""KullaniciDetay""
                WHERE  ""Kullanici_ID"" = @KullaniciId
                  AND  ""Form_ID""      = @FormId
                  AND  ""DelUser""      IS NULL
                LIMIT 1",
                new { KullaniciId = kullaniciId, FormId = formId });

            // yetki = -1 → kullanıcıya özel kaydı kaldır; etkin yetki tip yetkisine döner.
            if (yetki < 0)
            {
                if (mevcutId.HasValue)
                    await SQLExecute(
                        @"UPDATE ""KullaniciDetay"" SET ""DelUser"" = @Del, ""DelDate"" = now() WHERE ""ID"" = @ID",
                        new { Del = islemKullanici, ID = mevcutId.Value });
                return;
            }

            // yetki >= 0 → override kaydını yaz (Gizli=0 da dahil)
            if (mevcutId.HasValue)
            {
                await SQLExecute(
                    @"UPDATE ""KullaniciDetay""
                      SET ""Yetki"" = @Yetki, ""UpdUser"" = @Upd, ""UpdDate"" = now()
                      WHERE ""ID"" = @ID",
                    new { Yetki = yetki, Upd = islemKullanici, ID = mevcutId.Value });
            }
            else
            {
                await SQLExecute(
                    @"INSERT INTO ""KullaniciDetay"" (""Kullanici_ID"", ""Form_ID"", ""Yetki"", ""CreUser"", ""CreDate"")
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
                FROM ""KullaniciTipDetay"" tfy
                WHERE tfy.""DelUser"" IS NULL AND tfy.""KullaniciTip_ID"" = @TipId
                ORDER BY tfy.""Form_ID""",
                new { TipId = kullaniciTipId })).ToArray();
        }

        /// <summary>
        /// Kullanıcı tipi-form yetkisini set eder (insert veya update). Yetki=0 ise kaydı siler.
        /// </summary>
        public async Task SetKullaniciTipFormYetki(int kullaniciTipId, int formId, int yetki, int islemKullanici)
        {
            await EnsureFormYazmaYetkisiAsync(islemKullanici, "/kullanici-tip-yetki");

            var mevcutId = await SQLExecuteScalar<int?>(@"
                SELECT ""ID"" FROM ""KullaniciTipDetay""
                WHERE ""KullaniciTip_ID"" = @TipId AND ""Form_ID"" = @FormId AND ""DelUser"" IS NULL
                LIMIT 1",
                new { TipId = kullaniciTipId, FormId = formId });

            if (mevcutId.HasValue)
            {
                if (yetki == 0)
                {
                    await SQLExecute(@"UPDATE ""KullaniciTipDetay"" SET ""DelUser"" = @Del, ""DelDate"" = now() WHERE ""ID"" = @ID",
                        new { Del = islemKullanici, ID = mevcutId.Value });
                }
                else
                {
                    await SQLExecute(@"UPDATE ""KullaniciTipDetay"" SET ""Yetki"" = @Yetki, ""UpdUser"" = @Upd, ""UpdDate"" = now() WHERE ""ID"" = @ID",
                        new { Yetki = yetki, Upd = islemKullanici, ID = mevcutId.Value });
                }
            }
            else if (yetki > 0)
            {
                await SQLExecute(@"INSERT INTO ""KullaniciTipDetay"" (""KullaniciTip_ID"", ""Form_ID"", ""Yetki"", ""CreUser"", ""CreDate"")
                                   VALUES (@TipId, @FormId, @Yetki, @Cre, now())",
                    new { TipId = kullaniciTipId, FormId = formId, Yetki = yetki, Cre = islemKullanici });
            }
        }

        private async Task EnsureFormYazmaYetkisiAsync(int islemKullanici, string sayfaUrl)
        {
            var formId = await SQLExecuteScalar<int?>(@"
                SELECT ""ID"" FROM ""Form""
                WHERE ""DelUser"" IS NULL AND LOWER(""SayfaURL"") = LOWER(@Url)
                LIMIT 1",
                new { Url = sayfaUrl });

            if (!formId.HasValue)
                throw new UnauthorizedAccessException("Yetki formu bulunamadı.");

            var kullanici = (await GetKullanici(islemKullanici)).FirstOrDefault();
            if (kullanici is null)
                throw new UnauthorizedAccessException("İşlem yapan kullanıcı bulunamadı.");

            var yetki = await GetUserFormYetkiAsync(islemKullanici, kullanici.KullaniciTip_ID, formId.Value);
            if (yetki < YetkiTipi.Yazma)
                throw new UnauthorizedAccessException("Bu işlem için yazma yetkiniz yok.");
        }

        #endregion
    }
}