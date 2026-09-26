# Email Verification - Quick Start Guide

## Steps to Activate Email Verification

### 1. Apply Database Migration
Run the following command to create the email verification columns in the database:

```powershell
dotnet ef database update
```

This will execute the migration:
- `20261210000000_AddEmailVerificationToUser.cs`

Which adds three columns to the `Users` table:
- `EmailConfirmed` (bit, default: 0)
- `EmailVerificationToken` (nvarchar(500), nullable)  
- `EmailVerificationTokenExpiry` (datetime2, nullable)

### 2. Verify Email Configuration
Check that `appsettings.Development.json` has the correct Gmail SMTP settings:

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

**Note**: The password `ebwe kmyo jwfp gawx` is a Gmail App-specific password. To use this:
1. Log in to `abontyflora@gmail.com`
2. Go to Google Account settings
3. Navigate to "Security" → "App passwords"
4. Select "Mail" and "Windows Computer"
5. Generate a new app-specific password
6. Copy and update the `SenderPassword` value if needed

### 3. Test Email Verification Flow

1. **Stop any running application instances**
   - Close the app in Visual Studio or stop the debug session
   - This is needed because the application file may be locked during build

2. **Build the solution**
   ```powershell
   dotnet build
   ```

3. **Run the application**
   ```powershell
   dotnet run
   ```

4. **Register a new account**
   - Go to `/Account/Register`
   - Fill in all required fields
   - Choose a role (Educator or Local Guardian)
   - Click Register

5. **Check your inbox**
   - An email should arrive from "Onudhabon ISD" with subject "Email Verification - Onudhabon ISD"
   - The email contains a verification link

6. **Click the verification link**
   - The link will be in the format: `https://yoursite/Account/VerifyEmail?token=XXX&email=user@example.com`
   - After clicking, you should see a success message
   - You're now ready to log in!

## Troubleshooting

### Email Not Being Sent
- Check that `appsettings.Development.json` has correct Gmail credentials
- Verify Gmail App-specific password is correct (not regular Gmail password)
- Check Application logs for SMTP errors
- Ensure 2-Factor Authentication is enabled on Gmail account
- Verify firewall allows outbound SMTP on port 587

### Verification Link Expired
- Links expire after 24 hours
- If link is expired, user must register again to get a new link

### Database Migration Fails
- Ensure you have proper SQL Server connection
- Run `dotnet ef database update` from the project directory
- Check `appsettings.json` for correct `DefaultConnection` string

### Build Error: File Locked
- Stop the running application in Visual Studio
- Close any other instances of the application
- Run `dotnet clean` followed by `dotnet build`

## Files Modified

| File | Purpose |
|------|---------|
| `Services/IEmailService.cs` | Email service interface |
| `Services/EmailService.cs` | Gmail SMTP implementation |
| `Controllers/AccountController.cs` | Register with email verification, new VerifyEmail action |
| `Models/User.cs` | Added EmailConfirmed, EmailVerificationToken, EmailVerificationTokenExpiry |
| `Program.cs` | DI registration for IEmailService |
| `appsettings.json` | Email configuration section |
| `appsettings.Development.json` | Development Gmail SMTP settings |
| `Migrations/20261210000000_AddEmailVerificationToUser.cs` | Database schema migration |
| `Migrations/20261210000000_AddEmailVerificationToUser.Designer.cs` | Migration designer |

## Integration with Existing Flow

The email verification integrates with the existing approval workflow:
1. User registers and receives verification email
2. User confirms email by clicking link
3. User logs in with confirmed email
4. Account goes through admin approval process
5. Once admin approves, user has full access

**Note**: Email verification is separate from admin approval. Both are required for full account activation.

## Next Steps (Optional)

- Add UI to allow users to resend verification email
- Add admin panel to manage unverified emails
- Implement role-based access for unverified users
- Add email verification reminder notifications
