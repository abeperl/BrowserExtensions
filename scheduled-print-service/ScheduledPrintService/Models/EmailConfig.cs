namespace ScheduledPrintService.Models;

public class EmailConfig
{
    public bool Enabled { get; set; } = false;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty; // Comma separated list accepted
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int MaxFailuresBeforeEmail { get; set; } = 1;
}
