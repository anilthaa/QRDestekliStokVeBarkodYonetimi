using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// Profil şifre değişikliği akışında üretilen 6 haneli doğrulama kodlarını ve
    /// onay sonrası uygulanacak yeni şifre hash'ini kullanıcı başına in-memory tutar.
    /// <see cref="EpostaDogrulamaService"/> ile ayrı sözlük kullanılır (çakışma olmaz).
    /// </summary>
    public class SifreDegistirDogrulamaService
    {
        private static readonly TimeSpan KodSuresi = TimeSpan.FromMinutes(10);

        private sealed record Kayit(string YeniSifreHash, string Kod, DateTime BitisUtc);

        private readonly ConcurrentDictionary<int, Kayit> _store = new();
        private readonly TimeProvider _time;

        public SifreDegistirDogrulamaService()
            : this(TimeProvider.System)
        {
        }

        public SifreDegistirDogrulamaService(TimeProvider timeProvider)
        {
            _time = timeProvider;
        }

        /// <summary>
        /// 6 haneli kod üretir, hash ile birlikte saklar ve kodu döner.
        /// </summary>
        public string OlusturVeKaydet(int kullaniciId, string yeniSifreHash)
        {
            var sayi = RandomNumberGenerator.GetInt32(100_000, 1_000_000);
            var kod = sayi.ToString("D6");

            _store[kullaniciId] = new Kayit(
                YeniSifreHash: yeniSifreHash ?? string.Empty,
                Kod: kod,
                BitisUtc: _time.GetUtcNow().UtcDateTime.Add(KodSuresi));

            return kod;
        }

        /// <summary>
        /// Kod doğruysa kaydı siler ve yeni şifre hash'ini döner.
        /// </summary>
        public (bool Ok, string? YeniSifreHash) Dogrula(int kullaniciId, string? kod)
        {
            if (!_store.TryGetValue(kullaniciId, out var k))
                return (false, null);

            if (_time.GetUtcNow().UtcDateTime > k.BitisUtc)
            {
                _store.TryRemove(kullaniciId, out _);
                return (false, null);
            }

            if (!string.Equals(k.Kod, (kod ?? string.Empty).Trim(), StringComparison.Ordinal))
                return (false, null);

            _store.TryRemove(kullaniciId, out _);
            return (true, k.YeniSifreHash);
        }

        public void Iptal(int kullaniciId) => _store.TryRemove(kullaniciId, out _);

        public bool BekleyenVar(int kullaniciId)
            => _store.TryGetValue(kullaniciId, out var k) && _time.GetUtcNow().UtcDateTime <= k.BitisUtc;
    }
}
