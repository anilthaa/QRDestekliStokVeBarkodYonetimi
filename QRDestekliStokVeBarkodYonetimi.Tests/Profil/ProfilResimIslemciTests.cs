using QRDestekliStokVeBarkodYonetimi.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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

    [Fact]
    public async Task ProfilGifKaydet_AnimasyonluGif_CokKareli256x256()
    {
        var islemci = new ProfilResimIslemci();
        var temp = Path.Combine(Path.GetTempPath(), $"profil-gif-test-{Guid.NewGuid()}.gif");

        try
        {
            await using var kaynak = new MemoryStream();
            using (var img = new Image<Rgba32>(120, 80, Color.Red))
            {
                using var maviKare = new Image<Rgba32>(120, 80, Color.Blue);
                img.Frames.AddFrame(maviKare.Frames.RootFrame);
                await img.SaveAsGifAsync(kaynak);
            }

            kaynak.Position = 0;
            await islemci.ProfilGifKaydetAsync(kaynak, temp);

            Assert.True(File.Exists(temp));
            using var cikti = await Image.LoadAsync(temp);
            Assert.Equal("GIF", cikti.Metadata.DecodedImageFormat?.Name);
            Assert.True(cikti.Frames.Count > 1);
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
