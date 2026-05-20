using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services
{
    /// <summary>
    /// YetkiService'in AuthStateService'ten kullandığı oturum sözleşmesi.
    /// Testlerde NSubstitute ile kolayca taklit edilebilir.
    /// </summary>
    public interface IAuthState
    {
        ItemKullanicilar? CurrentUser  { get; }
        bool              IsAuthenticated { get; }
        event Action?     OnChange;

        /// <summary><see cref="CurrentUser"/> alanları in-place güncellendiğinde (profil vb.) abonelere bildirir.</summary>
        void NotifyStateChanged();
    }
}
