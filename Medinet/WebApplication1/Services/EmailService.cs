using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace WebApplication1.Services
{
    public class EmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService()
        {
            // Đọc cấu hình từ Web.config
            _smtpHost = ConfigurationManager.AppSettings["SmtpHost"];
            _smtpPort = Convert.ToInt32(ConfigurationManager.AppSettings["SmtpPort"]);
            _smtpUsername = ConfigurationManager.AppSettings["SmtpEmail"];
            _smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
            _fromEmail = ConfigurationManager.AppSettings["SmtpEmail"]; // Sử dụng email SMTP làm from email
            _fromName = ConfigurationManager.AppSettings["EmailFromName"];
        }

        public async Task SendVerificationEmailAsync(string toEmail, string toName, string verificationCode)
        {
            var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = "Xác nhận đăng ký tài khoản MediNet",
                IsBodyHtml = true,
                Body = $@"
                    <html>
                    <head>
                        <style>
                            body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                            .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                            .header {{ background-color: #007bff; color: white; padding: 10px; text-align: center; }}
                            .content {{ padding: 20px; }}
                            .code {{ font-size: 24px; font-weight: bold; text-align: center; padding: 15px; background-color: #f5f5f5; margin: 20px 0; }}
                            .footer {{ text-align: center; font-size: 12px; color: #666; margin-top: 20px; }}
                        </style>
                    </head>
                    <body>
                        <div class='container'>
                            <div class='header'>
                                <h2>MediNet</h2>
                            </div>
                            <div class='content'>
                                <p>Xin chào {toName},</p>
                                <p>Cảm ơn bạn đã đăng ký tài khoản tại MediNet. Để hoàn tất quá trình đăng ký, vui lòng nhập mã xác nhận bên dưới:</p>
                                <div class='code'>{verificationCode}</div>
                                <p>Mã xác nhận này có hiệu lực trong vòng 3 phút.</p>
                                <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.</p>
                                <p>Trân trọng,<br>Đội ngũ MediNet</p>
                            </div>
                            <div class='footer'>
                                <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                            </div>
                        </div>
                    </body>
                    </html>"
            };

            message.To.Add(new MailAddress(toEmail, toName));
            await client.SendMailAsync(message);
        }

        public async Task SendAccountDeletionEmailAsync(string toEmail, string toName)
        {
            var client = new SmtpClient(_smtpHost, _smtpPort)
            {
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(_fromEmail, _fromName),
                Subject = "Thông báo xóa tài khoản MediNet",
                IsBodyHtml = true,
                Body = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #007bff; color: white; padding: 10px; text-align: center; }}
                    .content {{ padding: 20px; }}
                    .warning {{ font-size: 18px; color: #dc3545; margin: 15px 0; }}
                    .footer {{ text-align: center; font-size: 12px; color: #666; margin-top: 20px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>MediNet</h2>
                    </div>
                    <div class='content'>
                        <p>Xin chào {toName},</p>
                        <p>Chúng tôi xác nhận rằng bạn đã yêu cầu xóa tài khoản MediNet của mình.</p>
                        <p>Tài khoản của bạn đã được đặt ở trạng thái không hoạt động và sẽ bị xóa hoàn toàn khỏi hệ thống sau 30 ngày.</p>
                        <div class='warning'>
                            <p>Lưu ý quan trọng: Nếu bạn muốn khôi phục tài khoản, vui lòng đăng nhập lại trong vòng 30 ngày kể từ hôm nay.</p>
                        </div>
                        <p>Nếu đây không phải là yêu cầu của bạn hoặc bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua địa chỉ hỗ trợ: support@medinet.com</p>
                        <p>Trân trọng,<br>Đội ngũ MediNet</p>
                    </div>
                    <div class='footer'>
                        <p>Email này được gửi tự động, vui lòng không trả lời.</p>
                        <p>© {DateTime.Now.Year} MediNet. Tất cả các quyền được bảo lưu.</p>
                    </div>
                </div>
            </body>
            </html>"
            };

            message.To.Add(new MailAddress(toEmail, toName));
            await client.SendMailAsync(message);
        }
    }
}