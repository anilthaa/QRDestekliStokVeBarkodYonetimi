using Microsoft.AspNetCore.Http;
using QRDestekliStokVeBarkodYonetimi.Services;

namespace QRDestekliStokVeBarkodYonetimi.Tests.Services;

public class RequestBaseUrlHelperTests
{
    private readonly RequestBaseUrlHelper _sut = new();

    [Fact]
    public void GetBaseUrl_VarsayilanIstek_LocalhostPort()
    {
        var ctx = CreateContext("http", new HostString("localhost:5165"));

        var url = _sut.GetBaseUrl(ctx);

        Assert.Equal("http://localhost:5165", url);
    }

    [Fact]
    public void GetBaseUrl_ForwardedHeaders_TunnelUrl()
    {
        var ctx = CreateContext("http", new HostString("localhost:5165"));
        ctx.Request.Headers["X-Forwarded-Host"] = "myapp.devtunnels.ms";
        ctx.Request.Headers["X-Forwarded-Proto"] = "https";

        var url = _sut.GetBaseUrl(ctx);

        Assert.Equal("https://myapp.devtunnels.ms", url);
    }

    [Fact]
    public void GetBaseUrl_SiteOrigin_LanIpEslesir()
    {
        var ctx = CreateContext("http", new HostString("192.168.1.10:5165"));

        var url = _sut.GetBaseUrl(ctx, "http://192.168.1.10:5165");

        Assert.Equal("http://192.168.1.10:5165", url);
    }

    [Fact]
    public void GetBaseUrl_SiteOrigin_LocalhostIstek_TunnelOrigin()
    {
        var ctx = CreateContext("http", new HostString("localhost:5165"));

        var url = _sut.GetBaseUrl(ctx, "https://myapp.devtunnels.ms");

        Assert.Equal("https://myapp.devtunnels.ms", url);
    }

    [Fact]
    public void GetBaseUrl_SiteOrigin_UyumsuzHost_YokSayilir()
    {
        var ctx = CreateContext("http", new HostString("192.168.1.10:5165"));

        var url = _sut.GetBaseUrl(ctx, "http://evil.example.com");

        Assert.Equal("http://192.168.1.10:5165", url);
    }

    private static DefaultHttpContext CreateContext(string scheme, HostString host)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = scheme;
        ctx.Request.Host = host;
        return ctx;
    }
}
