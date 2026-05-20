using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// Yönetici tarafından gönderilen hesap onay e-postasındaki tek kullanımlık token'ları bellekte tutar.
    /// </summary>
    public sealed class KullaniciHesapOnayTokenService
    {
        private static readonly TimeSpan TokenOmru = TimeSpan.FromHours(72);

        private sealed record Kayit(int UserId, DateTime BitisUtc);

        private readonly ConcurrentDictionary<string, Kayit> _store = new();

        public string OlusturKaydet(int kullaniciId)
        {
            foreach (var kv in _store.Where(x => x.Value.UserId == kullaniciId).ToList())
                _store.TryRemove(kv.Key, out _);

            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            var token = Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            _store[token] = new Kayit(kullaniciId, DateTime.UtcNow.Add(TokenOmru));
            return token;
        }

        public bool TryConsume(string? token, out int userId)
        {
            userId = 0;
            var key = (token ?? string.Empty).Trim();
            if (key.Length == 0) return false;

            if (!_store.TryRemove(key, out var k))
                return false;

            if (DateTime.UtcNow > k.BitisUtc)
                return false;

            userId = k.UserId;
            return true;
        }
    }
}
