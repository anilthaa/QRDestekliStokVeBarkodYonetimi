namespace QRDestekliStokVeBarkodYonetimi.Models
{
    /// <summary>
    /// Form yetki seviyeleri.
    ///
    ///  -1 = KalitimYok  → Kullanıcıya özel override kaydı kaldırılır; tip yetkisi devreye girer.
    ///   0 = Gizli        → Kullanıcıya özel "erişim engeli" kaydı (tip=Yazma olsa bile gizlenir).
    ///   1 = Okuma        → Kullanıcıya özel okuma override'ı.
    ///   2 = Yazma        → Kullanıcıya özel yazma override'ı.
    /// </summary>
    public static class YetkiTipi
    {
        /// <summary>Kullanıcıya özel kaydı sil; etkin yetki tip yetkisine döner.</summary>
        public const int KalitimYok = -1;

        /// <summary>Erişim engeli (gizle).</summary>
        public const int Gizli = 0;

        /// <summary>Yalnızca okuma erişimi.</summary>
        public const int Okuma = 1;

        /// <summary>Tam yazma erişimi.</summary>
        public const int Yazma = 2;

        /// <summary>Yetki seviyesinin kullanıcıya gösterilecek etiketini döner.</summary>
        public static string Label(int yetki) => yetki switch
        {
            KalitimYok => "Tip Yetkisi",
            Gizli      => "Gizli",
            Okuma      => "Okuma",
            Yazma      => "Yazma",
            _          => "?"
        };
    }
}