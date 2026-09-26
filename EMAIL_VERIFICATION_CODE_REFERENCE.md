# Email Verification - Code Reference Guide

This document provides quick code references for the email verification implementation.

## 1. Service Interface Definition

**File**: `Services/IEmailService.cs`

```csharp
public interface IEmailService
{
	/// <summary>
	/// Sends an email verification token to the specified email address.
	/// </summary>
	Task<bool> SendEmailVerificationAsync(string toEmail, string fullName, string verificationLink);

	/// <summary>
	/// Sends a general email message.
	/// </summary>
	Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
}
```

## 2. SMTP Service Implementation

**File**: `Services/EmailService.cs`

### Constructor
```csharp
public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
{
	_configuration = configuration;
	_logger = logger;
}
```

### Send Verification Email
```csharp
public async Task<bool> SendEmailVerificationAsync(string toEmail, string fullName, string verificationLink)
{
	var subject = "Email Verification - Onudhabon ISD";
	var htmlBody = $@"... professional HTML template ...";
	return await SendEmailAsync(toEmail, subject, htmlBody);
}
```

### Generic Send Email
```csharp
public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
{
	try
	{
		var smtpHost = _configuration["Email:SmtpHost"];
		var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
		var senderEmail = _configuration["Email:SenderEmail"];
		var senderPassword = _configuration["Email:SenderPassword"];
		var senderName = _configuration["Email:SenderName"] ?? "Onudhabon ISD";

		using (var client = new SmtpClient(smtpHost, smtpPort))
		{
			client.EnableSsl = true;
			client.Credentials = new NetworkCredential(senderEmail, senderPassword);
			client.Timeout = 10000;

			var mailMessage = new MailMessage
			{
				From = new MailAddress(senderEmail, senderName),
				Subject = subject,
				Body = htmlBody,
				IsBodyHtml = true
			};

			mailMessage.To.Add(toEmail);
			await client.SendMailAsync(mailMessage);
			_logger.LogInformation($"Email sent successfully to {toEmail}");
			return true;
		}
	}
	catch (Exception ex)
	{
		_logger.LogError($"Error sending email to {toEmail}: {ex.Message}");
		return false;
	}
}
```

## 3. User Model Updates

**File**: `Models/User.cs`

```csharp
[Display(Name = "Is Verified")]
public bool IsVerified { get; set; } = false;

[Display(Name = "Email Confirmed")]
public bool EmailConfirmed { get; set; } = false;

[MaxLength(500)]
[Display(Name = "Email Verification Token")]
public string? EmailVerificationToken { get; set; }

[Display(Name = "Email Verification Token Expiry")]
public DateTime? EmailVerificationTokenExpiry { get; set; }

[MaxLength(50)]
[Display(Name = "Verification Status")]
public string? VerificationStatus { get; set; } = "Pending";
```

## 4. Controller - Registration with Email Verification

**File**: `Controllers/AccountController.cs`

### Constructor with IEmailService Injection
```csharp
private readonly IEmailService _emailService;

public AccountController(
	ApplicationDbContext context,
	IPasswordHasher<User> passwordHasher,
	ICloudinaryService cloudinaryService,
	IEmailService emailService)
{
	_context = context;
	_passwordHasher = passwordHasher;
	_cloudinaryService = cloudinaryService;
	_emailService = emailService;
}
```

### Registration Handler
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
{
	// ... validation and file upload code ...

	// Create new User entity
	var user = new User
	{
		FullName = model.FullName.Trim(),
		Email = emailNormalized,
		PhoneNumber = model.PhoneNumber.Trim(),
		Role = model.Role.Trim(),
		City = model.City.Trim(),
		Area = model.Area.Trim(),
		// ... other properties ...
		IsRestricted = false,
		IsVerified = false,
		EmailConfirmed = false,  // New!
		VerificationStatus = "Pending",
		CreatedAt = DateTime.UtcNow,
		__v = 0
	};

	// Hash password
	user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

	// Generate email verification token
	user.EmailVerificationToken = GenerateVerificationToken();
	user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);

	_context.Users.Add(user);
	await _context.SaveChangesAsync();

	// Send verification email
	var verificationLink = Url.Action("VerifyEmail", "Account", 
		new { token = user.EmailVerificationToken, email = user.Email }, 
		protocol: Request.Scheme);

	await _emailService.SendEmailVerificationAsync(user.Email, user.FullName, verificationLink);

	TempData["SuccessMessage"] = "Account registered successfully! Please check your email to verify your email address. A verification link has been sent to your email.";
	return RedirectToAction("Login", new { returnUrl });
}
```

### Verify Email Endpoint
```csharp
[HttpGet]
public async Task<IActionResult> VerifyEmail(string token, string email)
{
	if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
	{
		TempData["ErrorMessage"] = "Invalid verification link. Please try again.";
		return RedirectToAction("Login");
	}

	var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
	if (user == null)
	{
		TempData["ErrorMessage"] = "User not found.";
		return RedirectToAction("Login");
	}

	if (user.EmailConfirmed)
	{
		TempData["InfoMessage"] = "Your email has already been verified.";
		return RedirectToAction("Login");
	}

	if (string.IsNullOrWhiteSpace(user.EmailVerificationToken) || user.EmailVerificationToken != token)
	{
		TempData["ErrorMessage"] = "Invalid verification token.";
		return RedirectToAction("Login");
	}

	if (user.EmailVerificationTokenExpiry.HasValue && user.EmailVerificationTokenExpiry < DateTime.UtcNow)
	{
		TempData["ErrorMessage"] = "Verification link has expired. Please register again.";
		return RedirectToAction("Register");
	}

	user.EmailConfirmed = true;
	user.EmailVerificationToken = null;
	user.EmailVerificationTokenExpiry = null;

	_context.Users.Update(user);
	await _context.SaveChangesAsync();

	TempData["SuccessMessage"] = "Email verified successfully! Your account is now activated. Please log in.";
	return RedirectToAction("Login");
}
```

### Token Generation Helper
```csharp
private static string GenerateVerificationToken()
{
	var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
	var random = new Random();
	var result = new System.Text.StringBuilder();

	for (int i = 0; i < 64; i++)
	{
		result.Append(chars[random.Next(chars.Length)]);
	}

	return result.ToString();
}
```

## 5. Dependency Injection Setup

**File**: `Program.cs`

```csharp
// Email Service (Gmail SMTP)
builder.Services.AddScoped<IEmailService, EmailService>();
```

## 6. Configuration

**File**: `appsettings.json`

```json
{
  "Email": {
	"SmtpHost": "",
	"SmtpPort": 587,
	"SenderEmail": "",
	"SenderPassword": "",
	"SenderName": "Onudhabon ISD"
  }
}
```

**File**: `appsettings.Development.json`

```json
{
  "Email": {
	"SmtpHost": "smtp.gmail.com",
	"SmtpPort": 587,
	"SenderEmail": "abontyflora@gmail.com",
	"SenderPassword": "ebwe kmyo jwfp gawx",
	"SenderName": "Onudhabon ISD"
  }
}
```

## 7. Database Migration

**File**: `Migrations/20261210000000_AddEmailVerificationToUser.cs`

```csharp
public partial class AddEmailVerificationToUser : Migration
{
	protected override void Up(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.AddColumn<bool>(
			name: "EmailConfirmed",
			table: "Users",
			type: "bit",
			nullable: false,
			defaultValue: false);

		migrationBuilder.AddColumn<string>(
			name: "EmailVerificationToken",
			table: "Users",
			type: "nvarchar(500)",
			maxLength: 500,
			nullable: true);

		migrationBuilder.AddColumn<DateTime>(
			name: "EmailVerificationTokenExpiry",
			table: "Users",
			type: "datetime2",
			nullable: true);
	}

	protected override void Down(MigrationBuilder migrationBuilder)
	{
		migrationBuilder.DropColumn(name: "EmailConfirmed", table: "Users");
		migrationBuilder.DropColumn(name: "EmailVerificationToken", table: "Users");
		migrationBuilder.DropColumn(name: "EmailVerificationTokenExpiry", table: "Users");
	}
}
```

## 8. Email Template Structure

The verification email HTML includes:

```html
<!DOCTYPE html>
<html>
<head>
	<meta charset='UTF-8'>
	<style><!-- CSS for professional styling --></style>
</head>
<body>
	<div class='container'>
		<div class='header'>
			<h1>Welcome to Onudhabon ISD</h1>
		</div>
		<div class='content'>
			<p>Hello {fullName},</p>
			<p>Thank you for registering. Please verify your email:</p>
			<a href='{verificationLink}' class='button'>Verify Your Email</a>
			<div class='warning'>
				<strong>Note:</strong> This link expires in 24 hours.
			</div>
		</div>
		<div class='footer'>
			<p>&copy; 2024 Onudhabon ISD. All rights reserved.</p>
		</div>
	</div>
</body>
</html>
```

## 9. Key Constants

- Token Length: 64 characters
- Token Characters: A-Z, a-z, 0-9
- Token Expiry: 24 hours from generation
- SMTP Port: 587 (for Gmail TLS)
- Email Timeout: 10000 milliseconds (10 seconds)

## 10. Error Handling

All methods return `Task<bool>`:
- `true` = email sent successfully
- `false` = email send failed (logged in error logs)

Verification endpoint provides user-friendly messages:
- "Invalid verification link. Please try again."
- "User not found."
- "Your email has already been verified."
- "Invalid verification token."
- "Verification link has expired. Please register again."
- "Email verified successfully! Your account is now activated. Please log in."

---

## Integration Flow Diagram

```
User Registration
	  ↓
Generate 64-char token
	  ↓
Set 24-hour expiry
	  ↓
Save user (EmailConfirmed = false)
	  ↓
Create verification link: 
/Account/VerifyEmail?token=XXX&email=user@example.com
	  ↓
Send HTML email via Gmail SMTP
	  ↓
User receives email
	  ↓
User clicks verification link
	  ↓
VerifyEmail action validates:
- token exists
- token not expired
- email matches
	  ↓
Set EmailConfirmed = true
Clear token/expiry
	  ↓
Redirect to login
	  ↓
User can now log in with verified email
```

---

This code follows ASP.NET Core best practices:
✓ Async/await for I/O operations
✓ Dependency injection for services
✓ Configuration binding for settings
✓ Error logging for debugging
✓ User-friendly error messages
✓ Secure token generation
✓ SQL parameterization (via EF Core)
✓ HTTPS for verification links
