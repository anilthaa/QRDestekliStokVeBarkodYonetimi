namespace QRDestekliStokVeBarkodYonetimi.Models
{
    public class ItemForm:ItemBase
    {
        public string Ad { get; set; }
        public bool IsMenu { get; set; }
        public int? UstMenu_ID { get; set; }
        public int Sira { get; set; }
        public string? SayfaURL { get; set; }
        public string? Icon { get; set; }
        
    }
}
