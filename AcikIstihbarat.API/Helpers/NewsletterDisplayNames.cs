namespace AcikIstihbarat.API.Helpers
{
    // Keep in lockstep with acik-istihbarat-public/lib/newsletterLabels.ts.
    // Known drift risk - tracked in ProjectImplementationDocs/TODO-AcikIstihbarat.md
    // ("Unify newsletter display name source of truth").
    public static class NewsletterDisplayNames
    {
        private static readonly Dictionary<string, string> Labels = new()
        {
            ["AcikGazete"] = "Gazete Özetleri",
            ["AcikKose"] = "Köşe Yazarları",
        };

        public static IReadOnlyCollection<string> Keys => Labels.Keys;

        public static string Resolve(string templateBaseName) =>
            Labels.TryGetValue(templateBaseName, out var label) ? label : templateBaseName;
    }
}
