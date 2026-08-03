using System.Web;
using HtmlAgilityPack;

namespace AcikIstihbarat.API.Helpers
{
    public static class HtmlCleanupHelper
    {
        public static string StripHtml(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var doc = new HtmlDocument();
            doc.LoadHtml(input);

            // Extract text and decode HTML entities (e.g. &nbsp;)
            var text = HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);

            // Fallback to remove potential rogue comments if InnerText misses them
            if (text.Contains("<!--") && text.Contains("-->"))
            {
                int startIndex = text.IndexOf("<!--");
                while (startIndex != -1)
                {
                    int endIndex = text.IndexOf("-->", startIndex);
                    if (endIndex != -1)
                    {
                        text = text.Remove(startIndex, endIndex - startIndex + 3);
                    }
                    else
                    {
                        break;
                    }
                    startIndex = text.IndexOf("<!--");
                }
            }

            return text.Trim();
        }
    }
}
