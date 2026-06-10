using System.ComponentModel.DataAnnotations;

namespace QRDestekliStokVeBarkodYonetimi.Models
{
    public class ItemUrun:ItemBase
    {
        [StringLength(50)]
        public string UrunKodu { get; set; }
        public int Kategori_ID { get; set; }
        [StringLength(20)]
        public string? BarkodNo { get; set; }
        [StringLength(500)]
        public string? ResimYolu { get; set; }
        [StringLength(150)]
        public string Ad { get; set; }
        [StringLength(250)]
        public string? Aciklama { get; set; }
        public int Birim_ID { get; set; }
        public int Stok { get; set; }
        public int KritikStokSeviyesi { get; set; }
    }
}
