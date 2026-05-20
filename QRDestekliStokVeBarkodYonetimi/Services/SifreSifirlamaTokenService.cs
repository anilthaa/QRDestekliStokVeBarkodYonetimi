using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// Şifre sıfırlama e-postasındaki tek kullanımlık token'ları bellekte tutar.
    /// Tek sunucu örneği için yeterlidir; yük dengeleme veya restart sonrası token geçersiz olur.
    /// </summary>
    public sealed class SifreSifirlamaTokenService
    {
        private static readonly TimeSpan TokenOmru = TimeSpan.FromMinutes(60);

        private sealed record Kayit(int UserId, DateTime BitisUtc);

        private readonly ConcurrentDictionary<string, Kayit> _store = new();

        /// <summary>Token geçerliyse kullanıcı ID döner; kaydı silmez (form doğrulaması için).</summary>
        public bool TryPeekValidToken(string? token, out int userId)
        {
            userId = 0;
            var key = (token ?? string.Empty).Trim();
            if (key.Length == 0) return false;

            if (!_store.TryGetValue(key, out var k))
                return false;

            if (DateTime.UtcNow > k.BitisUtc)
            {
                _store.TryRemove(key, out _);
                return false;
            }

            userId = k.UserId;
            return true;
        }

        /// <summary>Rastgele URL-güvenli token üretir, kullanıcıya bağlar ve döner.</summary>
        public string OlusturKaydet(int kullaniciId)
        {
            Span<byte> bytes = stackalloc byte[32];
            RandomNumberGenerator.Fill(bytes);
            var token = Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            _store[token] = new Kayit(kullaniciId, DateTime.UtcNow.Add(TokenOmru));
            return token;
        }

        /// <summary>Token geçerliyse tek seferde tüketir ve kullanıcı ID döner.</summary>
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
