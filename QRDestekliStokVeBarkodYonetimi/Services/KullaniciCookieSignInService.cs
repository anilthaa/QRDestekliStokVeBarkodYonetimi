using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services;

/// <summary>
/// Başarılı giriş sonrası kullanıcıyı cookie ile oturum açar.
/// </summary>
public class KullaniciCookieSignInService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public KullaniciCookieSignInService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task SignInAsync(ItemKullanicilar user, bool beniHatirla, CancellationToken ct = default)
    {
        var ctx = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext kullanılamıyor.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.ID.ToString()),
            new(ClaimTypes.Name, $"{user.Ad} {user.Soyad}".Trim()),
            new(ClaimTypes.Email, user.Eposta ?? string.Empty),
            new("KullaniciTip_ID", user.KullaniciTip_ID.ToString()),
            new("Ad", user.Ad ?? string.Empty),
            new("Soyad", user.Soyad ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProps = new AuthenticationProperties { IsPersistent = beniHatirla };
        if (beniHatirla)
            authProps.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);

        await ctx.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProps);
    }

    public static string GuvenliReturnUrl(string? returnUrl)
    {
        var hedef = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        if (!Uri.IsWellFormedUriString(hedef, UriKind.Relative))
            hedef = "/";
        return hedef;
    }
}
