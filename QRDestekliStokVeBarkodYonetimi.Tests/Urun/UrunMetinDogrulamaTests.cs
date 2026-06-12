using QRDestekliStokVeBarkodYonetimi.Models;
using QRDestekliStokVeBarkodYonetimi.Services;

namespace QRDestekliStokVeBarkodYonetimi.Tests.Urun;

public class UrunMetinDogrulamaTests
{
    private static ItemUrun GecerliUrun() => new()
    {
        UrunKodu = new string('K', UrunAlanSinirlari.UrunKodu),
        BarkodNo = new string('1', UrunAlanSinirlari.BarkodNo),
        Ad = new string('A', UrunAlanSinirlari.Ad),
        Aciklama = new string('C', UrunAlanSinirlari.Aciklama)
    };

    [Fact]
    public void SinirAsildi_Sinirda_Gecerli()
    {
        var urun = GecerliUrun();
        Assert.False(UrunMetinDogrulama.SinirAsildi(urun));
        Assert.Null(UrunMetinDogrulama.IlkHata(urun));
    }

    [Fact]
    public void SinirAsildi_UrunKoduAsim_HataDondurur()
    {
        var urun = GecerliUrun();
        urun.UrunKodu = new string('K', UrunAlanSinirlari.UrunKodu + 1);

        Assert.True(UrunMetinDogrulama.SinirAsildi(urun));
        Assert.Contains("Ürün kodu", UrunMetinDogrulama.IlkHata(urun));
    }

    [Fact]
    public void SinirAsildi_AdAsim_HataDondurur()
    {
        var urun = GecerliUrun();
        urun.Ad = new string('A', UrunAlanSinirlari.Ad + 1);

        Assert.True(UrunMetinDogrulama.SinirAsildi(urun));
        Assert.Contains("Ürün adı", UrunMetinDogrulama.IlkHata(urun));
    }

    [Fact]
    public void SinirAsildi_BarkodNoHarf_HataDondurur()
    {
        var urun = GecerliUrun();
        urun.BarkodNo = "ABC123";

        Assert.True(UrunMetinDogrulama.SinirAsildi(urun));
        Assert.Contains("Barkod no yalnızca rakam", UrunMetinDogrulama.IlkHata(urun));
    }

    [Fact]
    public void SinirAsildi_BosOpsiyonelAlanlar_Gecerli()
    {
        var urun = GecerliUrun();
        urun.BarkodNo = null;
        urun.Aciklama = null;

        Assert.False(UrunMetinDogrulama.SinirAsildi(urun));
    }

    [Fact]
    public void UrunAlanSinirlari_ItemUrunStringLengthIleUyumlu()
    {
        Assert.Equal(50, UrunAlanSinirlari.UrunKodu);
        Assert.Equal(20, UrunAlanSinirlari.BarkodNo);
        Assert.Equal(150, UrunAlanSinirlari.Ad);
        Assert.Equal(250, UrunAlanSinirlari.Aciklama);
        Assert.Equal(500, UrunAlanSinirlari.ResimYolu);
    }
}
