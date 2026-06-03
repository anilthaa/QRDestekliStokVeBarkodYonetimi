using QRDestekliStokVeBarkodYonetimi.Components.Shared;
using Xunit;

namespace QRDestekliStokVeBarkodYonetimi.Tests.Shared;

public class GridFilterHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Eslesir_BosFiltre_HerZamanTrue(string? filtre)
    {
        Assert.True(GridFilterHelper.Eslesir(42, filtre));
        Assert.True(GridFilterHelper.Eslesir("test", filtre));
    }

    [Theory]
    [InlineData(123, "12", true)]
    [InlineData(123, "99", false)]
    [InlineData(5.5, "5", true)]
    [InlineData(true, "tru", true)]
    public void Eslesir_KismiMetinEslesmesi(object deger, string filtre, bool beklenen)
    {
        Assert.Equal(beklenen, GridFilterHelper.Eslesir(deger, filtre));
    }

    [Fact]
    public void IdEslesir_BosFiltre_HerZamanTrue()
    {
        Assert.True(GridFilterHelper.IdEslesir(5, null));
    }

    [Theory]
    [InlineData(3, 3, true)]
    [InlineData(3, 4, false)]
    public void IdEslesir_SeciliId_TamEslesme(int id, int filtreId, bool beklenen)
    {
        Assert.Equal(beklenen, GridFilterHelper.IdEslesir(id, filtreId));
    }

    [Fact]
    public void Eslesir_BuyukKucukHarfDuyarsiz()
    {
        Assert.True(GridFilterHelper.Eslesir("Deneme", "deneme"));
        Assert.True(GridFilterHelper.Eslesir("Deneme", "DENE"));
        Assert.False(GridFilterHelper.Eslesir("Deneme", "xyz"));
    }

    [Fact]
    public void BoolEslesir_BosFiltre_HerZamanTrue()
    {
        Assert.True(GridFilterHelper.BoolEslesir(true, null));
        Assert.True(GridFilterHelper.BoolEslesir(false, null));
        Assert.True(GridFilterHelper.BoolEslesir(null, null));
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(null, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, false, true)]
    [InlineData(null, false, true)]
    public void BoolEslesir_EvetHayir(bool? deger, bool filtre, bool beklenen)
    {
        Assert.Equal(beklenen, GridFilterHelper.BoolEslesir(deger, filtre));
    }
}
