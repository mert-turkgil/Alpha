using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Alpha.EmailServices
{
  public class SmtpEmailSender : IEmailSender
    {
        private string _host;
        private int _port;
        private bool _enableSSL;
        private string _username;
        private string _password;
        private string _fromEmail;
        private string _fromName;
        
        public SmtpEmailSender(string host, int port, bool enableSSL, string username, string password, string? fromEmail = null, string? fromName = null)
        {
            this._enableSSL = enableSSL;
            this._host = host;
            this._password = password;
            this._username = username;
            this._port = port;
            this._fromEmail = fromEmail ?? username;
            this._fromName = fromName ?? "Alpha Safety Shoes";
        }
        
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var client = new SmtpClient(this._host, this._port)
                {
                    Credentials = new NetworkCredential(_username, _password),
                    EnableSsl = this._enableSSL,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = 30000 // 30 seconds timeout
                };

                var fromAddress = new MailAddress(_fromEmail, _fromName);
                var toAddress = new MailAddress(email);
                
                var mailMessage = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true,
                    Priority = MailPriority.Normal
                };

                // Add UTF-8 encoding for international characters
                mailMessage.BodyEncoding = System.Text.Encoding.UTF8;
                mailMessage.SubjectEncoding = System.Text.Encoding.UTF8;

                // Explicitly set content type to text/html
                mailMessage.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                    htmlMessage, 
                    System.Text.Encoding.UTF8, 
                    System.Net.Mime.MediaTypeNames.Text.Html
                ));

                // Add custom headers for Cloudflare Email Workers verification
                mailMessage.Headers.Add("X-Alpha-Contact-Form", "true");
                mailMessage.Headers.Add("X-Turnstile-Verified", "true");
                mailMessage.Headers.Add("X-Sent-From", "Alpha-Contact-Application");
                mailMessage.Headers.Add("X-App-Version", "1.0");

                Console.WriteLine($"[SMTP] Sending email to: {email}");
                Console.WriteLine($"[SMTP] From: {_fromEmail} ({_fromName})");
                Console.WriteLine($"[SMTP] Subject: {subject}");
                
                await client.SendMailAsync(mailMessage);
                
                Console.WriteLine($"[SMTP] Email sent successfully!");
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"[SMTP ERROR] Code: {smtpEx.StatusCode}");
                Console.WriteLine($"[SMTP ERROR] Message: {smtpEx.Message}");
                Console.WriteLine($"[SMTP ERROR] Inner: {smtpEx.InnerException?.Message}");
                throw new Exception($"Failed to send email: {smtpEx.Message}", smtpEx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] {ex.Message}");
                Console.WriteLine($"[EMAIL ERROR] Stack: {ex.StackTrace}");
                throw new Exception($"Email sending failed: {ex.Message}", ex);
            }
        }


    }
}