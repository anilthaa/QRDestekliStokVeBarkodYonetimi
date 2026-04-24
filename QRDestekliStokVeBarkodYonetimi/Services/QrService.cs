using QRCoder;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    public class QrService
    {
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
        /// Boş/null metin gelirse null döner.
        /// </summary>
        public string? GenerateBarcodeBase64(string? barkodNo, int width = 400, int height = 120)
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
            var svgBytes = System.Text.Encoding.UTF8.GetBytes(svg.ToString());
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