using QRDestekliStokVeBarkodYonetimi.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace QRDestekliStokVeBarkodYonetimi.Tests.Profil;

public class ProfilResimIslemciTests
{
    [Fact]
    public async Task KareProfilKaydet_200x100_Cikti256x256()
    {
        var islemci = new ProfilResimIslemci();
        var temp = Path.Combine(Path.GetTempPath(), $"profil-test-{Guid.NewGuid()}.jpg");

        try
        {
            await using var kaynak = new MemoryStream();
            using (var img = new Image<Rgba32>(200, 100))
            {
                await img.SaveAsPngAsync(kaynak);
            }

            kaynak.Position = 0;
            await islemci.KareProfilKaydetAsync(kaynak, temp);

            Assert.True(File.Exists(temp));
            using var cikti = await Image.LoadAsync(temp);
            Assert.Equal(ProfilResimIslemci.CiktiBoyut, cikti.Width);
            Assert.Equal(ProfilResimIslemci.CiktiBoyut, cikti.Height);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }
}
