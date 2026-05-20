using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// UI tarafında oturum durumunu tutar. Login/Logout işlemleri artık HTTP
    /// endpoint'leri (<c>/api/auth/login</c>, <c>/api/auth/logout</c>) üzerinden
    /// yapıldığı için bu sınıfın görevi <see cref="InitializeAsync"/> sırasında
    /// <see cref="CurrentUser"/>'ı öncelikle veritabanından (profil resmi vb. dahil),
    /// olmadıysa cookie claim'lerinden yüklemektir.
    ///
    /// Böylece URL'den direkt sayfa açılışlarında (yeni circuit, F5, yeni sekme)
    /// auth state kaybolmaz; tarayıcı her isteğe HttpOnly cookie'yi taşır.
    /// </summary>
    public class AuthStateService : IAuthState
    {
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly DataService _data;

        public ItemKullanicilar? CurrentUser { get; private set; }

        public bool IsAuthenticated => CurrentUser is not null;

        public event Action? OnChange;

        public AuthStateService(AuthenticationStateProvider authStateProvider, DataService data)
        {
            _authStateProvider = authStateProvider;
            _data = data;
        }

        private void NotifyChanged() => OnChange?.Invoke();

        /// <inheritdoc cref="IAuthState.NotifyStateChanged"/>
        public void NotifyStateChanged() => NotifyChanged();

    /// <summary>
    /// Oturum durumunu senkronlar: önce veritabanından kullanıcı satırı (profil resmi,
    /// aktiflik, güncelleme tarihi dahil); başarısızsa cookie claim'lerine düşer.
    /// Sayfaların <c>OnInitializedAsync</c> başında çağrılır; aynı kullanıcı zaten
    /// yüklüyse ek istek yapılmaz.
    /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                var state = await _authStateProvider.GetAuthenticationStateAsync();
                var ident = state.User?.Identity;

                // Auth değil → temizle
                if (ident is null || !ident.IsAuthenticated)
                {
                    if (CurrentUser is not null)
                    {
                        CurrentUser = null;
                        NotifyChanged();
                    }
                    return;
                }

                // Auth → claim'lerden ItemKullanicilar oluştur
                var user = state.User!;
                var idStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? user.FindFirst("sub")?.Value;
                if (!int.TryParse(idStr, out var id) || id <= 0)
                {
                    // Geçersiz cookie içeriği — temizle
                    if (CurrentUser is not null)
                    {
                        CurrentUser = null;
                        NotifyChanged();
                    }
                    return;
                }

                var tipStr = user.FindFirst("KullaniciTip_ID")?.Value;
                int.TryParse(tipStr, out var tip);

                var ad = user.FindFirst("Ad")?.Value ?? string.Empty;
                var soyad = user.FindFirst("Soyad")?.Value ?? string.Empty;
                var eposta = user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

                // Aynı kullanıcı zaten yüklü ise event tetikleme (gereksiz reload önler)
                if (CurrentUser is not null && CurrentUser.ID == id)
                    return;

                ItemKullanicilar? fromDb = null;
                try
                {
                    var rows = await _data.GetKullanici(id);
                    fromDb = rows.FirstOrDefault();
                }
                catch
                {
                    fromDb = null;
                }

                if (fromDb is not null)
                {
                    fromDb.Sifre = string.Empty;
                    CurrentUser = fromDb;
                    NotifyChanged();
                    return;
                }

                CurrentUser = new ItemKullanicilar
                {
                    ID = id,
                    KullaniciTip_ID = tip,
                    Ad = ad,
                    Soyad = soyad,
                    Eposta = eposta,
                    Sifre = string.Empty,
                    Aktif = true
                };
                NotifyChanged();
            }
            catch
            {
                CurrentUser = null;
            }
        }

        /// <summary>
        /// Eski API uyumluluğu için tutuldu — artık form-post ile <c>/api/auth/login</c>
        /// kullanılmalıdır. Doğrudan çağrılırsa cookie set edilemediği için oturum
        /// kalıcı olmaz; sadece bellek-içi geçici state oluşturur.
        /// </summary>
        [Obsolete("Form post /api/auth/login kullanın. Bu yöntem cookie set etmediği için yeni circuit'lerde state kaybedilir.")]
        public Task<(bool Success, string? Error)> LoginAsync(string eposta, string sifre)
        {
            return Task.FromResult<(bool, string?)>(
                (false, "Lütfen formu kullanarak giriş yapın."));
        }

        /// <summary>
        /// Eski API uyumluluğu için tutuldu — artık form-post ile <c>/api/auth/register</c>
        /// kullanılmalıdır.
        /// </summary>
        [Obsolete("Form post /api/auth/register kullanın.")]
        public Task<(bool Success, string? Error)> RegisterAsync(string ad, string soyad, string eposta, string sifre)
        {
            return Task.FromResult<(bool, string?)>(
                (false, "Lütfen formu kullanarak kayıt olun."));
        }

        /// <summary>
        /// Eski API uyumluluğu için tutuldu — artık form-post ile <c>/api/auth/logout</c>
        /// kullanılmalıdır.
        /// </summary>
        [Obsolete("Form post /api/auth/logout kullanın.")]
        public Task LogoutAsync()
        {
            // Yine de bellek state'ini temizle ki UI hızlıca tepki versin.
            // Asıl cookie temizliği endpoint tarafından yapılır.
            CurrentUser = null;
            NotifyChanged();
            return Task.CompletedTask;
        }
    }
}
