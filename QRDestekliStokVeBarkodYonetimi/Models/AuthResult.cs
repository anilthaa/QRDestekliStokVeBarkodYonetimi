namespace QRDestekliStokVeBarkodYonetimi.Models
{
    /// <summary>
    /// Kimlik doğrulama işlemlerinin sonucunu taşıyan nesne.
    /// Login / Register işlemlerinden döner.
    /// </summary>
    public class AuthResult
    {
        /// <summary>İşlem başarılı mı?</summary>
        public bool Basarili { get; set; }

        /// <summary>Hata veya bilgi mesajı.</summary>
        public string? Mesaj { get; set; }

        /// <summary>Üretilen JWT access token (başarılı girişte dolu gelir).</summary>
        public string? Token { get; set; }

        /// <summary>Token geçerlilik süresi (UTC).</summary>
        public DateTime? TokenExpiry { get; set; }

        /// <summary>Kimliği doğrulanan kullanıcı nesnesi (başarılı girişte dolu gelir).</summary>
        public ItemKullanicilar? User { get; set; }

        // ── Yardımcı factory metodları ────────────────────────────────────────

        /// <summary>Başarılı sonuç üretir.</summary>
        public static AuthResult Ok(ItemKullanicilar user, string token, DateTime expiry) =>
            new() { Basarili = true, User = user, Token = token, TokenExpiry = expiry };

        /// <summary>Başarısız sonuç üretir.</summary>
        public static AuthResult Fail(string mesaj) =>
            new() { Basarili = false, Mesaj = mesaj };
    }
}
