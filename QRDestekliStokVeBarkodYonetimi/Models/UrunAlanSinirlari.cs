using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace QRDestekliStokVeBarkodYonetimi.Models;

/// <summary>
/// Ürün metin alanı karakter üst sınırları — <see cref="ItemUrun"/> üzerindeki
/// <see cref="StringLengthAttribute"/> değerlerinden okunur (tek kaynak).
/// </summary>
public static class UrunAlanSinirlari
{
    public static int UrunKodu => Sinir(nameof(ItemUrun.UrunKodu));
    public static int BarkodNo => Sinir(nameof(ItemUrun.BarkodNo));
    public static int Ad => Sinir(nameof(ItemUrun.Ad));
    public static int Aciklama => Sinir(nameof(ItemUrun.Aciklama));
    public static int ResimYolu => Sinir(nameof(ItemUrun.ResimYolu));

    internal static int Sinir(string propertyName)
    {
        var prop = typeof(ItemUrun).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"ItemUrun.{propertyName} bulunamadı.");

        var attr = prop.GetCustomAttribute<StringLengthAttribute>();
        if (attr?.MaximumLength is not int max)
            throw new InvalidOperationException($"ItemUrun.{propertyName} için StringLength tanımlı değil.");

        return max;
    }
}
