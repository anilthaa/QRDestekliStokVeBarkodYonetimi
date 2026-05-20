using QRDestekliStokVeBarkodYonetimi.Services;
using Xunit;

namespace QRDestekliStokVeBarkodYonetimi.Tests.Qr;

public class QrServiceTests
{
    private readonly QrService _sut = new();

    [Fact]
    public void BuildUrunDetayUrl_GecerliBaseVeBarkod_TamUrl()
    {
        var url = _sut.BuildUrunDetayUrl("https://example.com/", "8690123456789");

        Assert.Equal("https://example.com/urun/8690123456789", url);
    }

    [Fact]
    public void BuildUrunDetayUrl_BaseSlashsiz_Birlestirir()
    {
        var url = _sut.BuildUrunDetayUrl("https://example.com", "123");

        Assert.Equal("https://example.com/urun/123", url);
    }

    [Fact]
    public void BuildUrunDetayUrl_BosBarkod_Null()
    {
        Assert.Null(_sut.BuildUrunDetayUrl("https://example.com/", null));
        Assert.Null(_sut.BuildUrunDetayUrl("https://example.com/", "   "));
    }

    [Theory]
    [InlineData("https://host/urun/8690123456789", "8690123456789")]
    [InlineData("http://localhost:5000/urun/1234567890123", "1234567890123")]
    [InlineData("/urun/9998887776665", "9998887776665")]
    public void BarkodNoCikar_Urlden_Ayiklar(string scanned, string expected)
    {
        Assert.Equal(expected, _sut.BarkodNoCikar(scanned));
    }

    [Fact]
    public void BarkodNoCikar_DuzMetin_OlduguGibi()
    {
        Assert.Equal("8690123456789", _sut.BarkodNoCikar("8690123456789"));
    }

    [Fact]
    public void BarkodNoCikar_Bos_Null()
    {
        Assert.Null(_sut.BarkodNoCikar(null));
        Assert.Null(_sut.BarkodNoCikar("  "));
    }
}
