using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Onudhabon_ISD.Data;
using Onudhabon_ISD.Models;
using Onudhabon_ISD.Services;

namespace Onudhabon_ISD.Controllers
{
    public class MaterialController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<MaterialController> _logger;

        public MaterialController(
            ApplicationDbContext context,
            ICloudinaryService cloudinaryService,
            ILogger<MaterialController> logger)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        private async Task<List<string>> GetCurrentUserIdentifiersAsync()
        {
            var identifiers = new List<string>();
            var userName = User.Identity?.Name;
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrWhiteSpace(userName)) identifiers.Add(userName.Trim().ToLower());
            if (!string.IsNullOrWhiteSpace(userEmail)) identifiers.Add(userEmail.Trim().ToLower());

            if (int.TryParse(userIdClaim, out int uid))
            {
                var dbUser = await _context.Users.FindAsync(uid);
                if (dbUser != null)
                {
                    if (!string.IsNullOrWhiteSpace(dbUser.FullName)) identifiers.Add(dbUser.FullName.Trim().ToLower());
                    if (!string.IsNullOrWhiteSpace(dbUser.Email)) identifiers.Add(dbUser.Email.Trim().ToLower());
                }
            }

            return identifiers.Distinct().ToList();
        }

        // GET: /Material
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? search, string? classLevel, string? subject, string? topic, string? version)
        {
            // Automatically discover and sync any existing Cloudinary material assets if present
            try
            {
                var cloudinaryDocs = await _cloudinaryService.FetchCloudinaryMaterialsAsync();
                if (cloudinaryDocs.Any())
                {
                    var existingUrls = await _context.Materials.Select(m => m.FileUrl).ToListAsync();
                    var newMaterials = new List<Material>();

                    foreach (var cDoc in cloudinaryDocs)
                    {
                        if (!string.IsNullOrEmpty(cDoc.SecureUrl) && !existingUrls.Contains(cDoc.SecureUrl))
                        {
                            newMaterials.Add(new Material
                            {
                                Title = cDoc.DisplayTitle,
                                Description = $"Educational study material for {cDoc.DisplayTitle}",
                                Instructor = "Educator",
                                Version = "Bangla",
                                ClassLevel = "1",
                                Subject = "Bangla",
                                Topic = cDoc.DisplayTitle,
                                FileUrl = cDoc.SecureUrl,
                                Size = cDoc.FormattedSize,
                                Status = "Active",
                                Downloads = 0,
                                Date = cDoc.CreatedAt,
                                __v = 0
                            });
                        }
                    }

                    if (newMaterials.Any())
                    {
                        _context.Materials.AddRange(newMaterials);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Cloudinary material discovery skipped: {Message}", ex.Message);
            }

            var classPlans = await ClassPlanHelper.GetSortedClassPlansAsync(_context);
            var allSubjects = ClassPlanHelper.GetAllDistinctSubjects(classPlans);

            var isAdmin = User.IsInRole("Admin") || User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value == "Admin";
            var isEducator = User.IsInRole("Educator") || User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value == "Educator";
            var query = _context.Materials.AsQueryable();

            if (isAdmin || isEducator)
            {
                // Admins and Educators see all materials (active, approved, pending, declined)
            }
            else if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                // Logged-in users see all active materials plus their own uploads (including pending/declined)
                var userIdentifiers = await GetCurrentUserIdentifiersAsync();
                query = query.Where(m => m.Status == "Active" || m.Status == "active" || m.Status == "Approved" || m.Status == "approved" 
                    || (m.Instructor != null && userIdentifiers.Contains(m.Instructor.ToLower())));
            }
            else
            {
                // Anonymous visitors only see approved materials
                query = query.Where(m => m.Status == "Active" || m.Status == "active" || m.Status == "Approved" || m.Status == "approved");
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var cleanSearch = search.Trim().ToLower();
                query = query.Where(m =>
                    (m.Title != null && m.Title.ToLower().Contains(cleanSearch)) ||
                    (m.Instructor != null && m.Instructor.ToLower().Contains(cleanSearch)) ||
                    (m.Topic != null && m.Topic.ToLower().Contains(cleanSearch)) ||
                    (m.Subject != null && m.Subject.ToLower().Contains(cleanSearch)) ||
                    (m.Description != null && m.Description.ToLower().Contains(cleanSearch)));
            }

            if (!string.IsNullOrWhiteSpace(classLevel))
            {
                var normFilterClass = ClassPlanHelper.NormalizeClassLevel(classLevel);
                query = query.Where(m => m.ClassLevel == classLevel || m.ClassLevel == normFilterClass || m.ClassLevel == $"Class {normFilterClass}");
            }

            if (!string.IsNullOrWhiteSpace(subject))
            {
                query = query.Where(m => m.Subject == subject);
            }

            if (!string.IsNullOrWhiteSpace(topic))
            {
                query = query.Where(m => m.Topic == topic);
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                query = query.Where(m => m.Version == version);
            }

            var materials = await query
                .OrderByDescending(m => m.Date)
                .ToListAsync();

            ViewBag.ClassPlans = classPlans;
            ViewBag.ClassPlansJson = System.Text.Json.JsonSerializer.Serialize(classPlans.Select(p => new
            {
                classLevel = ClassPlanHelper.NormalizeClassLevel(p.ClassLevel),
                display = $"Class {ClassPlanHelper.NormalizeClassLevel(p.ClassLevel)}",
                subjects = p.Subjects?.Select(s => s.Name.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList() ?? new List<string>()
            }));
            ViewBag.AllSubjects = allSubjects;
            ViewBag.ClassLevel = classLevel;
            ViewBag.Subject = subject;
            ViewBag.Topic = topic;
            ViewBag.Search = search;
            ViewBag.Version = version;

            return View(materials);
        }

        // GET: /Material/Details/5
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var material = await _context.Materials.FirstOrDefaultAsync(m => m.Id == id);
            if (material == null)
            {
                return NotFound();
            }

            var isAdmin = User.IsInRole("Admin") || User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value == "Admin";
            var isEducator = User.IsInRole("Educator") || User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value == "Educator";
            bool isApproved = material.Status == "Active" || material.Status == "Approved" || material.Status == "approved";
            bool isOwner = false;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdentifiers = await GetCurrentUserIdentifiersAsync();
                isOwner = !string.IsNullOrEmpty(material.Instructor) && userIdentifiers.Contains(material.Instructor.Trim().ToLower());
            }

            if (!isApproved && !isAdmin && !isEducator && !isOwner)
            {
                return NotFound();
            }

            return View(material);
        }

        // GET: /Material/Upload
        [HttpGet]
        [Authorize(Roles = "Educator")]
        public async Task<IActionResult> Upload()
        {
            var userFullName = User.Identity?.Name;
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int uid))
            {
                var dbUser = await _context.Users.FindAsync(uid);
                if (dbUser == null || dbUser.IsRestricted || 
                    (!dbUser.IsVerified && !string.Equals(dbUser.VerificationStatus, "Active", StringComparison.OrdinalIgnoreCase) && !string.Equals(dbUser.VerificationStatus, "Approved", StringComparison.OrdinalIgnoreCase)))
                {
                    TempData["ErrorMessage"] = "Your account is pending administrator approval. You can only visit pages until an administrator approves your account.";
                    return RedirectToAction("Index", "Material");
                }

                if (!string.IsNullOrWhiteSpace(dbUser.FullName))
                {
                    userFullName = dbUser.FullName;
                }
            }

            var classPlans = await ClassPlanHelper.GetSortedClassPlansAsync(_context);
            ViewBag.ClassPlansJson = System.Text.Json.JsonSerializer.Serialize(classPlans.Select(p => new
            {
                classLevel = ClassPlanHelper.NormalizeClassLevel(p.ClassLevel),
                display = $"Class {ClassPlanHelper.NormalizeClassLevel(p.ClassLevel)}",
                subjects = p.Subjects?.Select(s => s.Name.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList() ?? new List<string>()
            }));

            return View(new MaterialUploadViewModel
            {
                Instructor = userFullName ?? "Educator"
            });
        }

        // POST: /Material/Upload
        [HttpPost]
        [Authorize(Roles = "Educator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(MaterialUploadViewModel model)
        {
            var userFullName = User.Identity?.Name;
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int uid))
            {
                var dbUser = await _context.Users.FindAsync(uid);
                if (dbUser == null || dbUser.IsRestricted || 
                    (!dbUser.IsVerified && !string.Equals(dbUser.VerificationStatus, "Active", StringComparison.OrdinalIgnoreCase) && !string.Equals(dbUser.VerificationStatus, "Approved", StringComparison.OrdinalIgnoreCase)))
                {
                    TempData["ErrorMessage"] = "Your account is pending administrator approval. You can only visit pages until an administrator approves your account.";
                    return RedirectToAction("Index", "Material");
                }

                if (!string.IsNullOrWhiteSpace(dbUser.FullName))
                {
                    userFullName = dbUser.FullName;
                }
            }

            // Always enforce user profile name as Instructor
            model.Instructor = userFullName ?? (!string.IsNullOrWhiteSpace(model.Instructor) ? model.Instructor.Trim() : "Educator");

            if (!ModelState.IsValid)
            {
                var classPlans = await ClassPlanHelper.GetSortedClassPlansAsync(_context);
                ViewBag.ClassPlansJson = System.Text.Json.JsonSerializer.Serialize(classPlans.Select(p => new
                {
                    classLevel = ClassPlanHelper.NormalizeClassLevel(p.ClassLevel),
                    display = $"Class {ClassPlanHelper.NormalizeClassLevel(p.ClassLevel)}",
                    subjects = p.Subjects?.Select(s => s.Name.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList() ?? new List<string>()
                }));
                return View(model);
            }

            if (model.MaterialFile == null || model.MaterialFile.Length == 0)
            {
                ModelState.AddModelError(nameof(model.MaterialFile), "Please select a material document file (.pdf, .docx, .doc, .pptx, .ppt) to upload.");
                var classPlans = await ClassPlanHelper.GetSortedClassPlansAsync(_context);
                ViewBag.ClassPlansJson = System.Text.Json.JsonSerializer.Serialize(classPlans.Select(p => new
                {
                    classLevel = ClassPlanHelper.NormalizeClassLevel(p.ClassLevel),
                    display = $"Class {ClassPlanHelper.NormalizeClassLevel(p.ClassLevel)}",
                    subjects = p.Subjects?.Select(s => s.Name.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList() ?? new List<string>()
                }));
                return View(model);
            }

            var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".pptx", ".ppt" };
            var fileExt = Path.GetExtension(model.MaterialFile.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(fileExt) || !allowedExtensions.Contains(fileExt))
            {
                ModelState.AddModelError(nameof(model.MaterialFile), "Invalid file type. Only document files (.pdf, .docx, .doc, .pptx, .ppt) are allowed.");
                var classPlans = await ClassPlanHelper.GetSortedClassPlansAsync(_context);
                ViewBag.ClassPlansJson = System.Text.Json.JsonSerializer.Serialize(classPlans.Select(p => new
                {
                    classLevel = ClassPlanHelper.NormalizeClassLevel(p.ClassLevel),
                    display = $"Class {ClassPlanHelper.NormalizeClassLevel(p.ClassLevel)}",
                    subjects = p.Subjects?.Select(s => s.Name.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList() ?? new List<string>()
                }));
                return View(model);
            }

            var uploadResult = await _cloudinaryService.UploadMaterialPdfAsync(model.MaterialFile);

            if (!uploadResult.Success)
            {
                ModelState.AddModelError(nameof(model.MaterialFile), uploadResult.ErrorMessage ?? "Failed to upload document to Cloudinary.");
                var classPlans = await ClassPlanHelper.GetSortedClassPlansAsync(_context);
                ViewBag.ClassPlansJson = System.Text.Json.JsonSerializer.Serialize(classPlans.Select(p => new
                {
                    classLevel = ClassPlanHelper.NormalizeClassLevel(p.ClassLevel),
                    display = $"Class {ClassPlanHelper.NormalizeClassLevel(p.ClassLevel)}",
                    subjects = p.Subjects?.Select(s => s.Name.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList() ?? new List<string>()
                }));
                return View(model);
            }

            string? fileUrl = uploadResult.SecureUrl;
            string? size = uploadResult.FormattedSize;

            var material = new Material
            {
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                Instructor = model.Instructor,
                Version = model.Version?.Trim() ?? "Bangla",
                ClassLevel = model.ClassLevel.Trim(),
                Subject = model.Subject.Trim(),
                Topic = model.Topic.Trim(),
                FileUrl = fileUrl,
                Size = size,
                Status = "pending",
                Downloads = 0,
                Date = DateTime.UtcNow,
                __v = 0
            };

            _context.Materials.Add(material);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material uploaded successfully with status 'pending'!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Material/Download/5
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var material = await _context.Materials.FirstOrDefaultAsync(m => m.Id == id);
            if (material == null || string.IsNullOrWhiteSpace(material.FileUrl))
            {
                return NotFound();
            }

            var isAdmin = User.IsInRole("Admin");
            bool isApproved = material.Status == "Active" || material.Status == "Approved" || material.Status == "approved";
            bool isOwner = false;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdentifiers = await GetCurrentUserIdentifiersAsync();
                isOwner = !string.IsNullOrEmpty(material.Instructor) && userIdentifiers.Contains(material.Instructor.Trim().ToLower());
            }

            if (!isApproved && !isAdmin && !isOwner)
            {
                return NotFound();
            }

            // Increment download count
            material.Downloads++;
            await _context.SaveChangesAsync();

            return Redirect(material.FileUrl);
        }

        // GET: /Material/GetApproved
        [HttpGet]
        public async Task<IActionResult> GetApproved(string? classLevel, string? subject)
        {
            var query = _context.Materials
                .Where(m => m.Status == "Active" || m.Status == "active" || m.Status == "Approved" || m.Status == "approved");

            if (!string.IsNullOrWhiteSpace(classLevel))
            {
                var normFilterClass = ClassPlanHelper.NormalizeClassLevel(classLevel);
                query = query.Where(m => m.ClassLevel == classLevel || m.ClassLevel == normFilterClass || m.ClassLevel == $"Class {normFilterClass}");
            }

            if (!string.IsNullOrWhiteSpace(subject))
            {
                query = query.Where(m => m.Subject == subject);
            }

            var approvedMaterials = await query
                .OrderByDescending(m => m.Date)
                .Select(m => new
                {
                    m.Id,
                    m.Title,
                    m.Description,
                    m.Instructor,
                    m.Version,
                    m.ClassLevel,
                    m.Subject,
                    m.Topic,
                    m.FileUrl,
                    m.Size,
                    m.Downloads,
                    m.Status,
                    m.Date
                })
                .ToListAsync();

            return Json(approvedMaterials);
        }

        // GET: /Material/GetTopicsBySubject
        [HttpGet]
        public async Task<IActionResult> GetTopicsBySubject(string? classLevel, string? subject)
        {
            if (string.IsNullOrWhiteSpace(subject))
            {
                return Json(Array.Empty<string>());
            }

            var isAdmin = User.IsInRole("Admin");
            var query = _context.Materials.Where(m => m.Subject == subject);

            if (!isAdmin)
            {
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    var userIdentifiers = await GetCurrentUserIdentifiersAsync();
                    query = query.Where(m => m.Status == "Active" || m.Status == "active" || m.Status == "Approved" || m.Status == "approved" 
                        || (m.Instructor != null && userIdentifiers.Contains(m.Instructor.ToLower())));
                }
                else
                {
                    query = query.Where(m => m.Status == "Active" || m.Status == "active" || m.Status == "Approved" || m.Status == "approved");
                }
            }

            if (!string.IsNullOrWhiteSpace(classLevel))
            {
                var normFilterClass = ClassPlanHelper.NormalizeClassLevel(classLevel);
                query = query.Where(m => m.ClassLevel == classLevel || m.ClassLevel == normFilterClass || m.ClassLevel == $"Class {normFilterClass}");
            }

            var topics = await query
                .Where(m => !string.IsNullOrEmpty(m.Topic))
                .Select(m => m.Topic!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            return Json(topics);
        }
    }
}
