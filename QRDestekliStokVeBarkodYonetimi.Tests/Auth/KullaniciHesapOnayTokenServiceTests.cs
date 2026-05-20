using QRDestekliStokVeBarkodYonetimi.Services;
using Xunit;

namespace QRDestekliStokVeBarkodYonetimi.Tests.Auth;

public sealed class KullaniciHesapOnayTokenServiceTests
{
    [Fact]
    public void TryConsume_GecerliToken_TekSeferdeTukenir()
    {
        var sut = new KullaniciHesapOnayTokenService();
        var token = sut.OlusturKaydet(7);

        Assert.True(sut.TryConsume(token, out var userId));
        Assert.Equal(7, userId);

        Assert.False(sut.TryConsume(token, out _));
    }

    [Fact]
    public void OlusturKaydet_AyniKullaniciIcin_EskiTokenGecersizOlur()
    {
        var sut = new KullaniciHesapOnayTokenService();
        var eski = sut.OlusturKaydet(3);
        var yeni = sut.OlusturKaydet(3);

        Assert.False(sut.TryConsume(eski, out _));
        Assert.True(sut.TryConsume(yeni, out var userId));
        Assert.Equal(3, userId);
    }
}
