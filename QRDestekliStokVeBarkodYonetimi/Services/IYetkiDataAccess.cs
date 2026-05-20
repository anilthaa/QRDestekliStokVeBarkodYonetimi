using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// YetkiService'in ihtiyaç duyduğu veri erişim sözleşmesi.
    /// DataService bu arayüzü uygular; testlerde kolayca taklit edilebilir.
    /// </summary>
    public interface IYetkiDataAccess
    {
        Task<ItemKullaniciDetay[]>    GetKullaniciFormYetki(int kullaniciId);
        Task<ItemKullanicilar[]>      GetKullanici(int ID = 0);
        Task<ItemKullaniciTipDetay[]> GetKullaniciTipFormYetki(int kullaniciTipId);
        Task<ItemForm[]>              GetTumFormlar();
    }
}
