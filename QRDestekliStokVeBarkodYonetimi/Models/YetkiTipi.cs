namespace QRDestekliStokVeBarkodYonetimi.Models
{
    /// <summary>
    /// Form yetki seviyeleri: 0=Gizli, 1=Okuma, 2=Yazma
    /// </summary>
    public static class YetkiTipi
    {
        public const int Gizli = 0;
        public const int Okuma = 1;
        public const int Yazma = 2;

        public static string Label(int yetki) => yetki switch
        {
            0 => "Gizli",
            1 => "Okuma",
            2 => "Yazma",
            _ => "?"
        };
    }
}