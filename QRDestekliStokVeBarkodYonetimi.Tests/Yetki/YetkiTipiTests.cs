using QRDestekliStokVeBarkodYonetimi.Models;
using Xunit;

namespace QRDestekliStokVeBarkodYonetimi.Tests.Yetki;

/// <summary>
/// YetkiTipi sabitlerini ve Label() metodunu doğrular.
/// </summary>
public class YetkiTipiTests
{
    [Fact(DisplayName = "Sabitler: beklenen tamsayı değerleri")]
    public void Sabitler_BeklenenDegerler()
    {
        Assert.Equal(-1, YetkiTipi.KalitimYok);
        Assert.Equal(0,  YetkiTipi.Gizli);
        Assert.Equal(1,  YetkiTipi.Okuma);
        Assert.Equal(2,  YetkiTipi.Yazma);
    }

    [Theory(DisplayName = "Label: her değer için doğru etiket")]
    [InlineData(YetkiTipi.KalitimYok, "Tip Yetkisi")]
    [InlineData(YetkiTipi.Gizli,      "Gizli")]
    [InlineData(YetkiTipi.Okuma,      "Okuma")]
    [InlineData(YetkiTipi.Yazma,      "Yazma")]
    public void Label_TumDegerler_DogruEtiket(int yetki, string beklenen)
    {
        Assert.Equal(beklenen, YetkiTipi.Label(yetki));
    }

    [Fact(DisplayName = "Label: bilinmeyen değer → '?'")]
    public void Label_BilinmeyenDeger_SoruIsareti()
    {
        Assert.Equal("?", YetkiTipi.Label(99));
    }

    [Fact(DisplayName = "Öncelik: Yazma > Okuma > Gizli > KalitimYok")]
    public void Sabitler_SiraliBuyukluguDogrular()
    {
        Assert.True(YetkiTipi.Yazma > YetkiTipi.Okuma);
        Assert.True(YetkiTipi.Okuma > YetkiTipi.Gizli);
        Assert.True(YetkiTipi.Gizli > YetkiTipi.KalitimYok);
    }
}
