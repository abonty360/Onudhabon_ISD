using System.Net;
using System.Net.Mail;

namespace Onudhabon_ISD.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendOtpEmailAsync(string toEmail, string recipientName, string otpCode, string verificationUrl)
        {
            var subject = $"{otpCode} is your Onudhabon Email Verification Code";
            var displayName = string.IsNullOrWhiteSpace(recipientName) ? "Learner/Volunteer" : recipientName.Trim();

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Email Verification Code</title>
</head>
<body style='margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; padding: 40px 15px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='100%' style='max-width: 580px; background-color: #ffffff; border-radius: 16px; box-shadow: 0 4px 12px rgba(0,0,0,0.06); overflow: hidden; border: 1px solid #e2e8f0;'>
                    <!-- Brand Header -->
                    <tr>
                        <td style='background: linear-gradient(135deg, #1e3a8a 0%, #1d4ed8 100%); padding: 32px 30px; text-align: center;'>
                            <h1 style='margin: 0; color: #ffffff; font-size: 26px; font-weight: 800; letter-spacing: -0.5px;'>Onudhabon</h1>
                            <p style='margin: 6px 0 0 0; color: #93c5fd; font-size: 14px;'>Exploring Education & Community Learning</p>
                        </td>
                    </tr>
                    
                    <!-- Content Body -->
                    <tr>
                        <td style='padding: 36px 32px; color: #334155; font-size: 15px; line-height: 1.6;'>
                            <h2 style='margin: 0 0 16px 0; color: #0f172a; font-size: 20px; font-weight: 700;'>Welcome, {displayName}!</h2>
                            
                            <p style='margin: 0 0 18px 0;'>Thank you for registering on Onudhabon. Please use the 6-digit verification code below to confirm your email address:</p>
                            
                            <!-- OTP Box -->
                            <table role='presentation' cellspacing='0' cellpadding='0' style='margin: 24px auto;'>
                                <tr>
                                    <td align='center' style='background-color: #f1f5f9; border: 2px dashed #94a3b8; border-radius: 12px; padding: 18px 36px;'>
                                        <span style='font-family: ""Courier New"", Courier, monospace; font-size: 34px; font-weight: 800; letter-spacing: 8px; color: #1e3a8a;'>{otpCode}</span>
                                    </td>
                                </tr>
                            </table>

                            <p style='text-align: center; color: #64748b; font-size: 13px; margin: 0 0 24px 0;'>This OTP code is valid for <strong>15 minutes</strong>.</p>
                            
                            <!-- Direct Link Alternative -->
                            <div style='margin-top: 24px; padding-top: 20px; border-top: 1px solid #e2e8f0; text-align: center;'>
                                <p style='margin: 0 0 12px 0; font-size: 14px; color: #64748b;'>Alternatively, you can verify your account directly by clicking below:</p>
                                <a href='{verificationUrl}' style='display: inline-block; padding: 10px 24px; background-color: #2563eb; color: #ffffff; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 14px;'>Verify via Link</a>
                            </div>

                            <p style='margin: 28px 0 0 0; color: #94a3b8; font-size: 12px; border-top: 1px solid #f1f5f9; padding-top: 16px;'>If you did not create an account with Onudhabon, you can safely ignore this email.</p>
                        </td>
                    </tr>
                    
                    <!-- Footer -->
                    <tr>
                        <td style='background-color: #f8fafc; padding: 20px 30px; text-align: center; border-top: 1px solid #e2e8f0;'>
                            <p style='margin: 0; color: #94a3b8; font-size: 12px;'>&copy; {DateTime.UtcNow.Year} Onudhabon. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            _logger.LogInformation("GENERATED OTP FOR {Email}: {Otp}", toEmail, otpCode);
            return await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task<bool> SendVerificationEmailAsync(string toEmail, string recipientName, string verificationUrl)
        {
            var subject = "Verify Your Email Address - Onudhabon";
            var displayName = string.IsNullOrWhiteSpace(recipientName) ? "Learner/Volunteer" : recipientName.Trim();

            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Email Verification</title>
</head>
<body style='margin: 0; padding: 0; background-color: #f8fafc; font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif;'>
    <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='background-color: #f8fafc; padding: 40px 15px;'>
        <tr>
            <td align='center'>
                <table role='presentation' width='100%' style='max-width: 580px; background-color: #ffffff; border-radius: 16px; box-shadow: 0 4px 12px rgba(0,0,0,0.06); overflow: hidden; border: 1px solid #e2e8f0;'>
                    <tr>
                        <td style='background: linear-gradient(135deg, #1e3a8a 0%, #1d4ed8 100%); padding: 32px 30px; text-align: center;'>
                            <h1 style='margin: 0; color: #ffffff; font-size: 26px; font-weight: 800; letter-spacing: -0.5px;'>Onudhabon</h1>
                            <p style='margin: 6px 0 0 0; color: #93c5fd; font-size: 14px;'>Exploring Education & Community Learning</p>
                        </td>
                    </tr>
                    <tr>
                        <td style='padding: 36px 32px; color: #334155; font-size: 15px; line-height: 1.6;'>
                            <h2 style='margin: 0 0 16px 0; color: #0f172a; font-size: 20px; font-weight: 700;'>Welcome, {displayName}!</h2>
                            <p style='margin: 0 0 18px 0;'>Thank you for registering on Onudhabon. Please verify your email address to complete your registration:</p>
                            <div style='text-align: center; margin: 28px 0;'>
                                <a href='{verificationUrl}' style='display: inline-block; padding: 12px 32px; background-color: #2563eb; color: #ffffff; text-decoration: none; border-radius: 8px; font-weight: 600; font-size: 15px;'>Verify Email Address</a>
                            </div>
                            <p style='word-break: break-all; font-size: 12px; color: #64748b;'>{verificationUrl}</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task<bool> SendEmailVerificationAsync(string toEmail, string fullName, string verificationLink)
        {
            return await SendVerificationEmailAsync(toEmail, fullName, verificationLink);
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var smtpHost = Environment.GetEnvironmentVariable("EMAIL_SMTP_HOST");
                if (string.IsNullOrWhiteSpace(smtpHost))
                {
                    smtpHost = _configuration["Email:SmtpHost"];
                }
                if (string.IsNullOrWhiteSpace(smtpHost))
                {
                    smtpHost = "smtp.gmail.com";
                }

                var smtpPortStr = Environment.GetEnvironmentVariable("EMAIL_SMTP_PORT");
                if (string.IsNullOrWhiteSpace(smtpPortStr))
                {
                    smtpPortStr = _configuration["Email:SmtpPort"];
                }
                if (!int.TryParse(smtpPortStr, out var smtpPort) || smtpPort <= 0)
                {
                    smtpPort = 587;
                }

                var senderEmail = Environment.GetEnvironmentVariable("EMAIL_SENDER_EMAIL");
                if (string.IsNullOrWhiteSpace(senderEmail))
                {
                    senderEmail = _configuration["Email:SenderEmail"];
                }
                if (string.IsNullOrWhiteSpace(senderEmail))
                {
                    throw new InvalidOperationException("Sender email not configured in .env or appsettings.json.");
                }

                var senderPassword = Environment.GetEnvironmentVariable("EMAIL_SENDER_PASSWORD");
                if (string.IsNullOrWhiteSpace(senderPassword))
                {
                    senderPassword = _configuration["Email:SenderPassword"];
                }
                if (string.IsNullOrWhiteSpace(senderPassword))
                {
                    throw new InvalidOperationException("Sender password not configured in .env (EMAIL_SENDER_PASSWORD) or appsettings.json (Email:SenderPassword).");
                }

                // Normalize App Password (strip all whitespace and line breaks)
                senderPassword = senderPassword.Trim().Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");

                var senderName = Environment.GetEnvironmentVariable("EMAIL_SENDER_NAME");
                if (string.IsNullOrWhiteSpace(senderName))
                {
                    senderName = _configuration["Email:SenderName"];
                }
                if (string.IsNullOrWhiteSpace(senderName))
                {
                    senderName = "Onudhabon ISD";
                }

                using (var client = new SmtpClient(smtpHost.Trim(), smtpPort))
                {
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(senderEmail.Trim(), senderPassword);
                    client.Timeout = 15000;

                    using var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail.Trim(), senderName.Trim()),
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(toEmail.Trim());

                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {ToEmail}: {ErrorMessage}", toEmail, ex.Message);
                return false;
            }
        }
    }
}
