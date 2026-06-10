using Microsoft.JSInterop;
using QRDestekliStokVeBarkodYonetimi.Models;

namespace QRDestekliStokVeBarkodYonetimi.Services;

public static class BarkodBasariSesiYardimci
{
    public static ValueTask CalAsync(IJSRuntime js, BarkodTaramaKaynagi kaynak) =>
        kaynak == BarkodTaramaKaynagi.Kamera
            ? js.InvokeVoidAsync("BarkodBasariSesi.cal")
            : ValueTask.CompletedTask;
}
