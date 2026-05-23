using System.Net;
using Microsoft.AspNetCore.Components;

namespace QRDestekliStokVeBarkodYonetimi.Components.Shared;

public static class DialogFragments
{
    public static string B(string text) =>
        $"<b>{WebUtility.HtmlEncode(text)}</b>";

    public static RenderFragment Html(string markup) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddMarkupContent(1, markup);
        builder.CloseElement();
    };
}
