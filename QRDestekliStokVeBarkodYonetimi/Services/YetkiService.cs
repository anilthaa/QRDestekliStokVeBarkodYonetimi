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
    public class YetkiService
    {
        private readonly DataService      _data;
        private readonly AuthStateService _auth;

        // Form ID → etkin yetki seviyesi (0=Gizli, 1=Okuma, 2=Yazma)
        private Dictionary<int, int> _yetkiMap           = new();
        private int                  _yuklenenKullaniciId = 0;

        // Yükleme tamamlandı mı? (boş map ile "henüz yüklenmedi" ayrımı için)
        private bool _isLoaded = false;

        // Form listesi önbelleği (SayfaURL bazlı sorgu için)
        private ItemForm[] _tumFormlar      = [];
        private bool       _formlarYuklendi = false;

        public event Action? OnChange;

        /// <summary>
        /// Yetki haritasının en az bir kez başarıyla yüklenip yüklenmediğini gösterir.
        /// NavMenu bu flag'e göre menüyü filtreler; false iken tüm menüyü göstermez.
        /// </summary>
        public bool IsLoaded => _isLoaded;

        public YetkiService(DataService data, AuthStateService auth)
        {
            _data = data;
            _auth = auth;
            _auth.OnChange += OnAuthChanged;
        }

        private async void OnAuthChanged()
        {
            if (_auth.IsAuthenticated && _auth.CurrentUser is not null)
                await LoadAsync(_auth.CurrentUser.ID);
            else
                Clear();
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
            try
            {
                _tumFormlar      = await _data.GetTumFormlar();
                _formlarYuklendi = true;
            }
            catch
            {
                _tumFormlar = [];
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
        /// </summary>
        public async Task ForceReloadAsync(int kullaniciId)
        {
            if (kullaniciId <= 0) { Clear(); return; }
            _yuklenenKullaniciId = 0; // cache'i geçersiz kıl
            await YukleIcAsync(kullaniciId);
        }

        private async Task YukleIcAsync(int kullaniciId)
        {
            try
            {
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

                _yetkiMap            = map;
                _yuklenenKullaniciId = kullaniciId;
                _isLoaded            = true;
                OnChange?.Invoke();
            }
            catch
            {
                Clear();
            }
        }

        /// <summary>
        /// Yetki haritasını, form önbelleğini ve yükleme durumunu sıfırlar.
        /// </summary>
        public void Clear()
        {
            _yetkiMap            = new();
            _yuklenenKullaniciId = 0;
            _isLoaded            = false;
            _formlarYuklendi     = false;
            _tumFormlar          = [];
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
    }
}