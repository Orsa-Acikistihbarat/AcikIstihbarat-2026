namespace AcikIstihbarat.API.Services
{
    public interface IMailTemplateResolver
    {
        (string FileName, string Html)? ResolveLatest(string templateBaseName);
    }
}
