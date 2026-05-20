using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// E-posta değiştirme akışında üretilen 6 haneli doğrulama kodlarını
    /// kullanıcı başına in-memory olarak tutar. TTL süresi dolan kayıtlar
    /// doğrulama denemesinde otomatik olarak temizlenir.
    ///
    /// Singleton olarak kayıtlıdır; tek sunucu örneği için yeterlidir.
    /// Birden fazla instance / load-balanced senaryoda Redis veya DB tablosuna
    /// taşımak gerekir — şimdilik proje kapsamında basit ve yeterli.
    /// </summary>
    public class EpostaDogrulamaService
    {
        private static readonly TimeSpan KodSuresi = TimeSpan.FromMinutes(10);

        private sealed record Kayit(string YeniEposta, string Kod, DateTime BitisUtc);

        private readonly ConcurrentDictionary<int, Kayit> _store = new();

        /// <summary>
        /// Kullanıcı için 6 haneli kriptografik olarak güvenli kod üretir,
        /// 10 dakika TTL ile saklar ve döner. Aynı kullanıcı için varolan
        /// kayıt üzerine yazılır (kod gönder tekrar tıklanırsa).
        /// </summary>
        public string OlusturVeKaydet(int kullaniciId, string yeniEposta)
        {
            // RandomNumberGenerator ile öngörülemez 6 haneli kod
            var sayi = RandomNumberGenerator.GetInt32(100_000, 1_000_000);
            var kod = sayi.ToString("D6");

            _store[kullaniciId] = new Kayit(
                YeniEposta: (yeniEposta ?? string.Empty).Trim(),
                Kod: kod,
                BitisUtc: DateTime.UtcNow.Add(KodSuresi));

            return kod;
        }

        /// <summary>
        /// Doğrulamayı tek seferlik tüketir: kod doğruysa kaydı siler ve
        /// yeni e-posta adresini döner. Yanlış kod / süresi dolmuş /
        /// kayıt yok durumunda <c>(false, null)</c> döner.
        /// </summary>
        public (bool Ok, string? YeniEposta) Dogrula(int kullaniciId, string? kod)
        {
            if (!_store.TryGetValue(kullaniciId, out var k))
                return (false, null);

            if (DateTime.UtcNow > k.BitisUtc)
            {
                _store.TryRemove(kullaniciId, out _);
                return (false, null);
            }

            if (!string.Equals(k.Kod, (kod ?? string.Empty).Trim(), StringComparison.Ordinal))
                return (false, null);

            _store.TryRemove(kullaniciId, out _);
            return (true, k.YeniEposta);
        }

        /// <summary>Kullanıcının bekleyen kodunu el ile iptal eder (UI'da "İptal" butonu).</summary>
        public void Iptal(int kullaniciId) => _store.TryRemove(kullaniciId, out _);

        /// <summary>
        /// Kullanıcı için bekleyen bir kod var mı? (UI'nın doğrulama kartını
        /// göstermek için kullanabileceği yardımcı; mevcut akışta UI kendi
        /// state'ini tuttuğu için zorunlu değil.)
        /// </summary>
        public bool BekleyenVar(int kullaniciId)
            => _store.TryGetValue(kullaniciId, out var k) && DateTime.UtcNow <= k.BitisUtc;
    }
}
