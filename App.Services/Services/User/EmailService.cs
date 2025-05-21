using App.Services.Interfaces.User;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using App.Core.Jsetting;

namespace App.Services.Services.User
{
    public class EmailService : IEmailService
    {
        #region DI Constructor
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }
        #endregion
        
        private const string HtmlEmailTemplate = @"
        <!DOCTYPE html>
        <html lang=""vi"">
        <head>
            <meta charset=""UTF-8"">
            <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
            <title>{{Subject}}</title>
            <style>
                body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; font-size: 16px; }
                .container { max-width: 600px; margin: 20px auto; padding: 20px; background-color: #ffffff; border: 1px solid #ddd; border-radius: 5px; }
                h1 { 
                    color: #0056b3; 
                    border-bottom: 2px solid #eee; 
                    padding-bottom: 10px; 
                    font-size: 24px; 
                    text-align: center;
                }
                .greeting {
                    color: #000000;
                    margin-bottom: 15px;
                }
                .otp-code { font-weight: bold; font-size: 1.4em; color: #d9534f; padding: 8px 15px; background-color: #f9f2f4; border-radius: 4px; display: inline-block; margin-top: 5px; margin-bottom: 10px; border: 1px dashed #d9534f; }
                .footer { margin-top: 20px; font-size: 0.9em; color: #777; text-align: center; border-top: 1px solid #eee; padding-top: 10px; }
                a { color: #0056b3; text-decoration: none; }
                .button { display: inline-block; padding: 10px 20px; background-color: #0056b3; color: #ffffff; text-decoration: none; border-radius: 5px; font-weight: bold; }
                ul, ol { margin: 10px 0; padding-left: 20px; }
                li { margin-bottom: 5px; }
            </style>
        </head>
        <body>
            <div class=""container"">
                <h1>{{Subject}}</h1>
                <p class=""greeting"">{{Greeting}}</p>
                <div>{{MainMessage}}</div>
                <div class=""footer"">
                    <p>Nếu bạn không yêu cầu email này, vui lòng bỏ qua.</p>
                    <p>© {{Year}} Ngoại Ngữ Ngay. All rights reserved.</p>
                    <p>Địa chỉ: 123 Đường ABC, Quận XYZ, TP.HCM</p>
                    <p><a href=""mailto:contact@yourdomain.com"">Liên hệ</a> | <a href=""https://yourdomain.com/unsubscribe?email={{EmailAddress}}"">Hủy đăng ký</a></p>
                </div>
            </div>
        </body>
        </html>";

        public async Task SendEmailAsync(string email, string subject, string greeting, string mainMessage)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Ngoại Ngữ Ngay", _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress("Member", email));
            message.Subject = subject;
            
            // KHÔNG thêm Message-ID thủ công - MailKit đã tự tạo
            // ĐÚNG: Thêm các header khác
            message.Headers.Add("List-Unsubscribe", $"<mailto:unsubscribe@{_emailSettings.SenderEmail.Split('@')[1]}>");
            message.Headers.Add("X-Priority", "1");
            message.Headers.Add("Importance", "High");
            
            var bodyBuilder = new BodyBuilder();
            string htmlBody = HtmlEmailTemplate
                .Replace("{{Subject}}", subject)
                .Replace("{{Greeting}}", greeting)
                .Replace("{{MainMessage}}", mainMessage)
                .Replace("{{Year}}", DateTime.UtcNow.Year.ToString())
                .Replace("{{EmailAddress}}", email); // Thêm biến email cho link unsubscribe
        
            bodyBuilder.HtmlBody = htmlBody;
            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
                await smtp.SendAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email to {email}: {ex.Message}");
                throw;
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }
    }


}
