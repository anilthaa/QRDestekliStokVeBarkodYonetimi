using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// Login olan kullanıcının form yetkilerini önbelleğe alır ve sorgular.
    /// AuthStateService ile birlikte Scoped DI'da yaşar; kullanıcı değişince yenilenir.
    ///
    /// Etkin Yetki Öncelik Kuralı:
    ///   1. Kullanıcıya özel yetki tanımlanmışsa → o geçerlidir (tip yetkisini override eder).
    ///   2. Kullanıcıya özel yetki yoksa          → kullanıcı tipinin yetkisi kullanılır.
    ///   3. Her ikisi de yoksa                    → 0 (Gizli / Erişim Yok).
    /// </summary>
    public class YetkiService : IDisposable
    {
        private readonly IYetkiDataAccess _data;
        private readonly IAuthState _auth;

        // Form ID → etkin yetki seviyesi (0=Gizli, 1=Okuma, 2=Yazma)
        private Dictionary<int, int> _yetkiMap = new();
        private int _yuklenenKullaniciId = 0;

        // Yükleme tamamlandı mı? (boş map ile "henüz yüklenmedi" ayrımı için)
        private bool _isLoaded = false;

        // Form listesi önbelleği (SayfaURL bazlı sorgu için)
        private ItemForm[] _tumFormlar = [];
        private bool _formlarYuklendi = false;

        // Eşzamanlı LoadAsync / EnsureFormsLoadedAsync çağrılarında
        // çift DB sorgusunu ve _yetkiMap üzerindeki yarışı engeller.
        private readonly SemaphoreSlim _yukleKilit = new(1, 1);
        private readonly SemaphoreSlim _formKilit = new(1, 1);

        public event Action? OnChange;

        /// <summary>
        /// Yetki haritasının en az bir kez başarıyla yüklenip yüklenmediğini gösterir.
        /// NavMenu bu flag'e göre menüyü filtreler; false iken tüm menüyü göstermez.
        /// </summary>
        public bool IsLoaded => _isLoaded;

        public YetkiService(IYetkiDataAccess data, IAuthState auth)
        {
            _data = data;
            _auth = auth;
            _auth.OnChange += OnAuthChanged;
        }

        private async void OnAuthChanged()
        {
            // async void event handler — exception fırlatması app çökmesine yol açar.
            try
            {
                if (_auth.IsAuthenticated && _auth.CurrentUser is not null)
                    await LoadAsync(_auth.CurrentUser.ID);
                else
                    Clear();
            }
            catch
            {
                // DB hatası vb. UI'ı çökertmeyelim; map boş kalır, kullanıcı menüde hiçbir şey göremez.
                Clear();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Yükleme
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Form listesini yükler (sadece bir kez yüklenir).
        /// </summary>
        public async Task EnsureFormsLoadedAsync()
        {
            if (_formlarYuklendi) return;

            await _formKilit.WaitAsync();
            try
            {
                if (_formlarYuklendi) return; // double-check
                _tumFormlar = await _data.GetTumFormlar();
                _formlarYuklendi = true;
            }
            catch
            {
                _tumFormlar = [];
            }
            finally
            {
                _formKilit.Release();
            }
        }

        /// <summary>
        /// Kullanıcının form yetkilerini yükler.
        /// Aynı kullanıcı zaten yüklüyse tekrar yüklemez; zorla yenilemek için
        /// <see cref="ForceReloadAsync"/> kullanın.
        /// </summary>
        public async Task LoadAsync(int kullaniciId)
        {
            if (kullaniciId <= 0) { Clear(); return; }
            if (_yuklenenKullaniciId == kullaniciId && _isLoaded) return;

            await YukleIcAsync(kullaniciId);
        }

        /// <summary>
        /// Önbelleği görmezden gelip yetkileri yeniden yükler.
        /// Yetki değişikliği kaydedildikten sonra çağrılmalıdır.
        ///
        /// <para>
        /// <b>Önemli:</b> Bu servis Scoped'tur ve oturum açan kullanıcının yetki haritasını tutar.
        /// Çağrılan <paramref name="kullaniciId"/> mevcut oturum kullanıcısı ile aynı değilse
        /// sessizce no-op yapılır; aksi halde admin'in kendi yetki haritası başka bir
        /// kullanıcının haritasıyla overwrite olur ve menü/erişim kontrolleri bozulur.
        /// </para>
        /// </summary>
        public async Task ForceReloadAsync(int kullaniciId)
        {
            if (kullaniciId <= 0) { Clear(); return; }

            // Defense-in-depth: yalnızca oturumdaki kullanıcının haritası yenilenebilir.
            // Çağıran taraf yanlışlıkla başkasının ID'sini geçtiyse cache'i bozmuyoruz.
            if (_auth.CurrentUser is null || _auth.CurrentUser.ID != kullaniciId)
                return;

            await _yukleKilit.WaitAsync();
            try
            {
                _yuklenenKullaniciId = 0; // cache'i geçersiz kıl
                _isLoaded = false;
            }
            finally
            {
                _yukleKilit.Release();
            }

            await YukleIcAsync(kullaniciId);
        }

        private async Task YukleIcAsync(int kullaniciId)
        {
            await _yukleKilit.WaitAsync();
            try
            {
                // Bekleme sırasında başka bir çağrı zaten yüklemiş olabilir.
                if (_yuklenenKullaniciId == kullaniciId && _isLoaded) return;

                // ── 1. Kullanıcıya özel yetkiler (override) ───────────────────
                var kullaniciBazli = await _data.GetKullaniciFormYetki(kullaniciId);

                // ── 2. Kullanıcı tipinin yetkileri (taban) ────────────────────
                var user = (await _data.GetKullanici(kullaniciId)).FirstOrDefault();
                ItemKullaniciTipDetay[] tipBazli = [];
                if (user?.KullaniciTip_ID > 0)
                    tipBazli = await _data.GetKullaniciTipFormYetki(user.KullaniciTip_ID);

                // ── 3. Etkin yetki haritası ───────────────────────────────────
                //    Önce tip yetkilerini yükle (taban),
                //    ardından kullanıcıya özel kayıt varsa üzerine yaz (override).
                var map = new Dictionary<int, int>();

                foreach (var t in tipBazli)
                    map[t.Form_ID] = t.Yetki;

                // Kullanıcıya özel yetki, tip yetkisini geçersiz kılar.
                // Gizli (0) dahil — sıfır değeri de açıkça override'dır.
                foreach (var k in kullaniciBazli)
                    map[k.Form_ID] = k.Yetki;

                _yetkiMap = map;
                _yuklenenKullaniciId = kullaniciId;
                _isLoaded = true;
            }
            catch
            {
                _yetkiMap = new();
                _yuklenenKullaniciId = 0;
                _isLoaded = false;
            }
            finally
            {
                _yukleKilit.Release();
            }

            OnChange?.Invoke();
        }

        /// <summary>
        /// Yetki haritasını, form önbelleğini ve yükleme durumunu sıfırlar.
        /// </summary>
        public void Clear()
        {
            _yetkiMap = new();
            _yuklenenKullaniciId = 0;
            _isLoaded = false;
            _formlarYuklendi = false;
            _tumFormlar = [];
            OnChange?.Invoke();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Sorgulama
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Kullanıcının belirtilen form ID için etkin yetki seviyesini döner.
        /// Yüklenmemişse 0 (Gizli) döner.
        /// </summary>
        public int GetYetki(int formId) =>
            _yetkiMap.TryGetValue(formId, out var y) ? y : 0;

        /// <summary>
        /// Kullanıcının belirtilen SayfaURL için etkin yetki seviyesini döner.
        /// (İç önbellekteki form listesi kullanılır.)
        /// </summary>
        public int GetYetkiBySayfaURL(string? sayfaURL)
        {
            if (string.IsNullOrWhiteSpace(sayfaURL)) return 0;
            var form = _tumFormlar.FirstOrDefault(f =>
                string.Equals(f.SayfaURL, sayfaURL, StringComparison.OrdinalIgnoreCase));
            return form is null ? 0 : GetYetki(form.ID);
        }

        /// <summary>
        /// Kullanıcının belirtilen SayfaURL için etkin yetki seviyesini döner.
        /// (Dışarıdan sağlanan form listesi kullanılır.)
        /// </summary>
        public int GetYetkiBySayfaURL(string? sayfaURL, ItemForm[] tumFormlar)
        {
            if (string.IsNullOrWhiteSpace(sayfaURL)) return 0;
            var form = tumFormlar.FirstOrDefault(f =>
                string.Equals(f.SayfaURL, sayfaURL, StringComparison.OrdinalIgnoreCase));
            return form is null ? 0 : GetYetki(form.ID);
        }

        /// <summary>Kullanıcının belirtilen sayfaya erişim yetkisi var mı? (etkin yetki >= Okuma)</summary>
        public bool ErişimVar(string? sayfaURL) =>
            GetYetkiBySayfaURL(sayfaURL) >= YetkiTipi.Okuma;

        /// <summary>Kullanıcının belirtilen sayfada yalnızca okuma yetkisi var mı?</summary>
        public bool SadecOkuma(string? sayfaURL) =>
            GetYetkiBySayfaURL(sayfaURL) == YetkiTipi.Okuma;

        /// <summary>Kullanıcının belirtilen sayfada yazma yetkisi var mı? (etkin yetki >= Yazma)</summary>
        public bool HasYazma(string? sayfaURL) =>
            GetYetkiBySayfaURL(sayfaURL) >= YetkiTipi.Yazma;

        /// <summary>Kullanıcının belirtilen forma en az minYetki kadar etkin yetkisi var mı?</summary>
        public bool HasYetki(int formId, int minYetki = YetkiTipi.Okuma) =>
            GetYetki(formId) >= minYetki;

        /// <summary>
        /// Etkin yetkisi en az Okuma (1) olan form ID'lerini döner.
        /// Gizli (0) kesinlikle dahil edilmez.
        /// Yüklenmediyse boş koleksiyon döner.
        /// </summary>
        public IReadOnlyCollection<int> ErisebilirFormIdler() =>
            _yetkiMap.Where(kv => kv.Value >= YetkiTipi.Okuma)
                     .Select(kv => kv.Key)
                     .ToList();

        // ─────────────────────────────────────────────────────────────────────
        // Async sorgulama — README PermissionService API sözleşmesi
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Mevcut kullanıcının belirtilen sayfaya en az <c>Okuma (1)</c> yetkisi var mı?
        ///
        /// Yetki haritası henüz yüklenmemişse, oturumdaki kullanıcı kimliğiyle otomatik olarak
        /// yüklenir. Bu sayede bileşenler <c>LoadAsync</c> çağırmadan güvenle kullanabilir.
        /// </summary>
        /// <param name="sayfaUrl">Blazor route (örn. "/urunler").</param>
        public async Task<bool> CanReadAsync(string? sayfaUrl)
        {
            await EnsureLoadedAsync();
            await EnsureFormsLoadedAsync();
            return GetYetkiBySayfaURL(sayfaUrl) >= YetkiTipi.Okuma;
        }

        /// <summary>
        /// Mevcut kullanıcının belirtilen sayfaya en az <c>Yazma (2)</c> yetkisi var mı?
        ///
        /// Yetki haritası henüz yüklenmemişse otomatik olarak yüklenir.
        /// </summary>
        /// <param name="sayfaUrl">Blazor route (örn. "/urunler").</param>
        public async Task<bool> CanWriteAsync(string? sayfaUrl)
        {
            await EnsureLoadedAsync();
            await EnsureFormsLoadedAsync();
            return GetYetkiBySayfaURL(sayfaUrl) >= YetkiTipi.Yazma;
        }

        /// <summary>
        /// Mevcut kullanıcının belirtilen sayfadaki etkin yetki seviyesi
        /// <paramref name="minYetki"/> değerini karşılıyor mu?
        ///
        /// Örnek kullanım:
        /// <code>
        /// bool erisilebilir = await Yetki.HasPermissionAsync("/urunler", YetkiTipi.Okuma);
        /// bool yazabilir    = await Yetki.HasPermissionAsync("/urunler", YetkiTipi.Yazma);
        /// </code>
        /// </summary>
        /// <param name="sayfaUrl">Blazor route.</param>
        /// <param name="minYetki"><see cref="YetkiTipi"/> sabiti.</param>
        public async Task<bool> HasPermissionAsync(string? sayfaUrl, int minYetki)
        {
            await EnsureLoadedAsync();
            await EnsureFormsLoadedAsync();
            return GetYetkiBySayfaURL(sayfaUrl) >= minYetki;
        }

        /// <summary>
        /// Mevcut kullanıcının belirtilen form ID için etkin yetki seviyesini asenkron olarak döner.
        /// Yetki haritası henüz yüklenmemişse otomatik olarak yüklenir.
        /// </summary>
        /// <param name="formId">Form tablosundaki birincil anahtar.</param>
        public async Task<int> GetCurrentUserYetkiAsync(int formId)
        {
            await EnsureLoadedAsync();
            return GetYetki(formId);
        }

        /// <summary>
        /// Yetki haritası yüklenmemişse ve oturumda geçerli bir kullanıcı varsa yükler.
        /// </summary>
        public async Task EnsureLoadedAsync()
        {
            if (_isLoaded) return;
            if (_auth.CurrentUser is not null)
                await LoadAsync(_auth.CurrentUser.ID);
        }

        /// <summary>
        /// Sync sorgu metotlarının (<see cref="ErişimVar"/>, <see cref="SadecOkuma"/>,
        /// <see cref="HasYazma"/>, <see cref="GetYetkiBySayfaURL(string?)"/>) güvenle çalışabilmesi
        /// için hem kullanıcı yetki haritasını hem de form listesini yükler.
        ///
        /// <para>
        /// Sayfaların <c>OnInitializedAsync</c> içinde tek satırda erişim/yetki
        /// kontrolüne hazırlanması için tasarlanmıştır. URL ile direkt sayfa açılışlarında
        /// yetki haritası henüz yüklü olmadığı için <see cref="EnsureFormsLoadedAsync"/>
        /// tek başına yeterli değildir.
        /// </para>
        /// </summary>
        public async Task EnsureReadyAsync()
        {
            await EnsureLoadedAsync();
            await EnsureFormsLoadedAsync();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Dispose
        // ─────────────────────────────────────────────────────────────────────

        public void Dispose()
        {
            _auth.OnChange -= OnAuthChanged;
            _yukleKilit.Dispose();
            _formKilit.Dispose();
        }
    }
}
