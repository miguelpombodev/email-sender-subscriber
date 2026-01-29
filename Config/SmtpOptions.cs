using System.ComponentModel.DataAnnotations;

namespace SubEmailSender.Config;

public class SmtpOptions
{
    [Required]
    public string Host { get; set; } = default!;
    
    [Range(1, 65535)]
    public int Port { get; set; } = 25;
    
    [Required]
    public string User { get; set; } = default!;
    
    [Required]
    public string Password { get; set; } = default!;
    
    [Required, EmailAddress]
    public string FromEmail { get; set; } = default!;
    
    [Required]
    public string FromName { get; set; } = default!;
}