using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// JWT üretme / doğrulama servisi.
    ///  - <see cref="GenerateAccessToken"/>  : Kullanıcı için imzalı access token üretir.
    ///  - <see cref="GenerateRefreshToken"/> : Rastgele güvenli refresh token üretir.
    ///  - <see cref="ValidateToken"/>        : Gelen token'ı doğrular ve ClaimsPrincipal döner.
    ///  - <see cref="GetPrincipalFromExpiredToken"/> : Süresi dolmuş token'dan claim çıkarır
    ///    (refresh akışı için, imza doğrulanır ama süre kontrolü yapılmaz).
    /// </summary>
    public class JwtService
    {
        private readonly JwtSettings _settings;
        private readonly SymmetricSecurityKey _key;
        private readonly JwtSecurityTokenHandler _handler = new();

        public JwtService(IOptions<JwtSettings> options)
        {
            _settings = options.Value ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(_settings.SecretKey) || _settings.SecretKey.Length < 32)
                throw new InvalidOperationException(
                    "Jwt:SecretKey en az 32 karakter olmalıdır. appsettings.json dosyanızı kontrol edin.");

            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        }

        public TokenResult GenerateAccessToken(ItemKullanicilar user)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_settings.AccessTokenMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.ID.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(JwtRegisteredClaimNames.Email, user.Eposta ?? string.Empty),
                new(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new(ClaimTypes.Name, $"{user.Ad} {user.Soyad}".Trim()),
                new("KullaniciTip_ID", user.KullaniciTip_ID.ToString())
            };

            var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: credentials);

            var jwt = _handler.WriteToken(token);
            return new TokenResult(jwt, expires);
        }

        public string GenerateRefreshToken()
        {
            Span<byte> buffer = stackalloc byte[64];
            RandomNumberGenerator.Fill(buffer);
            return Convert.ToBase64String(buffer);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            try
            {
                var principal = _handler.ValidateToken(token, GetValidationParameters(validateLifetime: true),
                    out _);
                return principal;
            }
            catch
            {
                return null;
            }
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            try
            {
                var principal = _handler.ValidateToken(token, GetValidationParameters(validateLifetime: false),
                    out var securityToken);

                if (securityToken is not JwtSecurityToken jwt ||
                    !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Kimlik doğrulama middleware'inin de kullanacağı ortak validation parametreleri.
        /// </summary>
        public TokenValidationParameters GetValidationParameters(bool validateLifetime = true) => new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = validateLifetime,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = _key,
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        public record TokenResult(string Token, DateTime ExpiresUtc);
    }
}
