using System.Text;
using QRDestekliStokVeBarkodYonetimi.Services;
using Xunit;

namespace QRDestekliStokVeBarkodYonetimi.Tests.Services;

public class QrServiceGoruntuTests
{
    private readonly QrService _sut = new();

    [Fact]
    public void GoruntudenMetinOku_UretilenQrPng_MetniDondurur()
    {
        const string beklenen = "8690123456789";
        var base64 = _sut.GenerateQrBase64(beklenen, pixelsPerModule: 8);
        Assert.NotNull(base64);

        var pngBytes = Convert.FromBase64String(base64!);
        using var stream = new MemoryStream(pngBytes);

        var okunan = _sut.GoruntudenMetinOku(stream);

        Assert.Equal(beklenen, okunan);
    }

    [Fact]
    public void GoruntudenMetinOku_GecersizIcerik_Null()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));

        Assert.Null(_sut.GoruntudenMetinOku(stream));
    }

    [Fact]
    public void GoruntudenMetinOku_BosStream_Null()
    {
        using var stream = new MemoryStream();

        Assert.Null(_sut.GoruntudenMetinOku(stream));
    }

    [Fact]
    public void GoruntudenMetinOku_UretilenBarkodSvg_MetniDondurur()
    {
        const string beklenen = "8690123456789";
        var base64 = _sut.GenerateBarcodeSvgBase64(beklenen);
        Assert.NotNull(base64);

        var svgBytes = Convert.FromBase64String(base64!);
        using var stream = new MemoryStream(svgBytes);

        var okunan = _sut.GoruntudenMetinOku(stream, "image/svg+xml", "barkod_8690123456789.svg");

        Assert.Equal(beklenen, okunan);
    }

    [Fact]
    public void GoruntudenMetinOku_UretilenBarkodPng_MetniDondurur()
    {
        const string beklenen = "8690123456789";
        var base64 = _sut.GenerateBarcodePngBase64(beklenen);
        Assert.NotNull(base64);

        var pngBytes = Convert.FromBase64String(base64!);
        using var stream = new MemoryStream(pngBytes);

        var okunan = _sut.GoruntudenMetinOku(stream, "image/png", "barkod_8690123456789.png");

        Assert.Equal(beklenen, okunan);
    }

    [Fact]
    public void GoruntudenMetinOku_UretilenBarkodJpeg_MetniDondurur()
    {
        const string beklenen = "8690123456789";
        var base64 = _sut.GenerateBarcodeJpegBase64(beklenen);
        Assert.NotNull(base64);

        var jpegBytes = Convert.FromBase64String(base64!);
        using var stream = new MemoryStream(jpegBytes);

        var okunan = _sut.GoruntudenMetinOku(stream, "image/jpeg", "barkod_8690123456789.jpg");

        Assert.Equal(beklenen, okunan);
    }

    [Fact]
    public void GoruntudenMetinOku_GecersizRaster_DosyaAdiYedegi_MetniDondurur()
    {
        const string beklenen = "8690123456789";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));

        var okunan = _sut.GoruntudenMetinOku(stream, "image/png", "barkod_8690123456789.png");

        Assert.Equal(beklenen, okunan);
    }

    [Fact]
    public void GoruntudenMetinOku_UretilenBarkodSvg_Utf8Bom_MetniDondurur()
    {
        const string beklenen = "8690123456789";
        var base64 = _sut.GenerateBarcodeSvgBase64(beklenen);
        Assert.NotNull(base64);

        var svgBytes = Encoding.UTF8.GetPreamble()
            .Concat(Convert.FromBase64String(base64!))
            .ToArray();
        using var stream = new MemoryStream(svgBytes);

        var okunan = _sut.GoruntudenMetinOku(stream, "image/svg+xml", "barkod.svg");

        Assert.Equal(beklenen, okunan);
    }

    [Fact]
    public void GoruntudenMetinOku_BarkodSvg_YalnizcaTextEtiketi_MetniDondurur()
    {
        const string beklenen = "1234567890123";
        var xml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 50">
              <text x="10" y="40" font-size="12">{beklenen}</text>
            </svg>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        var okunan = _sut.GoruntudenMetinOku(stream, "image/svg+xml", "barkod.svg");

        Assert.Equal(beklenen, okunan);
    }

    [Fact]
    public void GoruntudenMetinOku_UretilenBarkodPng_OctetStreamMime_MetniDondurur()
    {
        const string beklenen = "8690123456789";
        var base64 = _sut.GenerateBarcodePngBase64(beklenen);
        Assert.NotNull(base64);

        var pngBytes = Convert.FromBase64String(base64!);
        var okunan = _sut.GoruntudenMetinOku(pngBytes, "application/octet-stream", "barkod_8690123456789.png");

        Assert.Equal(beklenen, okunan);
    }

    [Fact]
    public void GoruntudenMetinOku_UretilenBarkodPng_DuzDosyaAdiYedegi_MetniDondurur()
    {
        const string beklenen = "8690123456789";
        var base64 = _sut.GenerateBarcodePngBase64(beklenen);
        Assert.NotNull(base64);

        var pngBytes = Convert.FromBase64String(base64!);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("not an image"));

        var okunan = _sut.GoruntudenMetinOku(stream, "image/png", $"{beklenen}.png");

        Assert.Equal(beklenen, okunan);
    }

    [Fact]
    public void GorselDosyaIzinliMi_PngBaslik_OctetStream_True()
    {
        const string beklenen = "8690123456789";
        var pngBytes = Convert.FromBase64String(_sut.GenerateBarcodePngBase64(beklenen)!);

        Assert.True(QrService.GorselDosyaIzinliMi(pngBytes, "application/octet-stream", "scan.png"));
    }
}
