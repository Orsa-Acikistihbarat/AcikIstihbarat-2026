namespace AcikIstihbarat.API.Models.DTOs
{
    public class SubscribeRequest
    {
        public string Email { get; set; } = string.Empty;
        public List<string> TemplateBaseNames { get; set; } = new();
    }

    public class SubscribeResultItem
    {
        public string TemplateBaseName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class SubscribeResponse
    {
        public List<SubscribeResultItem> Results { get; set; } = new();
    }
}
