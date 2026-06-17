using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

namespace QRDestekliStokVeBarkodYonetimi.Services;

/// <summary>
/// Cookie oturumunun bitiş zamanını okur (kaydırmalı süre dahil).
/// </summary>
public class OturumSuresiService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptionsMonitor<CookieAuthenticationOptions> _cookieOptions;

    public OturumSuresiService(
        IHttpContextAccessor httpContextAccessor,
        IOptionsMonitor<CookieAuthenticationOptions> cookieOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _cookieOptions = cookieOptions;
    }

    public async Task<DateTimeOffset?> GetBitisZamaniUtcAsync()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null)
            return null;

        var auth = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!auth.Succeeded)
            return null;

        var props = auth.Properties;
        if (props.ExpiresUtc.HasValue)
            return props.ExpiresUtc;

        var expireSpan = _cookieOptions.Get(CookieAuthenticationDefaults.AuthenticationScheme).ExpireTimeSpan;
        if (props.IssuedUtc.HasValue)
            return props.IssuedUtc.Value.Add(expireSpan);

        return DateTimeOffset.UtcNow.Add(expireSpan);
    }

    public async Task<TimeSpan?> GetKalanSureAsync()
    {
        var bitis = await GetBitisZamaniUtcAsync();
        if (!bitis.HasValue)
            return null;

        var kalan = bitis.Value - DateTimeOffset.UtcNow;
        return kalan <= TimeSpan.Zero ? TimeSpan.Zero : kalan;
    }
}
