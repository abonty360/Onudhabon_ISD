using System.ComponentModel.DataAnnotations;

namespace Onudhabon_ISD.Models
{
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the 6-digit verification code.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "The OTP must be exactly 6 digits.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "The OTP must contain digits only.")]
        [Display(Name = "Verification Code (OTP)")]
        public string Otp { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
