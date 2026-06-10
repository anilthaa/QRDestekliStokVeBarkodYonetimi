namespace QRDestekliStokVeBarkodYonetimi.Services;

public static class UrunResimYardimci
{
    /// <summary>
    /// Noktalı virgülle birleştirilmiş resim yollarından ilkini döndürür.
    /// </summary>
    public static string? IlkResimYolu(string? resimYolu)
    {
        if (string.IsNullOrWhiteSpace(resimYolu))
            return null;

        return resimYolu
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
    }
}
