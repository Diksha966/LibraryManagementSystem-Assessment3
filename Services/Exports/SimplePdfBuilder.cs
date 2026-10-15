using System.Text;

namespace LibraryManagementSystem.Services.Exports;

public static class SimplePdfBuilder
{
    public static byte[] BuildSinglePageTextPdf(IEnumerable<string> lines)
    {
        string escapedText = string.Join("\\n", lines.Select(EscapePdfText));
        string contentStream = "BT\n/F1 11 Tf\n50 790 Td\n14 TL\n";

        foreach (var line in lines)
        {
            contentStream += $"({EscapePdfText(line)}) Tj T*\n";
        }

        contentStream += "ET";

        var objects = new List<string>
        {
            "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n",
            "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n",
            "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n",
            "4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n",
            $"5 0 obj\n<< /Length {Encoding.ASCII.GetByteCount(contentStream)} >>\nstream\n{contentStream}\nendstream\nendobj\n"
        };

        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");

        var offsets = new List<int> { 0 };

        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
            sb.Append(obj);
        }

        int xrefOffset = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append($"xref\n0 {objects.Count + 1}\n");
        sb.Append("0000000000 65535 f \n");

        for (int i = 1; i < offsets.Count; i++)
        {
            sb.Append($"{offsets[i]:D10} 00000 n \n");
        }

        sb.Append("trailer\n");
        sb.Append($"<< /Size {objects.Count + 1} /Root 1 0 R >>\n");
        sb.Append("startxref\n");
        sb.Append($"{xrefOffset}\n");
        sb.Append("%%EOF");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static string EscapePdfText(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");
    }
}
