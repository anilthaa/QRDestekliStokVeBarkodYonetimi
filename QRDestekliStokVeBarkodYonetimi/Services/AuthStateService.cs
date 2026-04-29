using System.IdentityModel.Tokens.Jwt;
using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// UI tarafında oturum durumunu tutan, Login / Register / Logout işlemlerini
    /// <see cref="DataService"/> üzerinden yürüten scoped servis.
    ///
    /// Token yalnızca bellekte tutulur; uygulama yeniden açıldığında oturum sıfırlanır
    /// ve kullanıcı tekrar giriş yapmak zorundadır.
    /// </summary>
    public class AuthStateService
    {
        private readonly DataService _data;
        private readonly JwtService _jwt;

        public ItemKullanicilar? CurrentUser { get; private set; }
        public string? AccessToken { get; private set; }
        public DateTime? AccessTokenExpiresUtc { get; private set; }

        public bool IsAuthenticated => CurrentUser is not null && !string.IsNullOrEmpty(AccessToken);

        public event Action? OnChange;

        public AuthStateService(DataService data, JwtService jwt)
        {
            _data = data;
            _jwt = jwt;
        }

        private void NotifyChanged() => OnChange?.Invoke();

        /// <summary>
        /// Oturum durumunu kontrol eder. Sadece bellek tabanlı; uygulama yeniden
        /// açıldığında oturum sıfırlanmış olur ve kullanıcı login sayfasına yönlendirilir.
        /// </summary>
        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<(bool Success, string? Error)> LoginAsync(string eposta, string sifre)
        {
            var result = await _data.LoginKullanici(eposta, sifre);
            if (result.Data is null)
                return (false, string.IsNullOrEmpty(result.SonucAciklama) ? "Giriş başarısız." : result.SonucAciklama);

            var token = _jwt.GenerateAccessToken(result.Data);

            CurrentUser = result.Data;
            AccessToken = token.Token;
            AccessTokenExpiresUtc = token.ExpiresUtc;

            NotifyChanged();
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> RegisterAsync(string ad, string soyad, string eposta, string sifre)
        {
            var result = await _data.RegisterKullanici(ad, soyad, eposta, sifre);
            if (result.SonucKodu < 0 || result.Data <= 0)
                return (false, string.IsNullOrEmpty(result.SonucAciklama) ? "Kayıt sırasında bir hata oluştu." : result.SonucAciklama);

            return (true, null);
        }

        public Task LogoutAsync()
        {
            CurrentUser = null;
            AccessToken = null;
            AccessTokenExpiresUtc = null;
            NotifyChanged();
            return Task.CompletedTask;
        }

        private static DateTime? ReadExpiry(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token)) return null;
                var jwt = handler.ReadJwtToken(token);
                return jwt.ValidTo == default ? null : jwt.ValidTo;
            }
            catch
            {
                return null;
            }
        }
    }
}