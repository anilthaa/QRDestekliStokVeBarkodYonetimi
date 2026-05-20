namespace QRDestekliStokVeBarkodYonetimi.Models
{
    public class ItemKullanicilar:ItemBase
    {
        public int KullaniciTip_ID { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string Sifre { get; set; }
        public string Eposta { get; set; }
        /// <summary>wwwroot köküne göre relative URL (örn. /uploads/profiles/12.jpg).</summary>
        public string? ProfilResmi { get; set; }
        public bool? Aktif { get; set; }
    }
}
