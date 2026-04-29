using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// Login olan kullanıcının form yetkilerini cache'ler ve sorgu sağlar.
    /// AuthState ile birlikte scoped DI'da yaşar; kullanıcı değişince yenilenir.
    /// </summary>
    public class YetkiService
    {
        private readonly DataService _data;
        private readonly AuthStateService _auth;

        // Form ID → yetki seviyesi (0=Gizli, 1=Okuma, 2=Yazma)
        private Dictionary<int, int> _yetkiMap = new();
        private int _yuklenenKullaniciId = 0;

        public event Action? OnChange;

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

        /// <summary>
        /// Kullanıcının form yetkilerini yükler. Zaten yüklüyse tekrar yüklemez.
        /// </summary>
        public async Task LoadAsync(int kullaniciId)
        {
            if (kullaniciId <= 0) { Clear(); return; }
            if (_yuklenenKullaniciId == kullaniciId) return;

            try
            {
                // Kullanıcı bazlı yetkiler
                var kullaniciBazli = await _data.GetKullaniciFormYetki(kullaniciId);

                // Kullanıcı tipi bazlı yetkileri de al
                var user = (await _data.GetKullanici(kullaniciId)).FirstOrDefault();
                ItemKullaniciTipDetay[] tipBazli = [];
                if (user?.KullaniciTip_ID > 0)
                    tipBazli = await _data.GetKullaniciTipFormYetki(user.KullaniciTip_ID);

                var map = new Dictionary<int, int>();

                // Önce tip bazlı yükle (taban yetki)
                foreach (var t in tipBazli)
                    map[t.Form_ID] = t.Yetki;

                // Kullanıcı bazlı: ikisinden BÜYÜK olanı al.
                // Tip yetkisi taban oluşturur; kullanıcı bazlı yetki sadece yükseltebilir, kısıtlayamaz.
                foreach (var k in kullaniciBazli)
                {
                    if (map.TryGetValue(k.Form_ID, out var tipYetki))
                        map[k.Form_ID] = Math.Max(k.Yetki, tipYetki);
                    else
                        map[k.Form_ID] = k.Yetki;
                }

                _yetkiMap = map;
                _yuklenenKullaniciId = kullaniciId;
                OnChange?.Invoke();
            }
            catch
            {
                Clear();
            }
        }

        public void Clear()
        {
            _yetkiMap.Clear();
            _yuklenenKullaniciId = 0;
            OnChange?.Invoke();
        }

        /// <summary>
        /// Kullanıcının belirtilen form ID için yetki seviyesini döner.
        /// </summary>
        public int GetYetki(int formId) =>
            _yetkiMap.TryGetValue(formId, out var y) ? y : 0;

        /// <summary>
        /// Kullanıcının belirtilen SayfaURL için yetki seviyesini döner.
        /// </summary>
        public int GetYetkiBySayfaURL(string? sayfaURL, ItemForm[] tumFormlar)
        {
            if (string.IsNullOrWhiteSpace(sayfaURL)) return 0;
            var form = tumFormlar.FirstOrDefault(f =>
                string.Equals(f.SayfaURL, sayfaURL, StringComparison.OrdinalIgnoreCase));
            return form is null ? 0 : GetYetki(form.ID);
        }

        /// <summary>
        /// Kullanıcının belirtilen forma en az minYetki kadar yetkisi var mı?
        /// </summary>
        public bool HasYetki(int formId, int minYetki = YetkiTipi.Okuma) =>
            GetYetki(formId) >= minYetki;

        /// <summary>
        /// Yüklü yetki map'inde en az 1 (Okuma) yetkisi olan form ID'lerini döner.
        /// </summary>
        public IReadOnlyCollection<int> ErisebilirFormIdler() =>
            _yetkiMap.Where(kv => kv.Value >= YetkiTipi.Okuma).Select(kv => kv.Key).ToList();
    }
}