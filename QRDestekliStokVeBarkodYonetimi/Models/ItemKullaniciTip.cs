namespace QRDestekliStokVeBarkodYonetimi.Models
{
    public class ItemKullaniciTip:ItemBase
    {
        public string Ad { get; set; }
        public bool? Aktif { get; set; }

        /// <summary>Yeni kayıt (/register) kullanıcılarına atanacak tek varsayılan tip.</summary>
        public bool Varsayilan { get; set; }
    }
}
