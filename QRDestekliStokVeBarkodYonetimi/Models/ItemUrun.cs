namespace QRDestekliStokVeBarkodYonetimi.Models
{
    public class ItemUrun:ItemBase
    {
        public string UrunKodu { get; set; }
        public int Kategori_ID { get; set; }
        public string? BarkodNo { get; set; }
        public string? ResimYolu { get; set; }
        public string Ad { get; set; }
        public string? Aciklama { get; set; }
        public int Birim_ID { get; set; }
        public int Stok { get; set; }
        public int KritikStokSeviyesi { get; set; }
    }
}
