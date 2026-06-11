using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace QRDestekliStokVeBarkodYonetimi.Services;

// ─────────────────────────────────────────────────────────────────────────────
// Liste sayfalarındaki "Çıktı Al" butonu için ortak dışa aktarma servisi.
// Kolon meta verisi (ExportColumn<T>) aracılığıyla Excel / PDF / CSV üretir.
// Sayfalar yalnızca verisini ve görünür kolon tanımını verir; biçimlendirme
// burada merkezîleşir, böylece her sayfada aynı düzgün çıktı elde edilir.
// ─────────────────────────────────────────────────────────────────────────────
public class ExportColumn<T>
{
    public string Header { get; set; } = "";
    public Func<T, object?> Value { get; set; } = _ => null;

    /// <summary>Sayısal/tarih biçimi (örn. "N2", "dd.MM.yyyy HH:mm").</summary>
    public string? Format { get; set; }

    /// <summary>0 = otomatik, &gt;0 = sabit genişlik (PDF için relative birim).</summary>
    public float Width { get; set; }

    public ExportAlign Align { get; set; } = ExportAlign.Left;
}

public enum ExportAlign { Left, Center, Right }

public class ExportService
{
    public byte[] ToExcel<T>(string sheetName, IEnumerable<T> items, IList<ExportColumn<T>> columns)
    {
        using var wb = new XLWorkbook();
        // Excel sheet adı 31 karakterden uzun olamaz ve bazı karakterleri kabul etmez
        var safeSheet = SafeSheetName(sheetName);
        var ws = wb.Worksheets.Add(safeSheet);

        // Başlık satırı
        for (int c = 0; c < columns.Count; c++)
        {
            var cell = ws.Cell(1, c + 1);
            cell.Value = columns[c].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1f6feb");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }

        int row = 2;
        foreach (var item in items)
        {
            for (int c = 0; c < columns.Count; c++)
            {
                var col = columns[c];
                var raw = col.Value(item);
                var cell = ws.Cell(row, c + 1);

                SetExcelCellValue(cell, raw, col.Format);

                cell.Style.Alignment.Horizontal = col.Align switch
                {
                    ExportAlign.Right => XLAlignmentHorizontalValues.Right,
                    ExportAlign.Center => XLAlignmentHorizontalValues.Center,
                    _ => XLAlignmentHorizontalValues.Left
                };

                if (row % 2 == 1) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#f5f7fa");
            }
            row++;
        }

        var lastRow = Math.Max(1, row - 1);
        var lastCol = columns.Count;
        if (lastCol > 0)
        {
            var tableRange = ws.Range(1, 1, lastRow, lastCol);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#dddddd");
            tableRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#dddddd");
        }

        ws.SheetView.FreezeRows(1);
        ws.RangeUsed()?.SetAutoFilter();
        ws.Columns().AdjustToContents(1, 60d);
        ws.Rows().Height = 18;
        ws.Row(1).Height = 24;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void SetExcelCellValue(IXLCell cell, object? raw, string? format)
    {
        if (raw is null) { cell.Value = string.Empty; return; }

        switch (raw)
        {
            case DateTime dt:
                cell.Value = dt;
                cell.Style.DateFormat.Format = format ?? "dd.MM.yyyy HH:mm";
                break;
            case bool b:
                cell.Value = b ? "Evet" : "Hayır";
                break;
            case decimal or double or float or int or long or short:
                cell.Value = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
                var excelFormat = ToExcelNumberFormat(format);
                if (!string.IsNullOrEmpty(excelFormat))
                    cell.Style.NumberFormat.Format = excelFormat;
                break;
            default:
                cell.Value = raw.ToString();
                break;
        }
    }

    public byte[] ToPdf<T>(string title, IEnumerable<T> items, IList<ExportColumn<T>> columns)
    {
        var data = items.ToList();
        var generated = DateTime.Now;

        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);
                page.DefaultTextStyle(t => t.FontSize(9).FontFamily(Fonts.Calibri));

                page.Header().Column(col =>
                {
                    col.Item().Text(title)
                        .FontSize(16).Bold().FontColor("#1f3a93");
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Toplam Kayıt: {data.Count}")
                            .FontSize(9).FontColor("#555");
                        r.ConstantItem(180).AlignRight()
                            .Text($"Oluşturulma: {generated:dd.MM.yyyy HH:mm}")
                            .FontSize(9).FontColor("#555");
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#1f3a93");
                });

                page.Content().PaddingVertical(8).Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        foreach (var c in columns)
                        {
                            if (c.Width > 0) cd.RelativeColumn(c.Width);
                            else cd.RelativeColumn();
                        }
                    });

                    // Başlık
                    table.Header(h =>
                    {
                        foreach (var c in columns)
                        {
                            h.Cell().Background("#1f6feb").Padding(5)
                                .AlignCenter()
                                .Text(c.Header).FontColor(Colors.White).Bold().FontSize(9);
                        }
                    });

                    // Satırlar
                    int rowIdx = 0;
                    foreach (var item in data)
                    {
                        var bg = rowIdx % 2 == 0 ? "#ffffff" : "#f5f7fa";
                        foreach (var c in columns)
                        {
                            var raw = c.Value(item);
                            var text = FormatForPdf(raw, c.Format);
                            var cell = table.Cell()
                                .Background(bg)
                                .BorderBottom(0.5f).BorderColor("#dddddd")
                                .Padding(4);

                            var aligned = c.Align switch
                            {
                                ExportAlign.Right => cell.AlignRight(),
                                ExportAlign.Center => cell.AlignCenter(),
                                _ => cell.AlignLeft()
                            };
                            aligned.Text(text).FontSize(9);
                        }
                        rowIdx++;
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Sayfa ").FontSize(8).FontColor("#777");
                    t.CurrentPageNumber().FontSize(8).FontColor("#777");
                    t.Span(" / ").FontSize(8).FontColor("#777");
                    t.TotalPages().FontSize(8).FontColor("#777");
                });
            });
        });

        return pdf.GeneratePdf();
    }

    public byte[] ToCsv<T>(IEnumerable<T> items, IList<ExportColumn<T>> columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', columns.Select(c => CsvEscape(c.Header))));
        foreach (var item in items)
        {
            sb.AppendLine(string.Join(';', columns.Select(c => CsvEscape(FormatForPdf(c.Value(item), c.Format)))));
        }
        // Excel'in Türkçe locale'de doğru açması için UTF-8 BOM ekle
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
        return result;
    }

    private static string CsvEscape(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var needsQuote = s.Contains(';') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
        var escaped = s.Replace("\"", "\"\"");
        return needsQuote ? $"\"{escaped}\"" : escaped;
    }

    private static string FormatForPdf(object? raw, string? format)
    {
        if (raw is null) return "-";
        return raw switch
        {
            DateTime dt => dt.ToString(string.IsNullOrEmpty(format) ? "dd.MM.yyyy HH:mm" : format, CultureInfo.GetCultureInfo("tr-TR")),
            bool b => b ? "Evet" : "Hayır",
            IFormattable f when !string.IsNullOrEmpty(format)
                => f.ToString(format, CultureInfo.GetCultureInfo("tr-TR")),
            _ => raw.ToString() ?? string.Empty
        };
    }

    private static string? ToExcelNumberFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format)) return null;
        return format.ToUpperInvariant() switch
        {
            "N0" => "#,##0",
            "N1" => "#,##0.0",
            "N2" => "#,##0.00",
            "F0" => "0",
            "F2" => "0.00",
            _ when format.Contains('#') || format.Contains('0') => format,
            _ => null
        };
    }

    private static string SafeSheetName(string name)
    {
        var invalid = new[] { '\\', '/', '*', '[', ']', ':', '?' };
        var clean = new string((name ?? "Liste").Select(ch => invalid.Contains(ch) ? ' ' : ch).ToArray());
        return clean.Length > 31 ? clean[..31] : clean;
    }
}
