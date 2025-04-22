namespace App.Core.Base
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = String.Empty;
        public int SmtpPort { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = String.Empty;
        public string SenderPassword { get; set; } = String.Empty;
        public bool UseSSL { get; set; }
    }
}
