using System.Collections;
using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services;

public static class GridSiraYardimci
{
    public const string KolonGenisligi = "64px";

    public static int ListeSirasi<T>(IReadOnlyList<T> liste, T satir) where T : class
        => ExportSirasi(liste, satir);

    public static int ExportSirasi<T>(IReadOnlyList<T> liste, T satir) where T : class
    {
        for (var i = 0; i < liste.Count; i++)
        {
            if (ReferenceEquals(liste[i], satir) || EqualityComparer<T>.Default.Equals(liste[i], satir))
                return i + 1;
        }

        return 0;
    }

    public static IComparer SiralamaIndeksiComparer<T>(IReadOnlyList<T> liste)
        where T : ItemBase
    {
        var indeks = new Dictionary<int, int>();
        for (var i = 0; i < liste.Count; i++)
            indeks[liste[i].ID] = i;

        return Comparer<object>.Create((a, b) =>
        {
            var ia = indeks.TryGetValue(Convert.ToInt32(a), out var va) ? va : int.MaxValue;
            var ib = indeks.TryGetValue(Convert.ToInt32(b), out var vb) ? vb : int.MaxValue;
            return ia.CompareTo(ib);
        });
    }
}
