namespace QRDestekliStokVeBarkodYonetimi.Services;

/// <summary>
/// Profil kırpma diyalogundan dönen JPEG baytları (circuit kapsamında; büyük byte[] dialog sonucu yerine).
/// </summary>
public class ProfilResimKirpState
{
    public byte[]? KirpilmisJpeg { get; private set; }

    public void KirpilmisJpegAyarla(byte[] bytes) => KirpilmisJpeg = bytes;

    public byte[]? KirpilmisJpegAl()
    {
        var bytes = KirpilmisJpeg;
        KirpilmisJpeg = null;
        return bytes;
    }

    public void Temizle() => KirpilmisJpeg = null;
}
