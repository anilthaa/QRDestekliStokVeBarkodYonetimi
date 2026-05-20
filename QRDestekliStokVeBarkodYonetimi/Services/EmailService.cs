using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// SMTP üzerinden HTML e-posta gönderir. .NET'in built-in
    /// <see cref="System.Net.Mail.SmtpClient"/> sınıfını kullanır — Gmail/Outlook
    /// gibi yaygın SMTP'ler için ekstra paket gerekmez.
    ///
    /// Hata durumunda exception fırlatmaz; loglar ve <c>false</c> döner.
    /// UI tarafı bu boolean'a göre kullanıcıya net bir uyarı gösterebilir,
    /// uygulama mail hatasında çökmez.
    /// </summary>
    public class EmailService
    {
        private readonly SmtpSettings _opts;
        private readonly ILogger<EmailService> _log;

        public EmailService(IOptions<SmtpSettings> opts, ILogger<EmailService> log)
        {
            _opts = opts.Value;
            _log = log;
        }

        /// <summary>
        /// HTML mail gönderir. SMTP konfigürasyonu eksikse log yazıp false döner.
        /// </summary>
        public async Task<bool> SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                _log.LogWarning("EmailService.SendAsync: alıcı adresi boş.");
                return false;
            }

            if (!_opts.IsConfigured)
            {
                _log.LogWarning("SMTP konfigürasyonu eksik (Host/Username/Password/FromAddress). Mail gönderilemedi.");
                return false;
            }

            try
            {
                using var msg = new MailMessage
                {
                    From = new MailAddress(_opts.FromAddress, _opts.FromName, Encoding.UTF8),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    SubjectEncoding = Encoding.UTF8,
                    BodyEncoding = Encoding.UTF8
                };
                msg.To.Add(new MailAddress(to));

                using var client = new SmtpClient(_opts.Host, _opts.Port)
                {
                    EnableSsl = _opts.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_opts.Username, _opts.Password),
                    Timeout = 20_000
                };

                await client.SendMailAsync(msg, ct);
                _log.LogInformation("E-posta gönderildi: {To} / Konu: {Subject}", to, subject);
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "E-posta gönderilemedi: {To} / Konu: {Subject}", to, subject);
                return false;
            }
        }
    }
}
