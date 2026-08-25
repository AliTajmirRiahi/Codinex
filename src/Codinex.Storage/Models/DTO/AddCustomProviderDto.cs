namespace Codinex.Storage.Models.DTO;

public class AddCustomProviderDto
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string IconColor { get; set; } = "";
    public string Protocol { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string ModelEndPoint { get; set; } = "";
    public bool NeedApiKey { get; set; } = true;
}
