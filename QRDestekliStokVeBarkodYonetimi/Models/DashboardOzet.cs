namespace QRDestekliStokVeBarkodYonetimi.Models
{
    public class DashboardOzet
    {
        public int ToplamUrun { get; set; }
        public int ToplamKategori { get; set; }
        public int ToplamKullanici { get; set; }
        public int ToplamHareket { get; set; }
        public int BugunkuHareket { get; set; }
        public int KritikStokSayisi { get; set; }
        public decimal BugunkuGiris { get; set; }
        public decimal BugunkuCikis { get; set; }
    }
}
