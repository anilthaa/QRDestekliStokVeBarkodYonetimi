namespace QRDestekliStokVeBarkodYonetimi.Components.Shared
{
    public static class GridFilterHelper
    {
        public static bool Eslesir(object? deger, string? filtre) =>
            string.IsNullOrWhiteSpace(filtre) ||
            (deger?.ToString()?.Contains(filtre, StringComparison.OrdinalIgnoreCase) ?? false);

        public static bool IdEslesir(int id, int? filtreId) =>
            !filtreId.HasValue || id == filtreId.Value;

        public static bool BoolEslesir(bool? deger, bool? filtre) =>
            !filtre.HasValue || (deger == true) == filtre.Value;
    }
}
