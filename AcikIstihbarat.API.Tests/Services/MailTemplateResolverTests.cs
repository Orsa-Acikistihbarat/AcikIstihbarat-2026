using AcikIstihbarat.API.Models.DTOs;
using AcikIstihbarat.API.Services;
using Microsoft.Extensions.Options;

namespace AcikIstihbarat.API.Tests.Services
{
    public class MailTemplateResolverTests : IDisposable
    {
        private readonly string _tempRoot;

        public MailTemplateResolverTests()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "MailTemplateResolverTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempRoot);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }

        private MailTemplateResolver CreateResolver()
        {
            var options = Options.Create(new MailOptions { TemplatesDataDir = _tempRoot });
            return new MailTemplateResolver(options);
        }

        private static string TodaySuffix()
        {
            var today = IstanbulClock.NowLocal();
            return $"{today:dd}{today:MM}{today:yy}";
        }

        [Fact]
        public void ResolveLatest_ExactTodayFilePresent_ReturnsIt()
        {
            const string folder = "AcikGazete";
            var folderDir = Path.Combine(_tempRoot, folder);
            Directory.CreateDirectory(folderDir);
            var todayFile = $"index{TodaySuffix()}.html";
            File.WriteAllText(Path.Combine(folderDir, todayFile), "<html>today</html>");

            var resolver = CreateResolver();
            var result = resolver.ResolveLatest(folder);

            Assert.NotNull(result);
            Assert.Equal(todayFile, result!.Value.FileName);
            Assert.Equal("<html>today</html>", result.Value.Html);
        }

        [Fact]
        public void ResolveLatest_OnlyPastDatedFilePresent_FallsBackToIt()
        {
            const string folder = "AcikGazete";
            var folderDir = Path.Combine(_tempRoot, folder);
            Directory.CreateDirectory(folderDir);
            File.WriteAllText(Path.Combine(folderDir, "index010120.html"), "<html>past</html>");

            var resolver = CreateResolver();
            var result = resolver.ResolveLatest(folder);

            Assert.NotNull(result);
            Assert.Equal("index010120.html", result!.Value.FileName);
        }

        [Fact]
        public void ResolveLatest_OnlyFutureDatedFilePresent_ReturnsNull()
        {
            const string folder = "AcikGazete";
            var folderDir = Path.Combine(_tempRoot, folder);
            Directory.CreateDirectory(folderDir);
            File.WriteAllText(Path.Combine(folderDir, "index010199.html"), "<html>future</html>");

            var resolver = CreateResolver();
            var result = resolver.ResolveLatest(folder);

            Assert.Null(result);
        }

        [Fact]
        public void ResolveLatest_EmptyFolder_ReturnsNull()
        {
            const string folder = "AcikGazete";
            Directory.CreateDirectory(Path.Combine(_tempRoot, folder));

            var resolver = CreateResolver();
            var result = resolver.ResolveLatest(folder);

            Assert.Null(result);
        }

        [Fact]
        public void ResolveLatest_MissingFolder_ReturnsNull()
        {
            var resolver = CreateResolver();
            var result = resolver.ResolveLatest("DoesNotExist");

            Assert.Null(result);
        }

        [Fact]
        public void ResolveLatest_MalformedFileName_IsIgnoredAndDoesNotCrash()
        {
            const string folder = "AcikGazete";
            var folderDir = Path.Combine(_tempRoot, folder);
            Directory.CreateDirectory(folderDir);
            File.WriteAllText(Path.Combine(folderDir, "notes.txt"), "irrelevant");
            File.WriteAllText(Path.Combine(folderDir, "index1.html"), "irrelevant");
            File.WriteAllText(Path.Combine(folderDir, $"index{TodaySuffix()}.html"), "<html>today</html>");

            var resolver = CreateResolver();
            var result = resolver.ResolveLatest(folder);

            Assert.NotNull(result);
            Assert.Equal($"index{TodaySuffix()}.html", result!.Value.FileName);
        }

        [Fact]
        public void ResolveLatest_MultiplePastDatedFiles_ReturnsMostRecent()
        {
            const string folder = "AcikGazete";
            var folderDir = Path.Combine(_tempRoot, folder);
            Directory.CreateDirectory(folderDir);
            File.WriteAllText(Path.Combine(folderDir, "index010120.html"), "<html>oldest</html>");
            File.WriteAllText(Path.Combine(folderDir, "indexNewer010121.html"), "irrelevant, wrong name pattern");
            File.WriteAllText(Path.Combine(folderDir, "index150121.html"), "<html>newest</html>");

            var resolver = CreateResolver();
            var result = resolver.ResolveLatest(folder);

            Assert.NotNull(result);
            Assert.Equal("index150121.html", result!.Value.FileName);
            Assert.Equal("<html>newest</html>", result.Value.Html);
        }
    }
}
