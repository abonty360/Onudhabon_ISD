# Onudhabon - Volunteer Based Educational Platform for Underprivileged Children

Onudhabon is an enterprise-grade, community-driven volunteer educational platform designed to empower underprivileged and marginalized students across Bangladesh. Built with **ASP.NET Core 10 (MVC)**, **Entity Framework Core 10**, **Microsoft SQL Server**, **Cloudinary CDN**, **SSLCommerz / bKash Gateway**, and **Google Gemini AI**, the platform connects passionate educators, local guardians, donors, and system administrators to deliver quality education, monitor student growth, and incentivize volunteer contributions.

---

##  Key Features & Architectural Modules

- **Volunteer Role Governance & Access Control**: Granular role-based workflows for **Admin**, **Educator**, **Local Guardian**, and **Donor/Visitor** with cookie authentication, sliding expiration, and automatic account locking.
- **Curriculum-Aligned Recorded Video Lectures & Materials**: Cloudinary-backed video lectures with automatic thumbnail generation and PDF study notes strictly categorized by National Curriculum (NCTB) Classes 1–12 and subjects.
- **Dynamic Search, Filtering & Instant Clear**: Client-side cascading subject/topic dropdowns, real-time keyword search, client-side pagination, and history-state filter resets.
- **Comprehensive Student Lifecycle & Progress Tracking**: End-to-end student enrollment, attendance monitoring, lecture-by-lecture evaluations, grade calculations (GPA / letter grade), and automatic promotion upon reaching 100% curriculum completion.
- **Interactive AI Tutor & PDF Knowledge Extraction (RAG)**: Integration with Google Gemini (`gemini-flash-lite`) and `UglyToad.PdfPig` to provide instant educational assistance, homework breakdown, and question solving from uploaded textbooks.
- **Volunteer Ranking & Gamification Engine**: Dynamic monthly ranking system that tracks educator uploads and local guardian student mentoring performance to award automated badges and reward distributions.
- **Community Forum & Moderation**: Discussion threads with categorized tags, nested comment replies, and unique upvote/downvote reaction tracking.
- **Donation Management & Payment Gateway**: Transparent donation workflows supporting SSLCommerz sandbox/production gateways, instant bKash checkout simulation, automated receipt generation, and donor anonymity.
- **Security, Email OTP Verification & Brute-Force Defense**: 6-digit email OTP account verification, password reset tokens via SMTP, and 5-strike failed attempt automated account locking with 3-strike security warnings.

---

## 🛠️ Technology Stack

| Layer | Technology |
|---|---|
| **Framework** | .NET 10.0 (ASP.NET Core MVC) |
| **Language** | C# 13 |
| **Database & ORM** | Microsoft SQL Server Express, Entity Framework Core 10 |
| **Cloud Media Storage** | Cloudinary .NET SDK (`CloudinaryDotNet`) |
| **Artificial Intelligence** | Google Gemini API (`gemini-flash-lite-latest`) |
| **PDF Extraction** | `UglyToad.PdfPig` |
| **Payment Gateway** | SSLCommerz Payment Gateway & bKash REST API |
| **Email Service** | SMTP Client (Gmail SMTP / Custom Mail Server) |
| **Frontend UI** | Razor Views (`.cshtml`), HTML5, CSS3, Bootstrap 5, Bootstrap Icons |

---

## ⚙️ Environment Configuration (`.env` / `appsettings.json`)

Create a `.env` file in the project root or configure `appsettings.json`. **Never commit actual production secrets to source control.**

### Sample `.env` Configuration:
```env
# Database Connection
ConnectionStrings__DefaultConnection=Server=localhost\SQLEXPRESS;Database=OnudhabonDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true

# Google Gemini AI API Key
GEMINI_API_KEY=your_gemini_api_key_here
GEMINI_MODEL=gemini-flash-lite-latest

# Cloudinary Storage
CLOUDINARY_CLOUD_NAME=your_cloudinary_cloud_name
CLOUDINARY_API_KEY=your_cloudinary_api_key
CLOUDINARY_API_SECRET=your_cloudinary_api_secret

# Email Verification (SMTP)
EMAIL_SMTP_HOST=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_SENDER_EMAIL=your_email@gmail.com
EMAIL_SENDER_PASSWORD=your_app_specific_password_here
EMAIL_SENDER_NAME=Onudhabon ISD

# SSLCommerz Payment Gateway
SSLCOMMERZ_STORE_ID=your_store_id
SSLCOMMERZ_STORE_PASSWORD=your_store_password
SSLCOMMERZ_IS_SANDBOX=true
```

---

## 📂 Project Structure

```text
Onudhabon_ISD/
├── Controllers/
│   ├── AccountController.cs       # Authentication, 5-strike lockout, OTP & Password Reset
│   ├── AdminController.cs         # Dashboard, Volunteer verification, Content approval
│   ├── ChatController.cs          # AI Tutor (Gemini) and PDF RAG processing
│   ├── DonationController.cs      # SSLCommerz & bKash donation workflows
│   ├── ForumController.cs         # Posts, Comments, Reactions & Moderation
│   ├── HomeController.cs          # Landing page, Impact statistics, Public queries
│   ├── LectureController.cs       # Video upload, Cloudinary integration, Filtering
│   ├── MaterialController.cs      # Document upload, PDF viewing, Download counts
│   └── StudentController.cs       # Enrollment, Progress tracking, Lecture evaluation
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DbContext & Entity Fluent API configurations
│   ├── ClassPlanHelper.cs         # NCTB curriculum mapping and class normalization
│   └── DbInitializer.cs           # Database seeding (Admin, ClassPlans, Students, Posts)
├── Models/                        # Domain entities & ViewModels
├── Services/                      # Cloudinary, Gemini AI, Email, SSLCommerz & Ranking Services
├── Views/                         # Razor UI Views grouped by controller
└── wwwroot/                       # Static CSS, JS, Images, and Vendor libraries
```


## 🚦 Getting Started & Local Setup

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Microsoft SQL Server / SQL Server Express](https://www.microsoft.com/sql-server/)
- [Visual Studio 2022 / 2025](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### Installation Steps

1. **Clone the repository**:
   ```bash
   git clone https://github.com/your-username/Onudhabon_ISD.git
   cd Onudhabon_ISD
   ```

2. **Configure Database Connection**:
   Ensure `DefaultConnection` in `appsettings.json` points to your local SQL Server instance:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=OnudhabonDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
   }
   ```

3. **Apply Database Migrations & Seed Data**:
   ```bash
   dotnet ef database update
   ```

4. **Build and Run the Application**:
   ```bash
   dotnet build
   dotnet run
   ```

5. **Access the Web Portal**:
   Open your browser and navigate to: `https://localhost:5001` or `http://localhost:5000`

