using System.Globalization;
using System.Text;

namespace LibraryManagementSystem.Services;

/// <summary>
/// Turns a ReportTable into CSV or PDF bytes. The PDF writer is built in
/// (plain Courier text, A4 landscape) so no extra NuGet package is needed.
/// </summary>
public static class ReportExporter
{
    // ---------------- CSV ----------------

    public static byte[] ToCsv(ReportTable report)
    {
        var sb = new StringBuilder();

        sb.Append(Csv("Report")).Append(',').Append(Csv(report.Title)).Append("\r\n");
        sb.Append(Csv("Generated")).Append(',')
          .Append(Csv(report.GeneratedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)))
          .Append("\r\n\r\n");

        sb.Append("Summary\r\n");

        foreach (var (label, value) in report.Summary)
            sb.Append(Csv(label)).Append(',').Append(Csv(value)).Append("\r\n");

        sb.Append("\r\n");
        sb.Append(string.Join(",", report.Columns.Select(Csv))).Append("\r\n");

        foreach (var row in report.Rows)
            sb.Append(string.Join(",", row.Select(Csv))).Append("\r\n");

        // UTF-8 with BOM so Excel opens it correctly.
        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
    }

    // Quotes fields and neutralises spreadsheet formulas (=, +, -, @).
    private static string Csv(string? value)
    {
        value ??= string.Empty;

        if (value.Length > 0 && "=+-@\t\r".IndexOf(value[0]) >= 0)
            value = "'" + value;

        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            value = "\"" + value.Replace("\"", "\"\"") + "\"";

        return value;
    }

    // ---------------- PDF ----------------

    private record PdfLine(string Text, bool Bold, double Size, bool TableRow);

    public static byte[] ToPdf(ReportTable report)
    {
        const double pageW = 842, pageH = 595, margin = 40;
        const double bodySize = 8;
        const int maxChars = 150;   // Courier 8pt is 4.8pt wide; 150 chars = 720pt

        // ---- Build the lines of text ----
        var lines = new List<PdfLine>();

        lines.Add(new PdfLine(report.Title, true, 16, false));
        lines.Add(new PdfLine(
            "Generated " + report.GeneratedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            false, bodySize, false));
        lines.Add(new PdfLine("", false, bodySize, false));
        lines.Add(new PdfLine("Summary", true, 11, false));

        int labelWidth = report.Summary.Count == 0
            ? 0
            : report.Summary.Max(s => s.Label.Length);

        foreach (var (label, value) in report.Summary)
            lines.Add(new PdfLine(label.PadRight(labelWidth + 3) + value, false, bodySize, false));

        lines.Add(new PdfLine("", false, bodySize, false));
        lines.Add(new PdfLine("Details (" + report.Rows.Count + (report.Rows.Count == 1 ? " row)" : " rows)"), true, 11, false));

        // Column widths: widest cell, capped so the table fits on the page.
        int colCount = report.Columns.Length;
        var widths = new int[colCount];

        for (int cap = 40; cap >= 6; cap--)
        {
            for (int c = 0; c < colCount; c++)
            {
                int w = report.Columns[c].Length;

                foreach (var row in report.Rows)
                    w = Math.Max(w, (row[c] ?? "").Length);

                widths[c] = Math.Min(w, cap);
            }

            if (widths.Sum() + 2 * (colCount - 1) <= maxChars)
                break;
        }

        string FormatRow(IReadOnlyList<string> cells)
        {
            var parts = new List<string>();

            for (int c = 0; c < colCount; c++)
            {
                string text = (cells[c] ?? "").Replace('\n', ' ').Replace('\r', ' ');

                if (text.Length > widths[c])
                    text = widths[c] > 3
                        ? text.Substring(0, widths[c] - 3) + "..."
                        : text.Substring(0, widths[c]);

                parts.Add(text.PadRight(widths[c]));
            }

            return string.Join("  ", parts).TrimEnd();
        }

        var header = new PdfLine(FormatRow(report.Columns), true, bodySize, true);
        var rule = new PdfLine(new string('-', widths.Sum() + 2 * (colCount - 1)), false, bodySize, true);

        lines.Add(header);
        lines.Add(rule);

        foreach (var row in report.Rows)
            lines.Add(new PdfLine(FormatRow(row), false, bodySize, true));

        if (report.Rows.Count == 0)
            lines.Add(new PdfLine("(no records)", false, bodySize, false));

        // ---- Paginate (repeat the table header on continuation pages) ----
        double usable = pageH - 2 * margin - 14;
        var pages = new List<List<PdfLine>>();
        var current = new List<PdfLine>();
        double used = 0;

        foreach (var line in lines)
        {
            double h = line.Size * 1.3;

            if (used + h > usable && current.Count > 0)
            {
                pages.Add(current);
                current = new List<PdfLine>();
                used = 0;

                if (line.TableRow && !ReferenceEquals(line, header) && !ReferenceEquals(line, rule))
                {
                    current.Add(header);
                    current.Add(rule);
                    used += (header.Size + rule.Size) * 1.3;
                }
            }

            current.Add(line);
            used += h;
        }

        if (current.Count > 0)
            pages.Add(current);

        // ---- Assemble the PDF (ASCII only, so string length = byte length) ----
        var pdf = new StringBuilder();
        var offsets = new List<int>();

        void AddObject(string body)
        {
            offsets.Add(pdf.Length);
            pdf.Append(offsets.Count).Append(" 0 obj\n")
               .Append(body).Append("\nendobj\n");
        }

        pdf.Append("%PDF-1.4\n");

        var kids = new StringBuilder();

        for (int i = 0; i < pages.Count; i++)
            kids.Append(5 + 2 * i).Append(" 0 R ");

        AddObject("<< /Type /Catalog /Pages 2 0 R >>");
        AddObject("<< /Type /Pages /Kids [" + kids + "] /Count " + pages.Count + " >>");
        AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");
        AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Courier-Bold >>");

        for (int p = 0; p < pages.Count; p++)
        {
            var content = new StringBuilder();
            double y = pageH - margin;

            foreach (var line in pages[p])
            {
                y -= line.Size * 1.3;

                if (line.Text.Length > 0)
                {
                    content.Append("BT /F").Append(line.Bold ? 2 : 1).Append(' ')
                           .Append(Num(line.Size)).Append(" Tf ")
                           .Append(Num(margin)).Append(' ').Append(Num(y))
                           .Append(" Td (").Append(Escape(line.Text)).Append(") Tj ET\n");
                }
            }

            content.Append("BT /F1 8 Tf ").Append(Num(margin)).Append(" 25 Td (")
                   .Append("Page ").Append(p + 1).Append(" of ").Append(pages.Count)
                   .Append(") Tj ET\n");

            string stream = content.ToString();

            AddObject(
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + Num(pageW) + " " + Num(pageH) + "] " +
                "/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> " +
                "/Contents " + (6 + 2 * p) + " 0 R >>");

            AddObject("<< /Length " + stream.Length + " >>\nstream\n" + stream + "endstream");
        }

        int xrefOffset = pdf.Length;

        pdf.Append("xref\n0 ").Append(offsets.Count + 1).Append('\n');
        pdf.Append("0000000000 65535 f \n");

        foreach (int offset in offsets)
            pdf.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");

        pdf.Append("trailer\n<< /Size ").Append(offsets.Count + 1)
           .Append(" /Root 1 0 R >>\nstartxref\n")
           .Append(xrefOffset).Append("\n%%EOF\n");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string Num(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    // Escapes PDF string characters and replaces anything non-ASCII with '?'.
    private static string Escape(string text)
    {
        var sb = new StringBuilder();

        foreach (char c in text)
        {
            if (c == '\\' || c == '(' || c == ')')
                sb.Append('\\').Append(c);
            else if (c >= 32 && c <= 126)
                sb.Append(c);
            else
                sb.Append('?');
        }

        return sb.ToString();
    }
}
