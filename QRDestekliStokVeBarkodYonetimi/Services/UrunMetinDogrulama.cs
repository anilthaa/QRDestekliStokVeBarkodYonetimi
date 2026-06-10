using System.Reflection;
using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services;

public static class UrunMetinDogrulama
{
    private static readonly (string Property, string Label)[] KontrolAlanlari =
    [
        (nameof(ItemUrun.UrunKodu), "Ürün kodu"),
        (nameof(ItemUrun.BarkodNo), "Barkod no"),
        (nameof(ItemUrun.Ad), "Ürün adı"),
        (nameof(ItemUrun.Aciklama), "Açıklama"),
        (nameof(ItemUrun.ResimYolu), "Resim yolu"),
    ];

    public static bool SinirAsildi(ItemUrun urun) => IlkHata(urun) is not null;

    public static string? IlkHata(ItemUrun urun)
    {
        foreach (var (property, label) in KontrolAlanlari)
        {
            var max = UrunAlanSinirlari.Sinir(property);
            var deger = typeof(ItemUrun).GetProperty(property, BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(urun) as string;

            if (Asildi(deger, max))
                return $"{label} en fazla {max} karakter olabilir.";
        }

        return null;
    }

    public static bool Asildi(string? deger, int max) =>
        (deger?.Length ?? 0) > max;
}
