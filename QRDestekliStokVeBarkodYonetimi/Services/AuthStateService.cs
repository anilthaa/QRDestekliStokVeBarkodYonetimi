using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// UI tarafında oturum durumunu tutan, Login / Register / Logout işlemlerini
    /// <see cref="DataService"/> üzerinden yürüten scoped servis.
    ///
    /// JWT access token ProtectedLocalStorage'da (şifrelenmiş tarayıcı deposu) saklanır;
    /// sayfa yenilense bile token imzası doğrulanarak oturum geri yüklenir.
    /// </summary>
    public class AuthStateService
    {
        private const string TokenStorageKey = "qr_auth_token";

        private readonly DataService _data;
        private readonly ProtectedLocalStorage _localStorage;
        private readonly JwtService _jwt;

        public ItemKullanicilar? CurrentUser { get; private set; }
        public string? AccessToken { get; private set; }
        public DateTime? AccessTokenExpiresUtc { get; private set; }

        public bool IsAuthenticated => CurrentUser is not null && !string.IsNullOrEmpty(AccessToken);

        public event Action? OnChange;

        public AuthStateService(DataService data, ProtectedLocalStorage localStorage, JwtService jwt)
        {
            _data = data;
            _localStorage = localStorage;
            _jwt = jwt;
        }

        private void NotifyChanged() => OnChange?.Invoke();

        /// <summary>
        /// Uygulama ilk render edildiğinde çağrılır. ProtectedLocalStorage'daki
        /// JWT'yi doğrular ve geçerliyse kullanıcıyı DataService üzerinden yükler.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (IsAuthenticated) return;

            try
            {
                var stored = await _localStorage.GetAsync<string>(TokenStorageKey);
                if (!stored.Success || string.IsNullOrWhiteSpace(stored.Value))
                    return;

                var principal = _jwt.ValidateToken(stored.Value);
                if (principal is null)
                {
                    await _localStorage.DeleteAsync(TokenStorageKey);
                    return;
                }

                var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(sub, out var userId) || userId <= 0)
                    return;

                var user = (await _data.GetKullanici(userId)).FirstOrDefault();
                if (user is null || user.Aktif == false)
                {
                    await _localStorage.DeleteAsync(TokenStorageKey);
                    return;
                }

                CurrentUser = user;
                AccessToken = stored.Value;
                AccessTokenExpiresUtc = ReadExpiry(stored.Value);
                NotifyChanged();
            }
            catch
            {
                // Prerender sırasında JS erişilemeyebilir; sessizce geç.
            }
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

            await _localStorage.SetAsync(TokenStorageKey, token.Token);
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

        public async Task LogoutAsync()
        {
            CurrentUser = null;
            AccessToken = null;
            AccessTokenExpiresUtc = null;
            try { await _localStorage.DeleteAsync(TokenStorageKey); } catch { }
            NotifyChanged();
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
