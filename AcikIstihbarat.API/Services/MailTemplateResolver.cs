using System.Text.RegularExpressions;
using AcikIstihbarat.API.Models.DTOs;
using Microsoft.Extensions.Options;

namespace AcikIstihbarat.API.Services
{
    public class MailTemplateResolver : IMailTemplateResolver
    {
        // Matches the exact convention used by acik-istihbarat-public/lib/newsletters.ts and
        // acikmedya-webhook/server.js: a fixed "index" prefix + zero-padded DD/MM/YY, regardless
        // of the containing folder's (templateBaseName's) name.
        private static readonly Regex FileNameRegex = new(@"^index(\d{2})(\d{2})(\d{2})\.html$", RegexOptions.Compiled);

        private readonly MailOptions _options;

        public MailTemplateResolver(IOptions<MailOptions> options)
        {
            _options = options.Value;
        }

        public (string FileName, string Html)? ResolveLatest(string templateBaseName)
        {
            var folderDir = Path.Combine(_options.TemplatesDataDir, templateBaseName);

            string[] entries;
            try
            {
                entries = Directory.GetFiles(folderDir);
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }

            var today = IstanbulClock.NowLocal();
            var todayValue = 20_00_00_00 + today.Year % 100 * 10000 + today.Month * 100 + today.Day;

            string? bestFileName = null;
            var bestValue = -1;

            foreach (var entry in entries)
            {
                var fileName = Path.GetFileName(entry);
                var match = FileNameRegex.Match(fileName);
                if (!match.Success)
                {
                    continue;
                }

                var dd = int.Parse(match.Groups[1].Value);
                var mm = int.Parse(match.Groups[2].Value);
                var yy = int.Parse(match.Groups[3].Value);
                var value = 20_00_00_00 + yy * 10000 + mm * 100 + dd;

                if (value > todayValue)
                {
                    continue; // future-dated file, ignore
                }

                if (value == todayValue)
                {
                    bestFileName = fileName;
                    bestValue = value;
                    break; // exact match for today wins outright
                }

                if (value > bestValue)
                {
                    bestValue = value;
                    bestFileName = fileName;
                }
            }

            if (bestFileName is null)
            {
                return null;
            }

            var html = File.ReadAllText(Path.Combine(folderDir, bestFileName));
            return (bestFileName, html);
        }
    }
}
