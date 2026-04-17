namespace QRDestekliStokVeBarkodYonetimi.Models
{
    public class ItemStokHareketleri: ItemBase
    {
        public int Urun_ID { get; set; }
        public short HareketTipi { get; set; }
        public decimal Miktar { get; set; }
        public string? Not { get; set; }
    }
}
