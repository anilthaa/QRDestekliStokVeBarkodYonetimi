namespace QRDestekliStokVeBarkodYonetimi.Services;

/// <summary>
/// E-posta ve dış bağlantılar için isteğin görünen kök URL'sini üretir
/// (localhost, LAN, port yönlendirme, reverse proxy / dev tunnel).
/// </summary>
public sealed class RequestBaseUrlHelper
{
    public string GetBaseUrl(HttpContext ctx, string? clientOrigin = null)
    {
        var forwarded = GetForwardedBaseUrl(ctx);
        if (forwarded is not null)
            return forwarded;

        if (TryGetValidatedClientOrigin(ctx, clientOrigin, out var clientBase))
            return clientBase;

        return GetRequestBaseUrl(ctx);
    }

    private static string? GetForwardedBaseUrl(HttpContext ctx)
    {
        if (!ctx.Request.Headers.TryGetValue("X-Forwarded-Host", out var hostValues))
            return null;

        var host = hostValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(host))
            return null;

        var scheme = ctx.Request.Headers["X-Forwarded-Proto"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(scheme))
            scheme = ctx.Request.Scheme;

        return NormalizeBase($"{scheme}://{host}");
    }

    private static bool TryGetValidatedClientOrigin(HttpContext ctx, string? clientOrigin, out string baseUrl)
    {
        baseUrl = string.Empty;
        if (string.IsNullOrWhiteSpace(clientOrigin))
            return false;

        if (!Uri.TryCreate(clientOrigin.Trim(), UriKind.Absolute, out var uri))
            return false;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return false;

        var clientAuthority = uri.Authority;
        var requestHost = ctx.Request.Host.Value ?? string.Empty;
        var forwardedHost = ctx.Request.Headers["X-Forwarded-Host"].FirstOrDefault();

        if (HostsEqual(clientAuthority, requestHost) ||
            (!string.IsNullOrWhiteSpace(forwardedHost) && HostsEqual(clientAuthority, forwardedHost)))
        {
            baseUrl = NormalizeBase(uri.GetLeftPart(UriPartial.Authority));
            return true;
        }

        if (IsLoopbackHost(requestHost))
        {
            baseUrl = NormalizeBase(uri.GetLeftPart(UriPartial.Authority));
            return true;
        }

        return false;
    }

    private static string GetRequestBaseUrl(HttpContext ctx) =>
        NormalizeBase($"{ctx.Request.Scheme}://{ctx.Request.Host}");

    private static string NormalizeBase(string url) => url.TrimEnd('/');

    private static bool IsLoopbackHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        var hostOnly = host;
        var colon = host.IndexOf(':');
        if (colon >= 0)
            hostOnly = host[..colon];

        return string.Equals(hostOnly, "localhost", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(hostOnly, "127.0.0.1", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HostsEqual(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!Uri.TryCreate($"http://{a}", UriKind.Absolute, out var uriA) ||
            !Uri.TryCreate($"http://{b}", UriKind.Absolute, out var uriB))
            return false;

        return string.Equals(uriA.Host, uriB.Host, StringComparison.OrdinalIgnoreCase) &&
               uriA.Port == uriB.Port;
    }
}
