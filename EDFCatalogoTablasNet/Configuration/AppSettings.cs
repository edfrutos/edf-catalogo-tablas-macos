namespace EDFCatalogoTablasNet.Configuration;

public class AppSettings
{
    public string AppName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public long MaxFileSize { get; set; }
    public string AllowedFileTypes { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public bool EnableDetailedErrors { get; set; }
    public bool CacheEnabled { get; set; }
}