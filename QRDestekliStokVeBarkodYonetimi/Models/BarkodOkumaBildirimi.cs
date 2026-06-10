namespace QRDestekliStokVeBarkodYonetimi.Models;

public enum BarkodTaramaKaynagi
{
    Manuel,
    Dosya,
    Kamera
}

public record BarkodOkumaBildirimi(string Barkod, BarkodTaramaKaynagi Kaynak);
