namespace Onudhabon_ISD.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends a verification email with a direct confirmation link.
        /// </summary>
        Task<bool> SendVerificationEmailAsync(string toEmail, string recipientName, string verificationUrl);

        /// <summary>
        /// Sends a 6-digit OTP verification code along with an optional verification link.
        /// </summary>
        Task<bool> SendOtpEmailAsync(string toEmail, string recipientName, string otpCode, string verificationUrl);

        /// <summary>
        /// Sends an email verification token to the specified email address.
        /// </summary>
        Task<bool> SendEmailVerificationAsync(string toEmail, string fullName, string verificationLink);

        /// <summary>
        /// Sends a general email message.
        /// </summary>
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);

        /// <summary>
        /// Sends a password reset email with a direct link.
        /// </summary>
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string recipientName, string resetUrl);

        /// <summary>
        /// Sends a 6-digit OTP code to reset password.
        /// </summary>
        Task<bool> SendPasswordResetOtpEmailAsync(string toEmail, string recipientName, string otp);
    }
}
