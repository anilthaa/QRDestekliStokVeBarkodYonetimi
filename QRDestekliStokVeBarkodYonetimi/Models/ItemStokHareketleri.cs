namespace QRDestekliStokVeBarkodYonetimi.Models
{
    public class ItemStokHareketleri : ItemBase
    {
        public int Urun_ID { get; set; }
        public short HareketTipi { get; set; }
        public decimal Miktar { get; set; }
        public string? Not { get; set; }

        // JOIN ile doldurulan görüntüleme alanları
        public string? UrunAd { get; set; }
        public string? UrunKodu { get; set; }
        public string? ResimYolu { get; set; }
        public string? BarkodNo { get; set; }
    }
}
