namespace QRDestekliStokVeBarkodYonetimi.Models
{
    public class ItemKategori:ItemBase
    {
        public string? ResimYolu { get; set; }
        public string Ad { get; set; }
        public string? Aciklama { get; set; }
        public bool Aktif { get; set; }
    }
}
