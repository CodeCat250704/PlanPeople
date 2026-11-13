using System.Text;

namespace PlanPeople.Core.Ics;

public static class IcsHelper
{
    // RFC 5545 规定每行不能超过 75 字节，超长需折行
    public static string FoldLine(string line)
    {
        if (line.Length <= 75) return line;

        var sb = new StringBuilder();
        var bytes = Encoding.UTF8.GetBytes(line);
        int currentLineLength = 0;

        for (int i = 0; i < bytes.Length; i++)
        {
            if (currentLineLength >= 73)
            {
                sb.Append("\r\n ");
                currentLineLength = 1;
            }
            sb.Append((char)bytes[i]);
            currentLineLength++;
        }
        return sb.ToString();
    }

    // 反转义
    public static string Unescape(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Replace("\\n", "\n")
                   .Replace("\\N", "\n")
                   .Replace("\\,", ",")
                   .Replace("\\;", ";")
                   .Replace("\\\\", "\\");
    }

    // 转义
    public static string Escape(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Replace("\\", "\\\\")
                   .Replace(";", "\\;")
                   .Replace(",", "\\,")
                   .Replace("\n", "\\n");
    }
}