namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// PBKDF2 (HMAC-SHA256, 100.000 iterasyon) ile şifre hash'leme ve doğrulama.
    ///
    /// Saklanan format: {iterations}.{base64Salt}.{base64Hash}
    ///
    /// Bu sınıf, README'deki <c>PasswordHelper</c> adlandırma sözleşmesini karşılamak için
    /// <see cref="PasswordHasher"/> üzerine ince bir sarmalayıcı (facade) olarak tasarlanmıştır.
    /// Yeni kodda doğrudan <see cref="PasswordHasher"/> veya <see cref="PasswordHelper"/>
    /// kullanılabilir; ikisi de aynı PBKDF2 algoritmasını uygular.
    ///
    /// Kullanım örneği:
    /// <code>
    /// string hash    = PasswordHelper.HashPassword("kullaniciSifresi");
    /// bool   gecerli = PasswordHelper.VerifyPassword("kullaniciSifresi", hash);
    /// </code>
    /// </summary>
    public static class PasswordHelper
    {
        // ── Hash ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Verilen düz metin şifreyi PBKDF2-HMAC-SHA256 ile hash'ler.
        /// </summary>
        /// <param name="plainTextPassword">Hash'lenecek düz metin şifre.</param>
        /// <returns>"{iterations}.{base64Salt}.{base64Hash}" biçiminde saklama dizesi.</returns>
        public static string HashPassword(string plainTextPassword)
            => PasswordHasher.Hash(plainTextPassword);

        // ── Verify ───────────────────────────────────────────────────────────

        /// <summary>
        /// Düz metin şifreyi daha önce hash'lenmiş değerle karşılaştırır.
        /// Sabit zamanlı karşılaştırma kullanarak zamanlama saldırılarına karşı koruma sağlar.
        /// </summary>
        /// <param name="plainTextPassword">Doğrulanacak düz metin şifre.</param>
        /// <param name="hashedPassword">Veritabanında saklanan hash değeri.</param>
        /// <returns>Şifre doğru ise <c>true</c>, aksi takdirde <c>false</c>.</returns>
        public static bool VerifyPassword(string plainTextPassword, string? hashedPassword)
            => PasswordHasher.Verify(plainTextPassword, hashedPassword);
    }
}
