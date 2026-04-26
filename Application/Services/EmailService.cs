using System.Net;
using System.Net.Mail;

namespace OperaLearningSystem.Application.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }

    public class EmailService : IEmailService
    {
        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var smtpClient = new SmtpClient("smtp.qq.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("2694161921@qq.com", "exoydfzavsskdfff"), // 注意：这里不是QQ密码，是设置里开通的SMTP授权码
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("2694161921@qq.com", "畅音雅韵官方"),
                Subject = subject,
                Body = message,
                IsBodyHtml = true, // 允许发送带HTML样式的邮件
            };
            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}