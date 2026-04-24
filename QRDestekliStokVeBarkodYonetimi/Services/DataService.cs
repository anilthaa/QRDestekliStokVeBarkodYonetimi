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

        #region KullaniciTip

        public async Task<ItemKullaniciTip[]> GetKullaniciTip(int ID = 0)
        {
            return (await SQLQueryAsync<ItemKullaniciTip>($@"SELECT ""ID"", ""Ad"", ""Aktif"", ""CreUser"", ""CreDate"", ""UpdUser"", ""UpdDate"", ""DelUser"", ""DelDate""
        FROM ""KullaniciTipler""
        WHERE ""DelUser"" IS NULL
        {(ID > 0 ? $@"AND ""ID"" = {ID}" : "")}
        ORDER BY ""Ad"" ")).ToArray();
        }

        public async Task<DataResult<int>> SetKullaniciTip(ItemKullaniciTip item)
        {
            if (item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                item.ID = await SQLExecuteScalar<int>(@"INSERT INTO ""KullaniciTipler"" (""Ad"", ""Aktif"", ""CreUser"", ""CreDate"")
                 VALUES (@Ad, @Aktif, @CreUser, now())
                 RETURNING ""ID""", item);
            }
            else if (!item.ID.Equals(0) && item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""KullaniciTipler""
                     SET ""Ad"" = @Ad, ""Aktif"" = @Aktif, ""UpdUser"" = @UpdUser, ""UpdDate"" = now()
                     WHERE ""ID"" = @ID", item);
            }
            else if (!item.ID.Equals(0) && !item.DelUser.Equals(0))
            {
                await SQLExecute(@"UPDATE ""KullaniciTipler""
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
    }
}