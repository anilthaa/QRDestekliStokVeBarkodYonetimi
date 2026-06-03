using System.Text;
using System.Text.RegularExpressions;
using QRCoder;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using Svg.Skia;
using ZXing;
using ZXing.Common;
using ZXing.ImageSharp;
using ZXing.Rendering;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    public class QrService
    {
        private static readonly Regex UrunPathRegex =
            new(@"/urun/([^/?#]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex SvgTextRegex =
            new(@"<text[^>]*>([^<]+)</text>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DosyaAdindanBarkodRegex =
            new(@"^barkod[_-](.+)\.(png|jpe?g|svg|webp|gif)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DosyaAdiGovdeBarkodRegex =
            new(@"^([A-Za-z0-9._%-]{4,})\.(png|jpe?g|svg|webp|gif)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HashSet<string> GorselMimeTurleri = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif", "image/svg+xml"
        };

        private static readonly string[] GorselUzantilar =
            [".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg"];

        private static readonly BarcodeFormat[] TaramaFormatlari =
        [
            BarcodeFormat.CODE_128,
            BarcodeFormat.QR_CODE,
            BarcodeFormat.EAN_13,
            BarcodeFormat.EAN_8,
            BarcodeFormat.CODE_39
        ];

        /// <summary>
        /// Herkese açık ürün detay sayfası URL'si üretir.
        /// </summary>
        public string? BuildUrunDetayUrl(string baseUri, string? barkodNo)
        {
            if (string.IsNullOrWhiteSpace(barkodNo) || string.IsNullOrWhiteSpace(baseUri))
                return null;

            var barkod = barkodNo.Trim();
            var baseNorm = baseUri.TrimEnd('/') + "/";
            return $"{baseNorm}urun/{Uri.EscapeDataString(barkod)}";
        }

        /// <summary>
        /// QR veya manuel girişten barkod numarasını ayıklar (tam URL veya düz metin).
        /// </summary>
        public string? BarkodNoCikar(string? scanned)
        {
            if (string.IsNullOrWhiteSpace(scanned))
                return null;

            var s = scanned.Trim();

            if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
            {
                var fromPath = UrunSegmentindenCikar(uri.AbsolutePath);
                if (fromPath is not null)
                    return fromPath;
            }

            var match = UrunPathRegex.Match(s);
            if (match.Success)
                return Uri.UnescapeDataString(match.Groups[1].Value);

            return s;
        }

        private static string? UrunSegmentindenCikar(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < segments.Length - 1; i++)
            {
                if (string.Equals(segments[i], "urun", StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(segments[i + 1]);
            }

            return null;
        }

        private const float SvgRasterMinGenislik = 1200f;
        private const int SvgRasterPadding = 16;

        /// <summary>
        /// Görsel dosyadan QR veya barkod metnini okur. Okunamazsa null döner.
        /// </summary>
        public string? GoruntudenMetinOku(Stream stream) =>
            GoruntudenMetinOku(stream, null, null);

        /// <summary>
        /// Görsel veya SVG dosyadan QR/barkod metnini okur.
        /// </summary>
        public string? GoruntudenMetinOku(Stream stream, string? contentType, string? fileName)
        {
            if (stream is null || !stream.CanRead)
                return null;

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return GoruntudenMetinOku(ms.ToArray(), contentType, fileName);
        }

        /// <summary>
        /// Görsel bayt dizisinden QR/barkod metnini okur.
        /// </summary>
        public string? GoruntudenMetinOku(byte[] bytes, string? contentType, string? fileName)
        {
            if (bytes is null || bytes.Length == 0)
                return null;

            try
            {
                using var buffer = new MemoryStream(bytes);
                if (SvgMi(contentType, fileName, buffer))
                    return SvgDosyasindanMetinOku(buffer);

                return RasterDosyasindanMetinOku(bytes, fileName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Stok dosya yüklemesi için MIME/uzantı veya dosya başlığına göre görsel izni.
        /// </summary>
        public static bool GorselDosyaIzinliMi(byte[] bytes, string? contentType, string? fileName)
        {
            if (bytes.Length > 0
                && !string.IsNullOrWhiteSpace(contentType)
                && GorselMimeTurleri.Contains(contentType))
                return true;

            if (UzantidanGorselIzinli(fileName))
                return true;

            if (BelirsizMime(contentType) && bytes.Length > 0 && GorselBasliktanTanima(bytes))
                return true;

            return false;
        }

        private static bool BelirsizMime(string? contentType) =>
            string.IsNullOrWhiteSpace(contentType)
            || string.Equals(contentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase);

        private static bool UzantidanGorselIzinli(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            return GorselUzantilar.Any(ext =>
                fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        private static bool GorselBasliktanTanima(byte[] bytes)
        {
            if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return true;

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8)
                return true;

            if (bytes.Length >= 3
                && bytes[0] == (byte)'G' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F')
                return true;

            if (bytes.Length >= 12
                && bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F'
                && bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
                return true;

            var peekLen = Math.Min(256, bytes.Length);
            var header = BomTemizle(Encoding.UTF8.GetString(bytes.AsSpan(0, peekLen)));
            return header.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
                || header.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase);
        }

        private static bool SvgMi(string? contentType, string? fileName, MemoryStream buffer)
        {
            if (string.Equals(contentType, "image/svg+xml", StringComparison.OrdinalIgnoreCase))
                return true;

            if (fileName?.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) == true)
                return true;

            if (buffer.Length == 0)
                return false;

            var peekLen = (int)Math.Min(256, buffer.Length);
            buffer.Position = 0;
            var peek = new byte[peekLen];
            _ = buffer.Read(peek, 0, peekLen);
            buffer.Position = 0;
            var header = BomTemizle(Encoding.UTF8.GetString(peek));
            return header.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
                || header.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase);
        }

        private string? SvgDosyasindanMetinOku(MemoryStream buffer)
        {
            var xml = SvgXmlOku(buffer);

            string? metin = null;
            using (var svgStream = Utf8BomTemizlenmisStream(buffer))
            {
                svgStream.Position = 0;
                using var png = SvgyiPngStreameDonustur(svgStream);
                if (png is not null)
                {
                    using var image = Image.Load<Rgba32>(png);
                    metin = BarkodMetniCozGelistirilmis(image);
                }
            }

            if (string.IsNullOrWhiteSpace(metin) && !string.IsNullOrWhiteSpace(xml))
                metin = SvgMetnindenBarkodCikar(xml);

            if (string.IsNullOrWhiteSpace(metin))
                return null;

            return BarkodNoCikar(metin) ?? metin.Trim();
        }

        private static string SvgXmlOku(MemoryStream buffer)
        {
            buffer.Position = 0;
            var bytes = Utf8BomKaldir(buffer.ToArray());
            return Encoding.UTF8.GetString(bytes);
        }

        private static MemoryStream Utf8BomTemizlenmisStream(MemoryStream buffer)
        {
            buffer.Position = 0;
            var bytes = Utf8BomKaldir(buffer.ToArray());
            return new MemoryStream(bytes);
        }

        private static byte[] Utf8BomKaldir(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return bytes.AsSpan(3).ToArray();
            return bytes;
        }

        private static string BomTemizle(string text) =>
            text.TrimStart('\uFEFF').TrimStart();

        private static string? SvgMetnindenBarkodCikar(string xml)
        {
            string? enIyi = null;
            foreach (Match match in SvgTextRegex.Matches(xml))
            {
                var aday = match.Groups[1].Value.Trim();
                if (string.IsNullOrWhiteSpace(aday))
                    continue;
                if (enIyi is null || aday.Length > enIyi.Length)
                    enIyi = aday;
            }

            return enIyi;
        }

        private static MemoryStream? SvgyiPngStreameDonustur(Stream svgStream)
        {
            using var svg = new SKSvg();
            if (svg.Load(svgStream) is null)
                return null;

            var picture = svg.Picture;
            if (picture is null)
                return null;

            var bounds = picture.CullRect;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return null;

            var scale = Math.Max(2f, SvgRasterMinGenislik / bounds.Width);
            var contentW = bounds.Width * scale;
            var contentH = bounds.Height * scale;
            var width = (int)Math.Ceiling(contentW) + SvgRasterPadding * 2;
            var height = (int)Math.Ceiling(contentH) + SvgRasterPadding * 2;

            using var bitmap = new SKBitmap(width, height);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White);
                canvas.Translate(SvgRasterPadding, SvgRasterPadding);
                canvas.Scale(scale);
                canvas.Translate(-bounds.Left, -bounds.Top);
                canvas.DrawPicture(picture);
                canvas.Flush();
            }

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            if (data is null)
                return null;

            return new MemoryStream(data.ToArray());
        }

        private string? RasterDosyasindanMetinOku(byte[] bytes, string? fileName)
        {
            var metin = RasterBaytDizisindenMetinOku(bytes);

            if (string.IsNullOrWhiteSpace(metin))
                metin = DosyaAdindanBarkodCikar(fileName);

            if (string.IsNullOrWhiteSpace(metin))
                return null;

            return BarkodNoCikar(metin) ?? metin.Trim();
        }

        private static string? RasterBaytDizisindenMetinOku(byte[] bytes)
        {
            string? metin = null;

            try
            {
                using var ms = new MemoryStream(bytes);
                using var image = Image.Load<Rgba32>(ms);
                metin = BarkodMetniCozGelistirilmis(image);
            }
            catch
            {
                // ImageSharp decode başarısız — Skia yedeği
            }

            if (!string.IsNullOrWhiteSpace(metin))
                return metin;

            try
            {
                using var ms = new MemoryStream(bytes);
                using var png = SkiaBayttanPngStream(ms);
                if (png is not null)
                {
                    using var image = Image.Load<Rgba32>(png);
                    metin = BarkodMetniCozGelistirilmis(image);
                }
            }
            catch
            {
                // Skia yedeği de başarısız
            }

            return metin;
        }

        private static MemoryStream? SkiaBayttanPngStream(Stream stream)
        {
            using var bitmap = SKBitmap.Decode(stream);
            if (bitmap is null)
                return null;

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            if (data is null)
                return null;

            return new MemoryStream(data.ToArray());
        }

        private static string? BarkodMetniCozGelistirilmis(Image<Rgba32> image)
        {
            var metin = BarkodMetniCoz(image);
            if (!string.IsNullOrWhiteSpace(metin))
                return metin;

            using (var processed = image.Clone(ctx => ctx.Grayscale().Contrast(1.4f)))
            {
                metin = BarkodMetniCoz(processed);
                if (!string.IsNullOrWhiteSpace(metin))
                    return metin;
            }

            foreach (var olcek in new[] { 2, 3, 4, 5 })
            {
                using var buyuk = image.Clone(ctx => ctx.Resize(image.Width * olcek, image.Height * olcek));
                metin = BarkodMetniCoz(buyuk);
                if (!string.IsNullOrWhiteSpace(metin))
                    return metin;

                using var buyukIslenmis = buyuk.Clone(ctx => ctx.Grayscale().Contrast(1.4f));
                metin = BarkodMetniCoz(buyukIslenmis);
                if (!string.IsNullOrWhiteSpace(metin))
                    return metin;
            }

            return null;
        }

        private static string? BarkodMetniCoz(Image<Rgba32> image)
        {
            var reader = new ZXing.ImageSharp.BarcodeReader<Rgba32>
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    TryHarder = true,
                    TryInverted = true,
                    PossibleFormats = TaramaFormatlari
                }
            };
            var text = reader.Decode(image)?.Text;
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private static string? DosyaAdindanBarkodCikar(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            var ad = Path.GetFileName(fileName.Trim());
            var match = DosyaAdindanBarkodRegex.Match(ad);
            if (match.Success)
                return Uri.UnescapeDataString(match.Groups[1].Value.Trim());

            var govdeMatch = DosyaAdiGovdeBarkodRegex.Match(ad);
            if (!govdeMatch.Success)
                return null;

            var govde = govdeMatch.Groups[1].Value.Trim();
            if (govde.StartsWith("barkod", StringComparison.OrdinalIgnoreCase))
                return null;

            return Uri.UnescapeDataString(govde);
        }

        private const int BarkodExportGenislik = 600;
        private const int BarkodExportYukseklik = 180;

        private static Image<Rgba32>? BarkodGorseliOlustur(string barkodNo, int width = BarkodExportGenislik, int height = BarkodExportYukseklik)
        {
            var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width       = width,
                    Height      = height,
                    Margin      = 10,
                    PureBarcode = true
                }
            };

            return writer.Write(barkodNo.Trim());
        }

        /// <summary>
        /// Verilen metinden QR kod üretir ve base64 PNG string olarak döner.
        /// Boş/null metin gelirse null döner.
        /// </summary>
        public string? GenerateQrBase64(string? text, int pixelsPerModule = 10)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            using var generator = new QRCodeGenerator();
            using var data      = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            using var code      = new PngByteQRCode(data);

            var bytes = code.GetGraphic(pixelsPerModule);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Verilen barkod numarasından CODE_128 barkod görüntüsü üretir ve base64 PNG olarak döner.
        /// </summary>
        public string? GenerateBarcodePngBase64(string? barkodNo, int width = BarkodExportGenislik, int height = BarkodExportYukseklik)
        {
            if (string.IsNullOrWhiteSpace(barkodNo))
                return null;

            using var image = BarkodGorseliOlustur(barkodNo, width, height);
            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Verilen barkod numarasından CODE_128 barkod JPEG üretir ve base64 olarak döner.
        /// </summary>
        public string? GenerateBarcodeJpegBase64(string? barkodNo, int width = BarkodExportGenislik, int height = BarkodExportYukseklik)
        {
            if (string.IsNullOrWhiteSpace(barkodNo))
                return null;

            using var image = BarkodGorseliOlustur(barkodNo, width, height);
            using var ms = new MemoryStream();
            image.SaveAsJpeg(ms, new JpegEncoder { Quality = 90 });
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// Verilen barkod numarasından CODE_128 barkod SVG üretir ve base64 olarak döner.
        /// </summary>
        public string? GenerateBarcodeSvgBase64(string? barkodNo, int width = BarkodExportGenislik, int height = BarkodExportYukseklik)
        {
            if (string.IsNullOrWhiteSpace(barkodNo))
                return null;

            var writer = new BarcodeWriterSvg
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width       = width,
                    Height      = height,
                    Margin      = 10,
                    PureBarcode = false
                }
            };

            var svg = writer.Write(barkodNo);
            var svgBytes = Encoding.UTF8.GetBytes(svg.ToString());
            return Convert.ToBase64String(svgBytes);
        }

        /// <summary>
        /// 13 haneli rastgele EAN-13 benzeri barkod numarası üretir.
        /// </summary>
        public string GenerateRandomBarkod()
        {
            var rng    = Random.Shared;
            var digits = new int[12];
            for (int i = 0; i < 12; i++)
                digits[i] = rng.Next(0, 10);

            // EAN-13 check digit
            int sum = 0;
            for (int i = 0; i < 12; i++)
                sum += digits[i] * (i % 2 == 0 ? 1 : 3);

            int check = (10 - (sum % 10)) % 10;

            return string.Concat(digits) + check;
        }
    }
}