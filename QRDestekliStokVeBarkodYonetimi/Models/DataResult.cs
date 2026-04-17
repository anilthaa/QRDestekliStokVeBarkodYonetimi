namespace QRDestekliStokVeBarkodYonetimi.Models
{
    public class DataResult
    {
        public int SonucKodu { get; set; } = 0;
        public string SonucAciklama { get; set; } = string.Empty;
    }

    public class DataResult<T> : DataResult
    {
        public T? Data { get; set; }
    }
}
