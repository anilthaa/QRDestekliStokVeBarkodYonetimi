namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// appsettings.json içindeki "Smtp" bölümünün tipli karşılığı.
    /// E-posta doğrulama akışı (profil sayfasında e-posta değiştirme) ve ileride
    /// eklenebilecek diğer mail bildirimleri için kullanılır.
    ///
    /// Gerçek kullanıcı/şifre bilgileri appsettings.Development.json veya User Secrets'te
    /// tutulmalı; appsettings.json'da yalnızca placeholder bırakın.
    /// Gmail için "App Password" üretip Password alanına onu yazın (gerçek hesap şifresi değil).
    /// </summary>
    public class SmtpSettings
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = "QR Stok Yönetimi";

        /// <summary>SMTP konfigürasyonu yeterli alanlarla doldurulmuş mu?</summary>
        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Host) &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(FromAddress);
    }
}
