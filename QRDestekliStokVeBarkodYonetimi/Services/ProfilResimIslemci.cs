using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace QRDestekliStokVeBarkodYonetimi.Services;

public class ProfilResimIslemci
{
    public const int CiktiBoyut = 256;
    private const int OnizlemeMaxKenar = 1280;

    /// <summary>
    /// Kırpma diyalogu için küçültülmüş JPEG data URL (SignalR yükünü azaltır).
    /// </summary>
    public async Task<string> OnizlemeDataUrlOlusturAsync(Stream kaynak, string? contentType, CancellationToken ct = default)
    {
        if (string.Equals(contentType, "image/gif", StringComparison.OrdinalIgnoreCase))
        {
            using var gifBuffer = new MemoryStream();
            await kaynak.CopyToAsync(gifBuffer, ct);
            return $"data:image/gif;base64,{Convert.ToBase64String(gifBuffer.ToArray())}";
        }

        using var image = await Image.LoadAsync(kaynak, ct);
        var maxKenar = Math.Max(image.Width, image.Height);
        if (maxKenar > OnizlemeMaxKenar)
        {
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(OnizlemeMaxKenar, OnizlemeMaxKenar),
                Mode = ResizeMode.Max
            }));
        }

        using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 88 }, ct);
        return $"data:image/jpeg;base64,{Convert.ToBase64String(ms.ToArray())}";
    }

    public async Task KareProfilKaydetAsync(Stream kaynak, string hedefDosyaYolu, CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(kaynak, ct);

        var kenar = Math.Min(image.Width, image.Height);
        var x = (image.Width - kenar) / 2;
        var y = (image.Height - kenar) / 2;

        image.Mutate(ctx => ctx
            .Crop(new Rectangle(x, y, kenar, kenar))
            .Resize(CiktiBoyut, CiktiBoyut));

        var dizin = Path.GetDirectoryName(hedefDosyaYolu);
        if (!string.IsNullOrEmpty(dizin))
            Directory.CreateDirectory(dizin);

        await image.SaveAsJpegAsync(hedefDosyaYolu, new JpegEncoder { Quality = 85 }, ct);
    }

    /// <summary>
    /// Kırpılmış kare JPEG akışını profil boyutuna indirip kaydeder.
    /// </summary>
    public async Task ProfilJpegKaydetAsync(Stream kaynak, string hedefDosyaYolu, CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(kaynak, ct);

        image.Mutate(ctx => ctx.Resize(CiktiBoyut, CiktiBoyut));

        var dizin = Path.GetDirectoryName(hedefDosyaYolu);
        if (!string.IsNullOrEmpty(dizin))
            Directory.CreateDirectory(dizin);

        await image.SaveAsJpegAsync(hedefDosyaYolu, new JpegEncoder { Quality = 85 }, ct);
    }

    /// <summary>
    /// GIF profil resmini animasyonu koruyarak kare 256px boyutuna indirip kaydeder.
    /// </summary>
    public async Task ProfilGifKaydetAsync(Stream kaynak, string hedefDosyaYolu, CancellationToken ct = default)
    {
        using var image = await Image.LoadAsync(kaynak, ct);

        image.Mutate(ctx => ctx.Resize(CiktiBoyut, CiktiBoyut));

        var dizin = Path.GetDirectoryName(hedefDosyaYolu);
        if (!string.IsNullOrEmpty(dizin))
            Directory.CreateDirectory(dizin);

        await image.SaveAsGifAsync(hedefDosyaYolu, new GifEncoder(), ct);
    }
}
