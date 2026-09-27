using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Onudhabon_ISD.Data;
using Onudhabon_ISD.Models;
using System.Security.Claims;

namespace Onudhabon_ISD.Controllers
{
    public class ForumController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ForumController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return null;
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            return null;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var userName = User.Identity?.Name;
            var isAdmin = User.IsInRole("Admin");

            var query = _context.ForumPosts.AsQueryable();

            if (isAdmin)
            {
                // Admins see all posts
            }
            else if (!string.IsNullOrEmpty(userName))
            {
                // Logged in users see all active posts plus their own posts (including Pending/Declined)
                query = query.Where(p => p.Status == "Active" || p.Status == "Approved" || p.Status == "approved" || p.Author == userName);
            }
            else
            {
                // Anonymous visitors only see approved posts
                query = query.Where(p => p.Status == "Active" || p.Status == "Approved" || p.Status == "approved");
            }

            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var authorNames = posts
                .Where(post => !string.IsNullOrWhiteSpace(post.Author))
                .Select(post => post.Author!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var authorPreviews = new Dictionary<string, ForumAuthorPreviewViewModel>(StringComparer.OrdinalIgnoreCase);

            if (authorNames.Count > 0)
            {
                var users = await _context.Users
                    .Where(user => authorNames.Contains(user.FullName) || authorNames.Contains(user.Email))
                    .OrderBy(user => user.Id)
                    .Select(user => new
                    {
                        FullName = user.FullName,
                        Email = user.Email,
                        Picture = user.Picture,
                        Role = user.Role,
                        user.City,
                        user.Area,
                        user.EducationLevel,
                        user.Major,
                        user.UniversityName,
                        user.HscInstitute,
                        user.SscInstitute,
                        MemberSince = user.CreatedAt
                    })
                    .ToListAsync();

                foreach (var user in users)
                {
                    var preview = new ForumAuthorPreviewViewModel
                    {
                        FullName = user.FullName,
                        Picture = user.Picture,
                        Role = user.Role,
                        Location = string.Join(", ", new[] { user.City, user.Area }.Where(value => !string.IsNullOrWhiteSpace(value))),
                        Education = string.Join(" | ", new[]
                        {
                            user.EducationLevel,
                            user.Major,
                            user.UniversityName ?? user.HscInstitute ?? user.SscInstitute
                        }.Where(value => !string.IsNullOrWhiteSpace(value))),
                        MemberSince = user.MemberSince
                    };

                    foreach (var authorName in authorNames.Where(name =>
                        name.Equals(user.FullName, StringComparison.OrdinalIgnoreCase) ||
                        name.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
                    {
                        authorPreviews.TryAdd(authorName, preview);
                    }
                }

                foreach (var post in posts.Where(post => !string.IsNullOrWhiteSpace(post.Author)))
                {
                    authorPreviews.TryAdd(post.Author!, new ForumAuthorPreviewViewModel
                    {
                        FullName = post.Author!,
                        Role = post.AuthorRole ?? "User"
                    });
                }

                var publicForumAuthors = await _context.ForumPosts
                    .Where(post => post.Author != null && authorNames.Contains(post.Author))
                    .Select(post => post.Author!)
                    .ToListAsync();
                var publicLectureAuthors = await _context.Lectures
                    .Where(lecture => lecture.Instructor != null && authorNames.Contains(lecture.Instructor) &&
                        (lecture.Status != null && (lecture.Status.ToLower() == "active" || lecture.Status.ToLower() == "approved")))
                    .Select(lecture => lecture.Instructor!)
                    .ToListAsync();

                var forumPostCounts = publicForumAuthors
                    .GroupBy(author => author, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
                var lectureCounts = publicLectureAuthors
                    .GroupBy(author => author, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

                foreach (var (authorName, preview) in authorPreviews)
                {
                    preview.ForumPostCount = forumPostCounts.GetValueOrDefault(authorName);
                    preview.LectureCount = lectureCounts.GetValueOrDefault(authorName);
                    if (string.IsNullOrWhiteSpace(preview.Location))
                    {
                        preview.Location = "—";
                    }
                    if (string.IsNullOrWhiteSpace(preview.Education))
                    {
                        preview.Education = "—";
                    }
                }
            }

            var userReactions = new Dictionary<int, bool>(); // PostId -> IsLike (true = like, false = dislike)
            var currentUserId = GetCurrentUserId();
            if (currentUserId.HasValue)
            {
                var postIds = posts.Select(p => p.Id).ToList();
                var reactions = await _context.ForumPostReactions
                    .Where(r => r.UserId == currentUserId.Value && postIds.Contains(r.PostId))
                    .ToListAsync();

                userReactions = reactions.ToDictionary(r => r.PostId, r => r.IsLike);
            }

            ViewBag.UserReactions = userReactions;
            return View(new ForumIndexViewModel
            {
                Posts = posts,
                AuthorPreviews = authorPreviews
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(string title, string content, string? category, string? tags)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Title and Content are required.";
                return RedirectToAction(nameof(Index));
            }

            var userName = User.Identity?.Name ?? "Anonymous";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
            var isAdmin = User.IsInRole("Admin") || userRole == "Admin";

            var post = new ForumPost
            {
                Title = title.Trim(),
                Content = content.Trim(),
                Author = userName,
                AuthorRole = userRole,
                Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(),
                Tags = string.IsNullOrWhiteSpace(tags) ? "#discussion" : (tags.StartsWith("#") ? tags.Trim() : "#" + tags.Trim()),
                Status = isAdmin ? "Active" : "Pending",
                CreatedAt = DateTime.UtcNow,
                Likes = 0,
                Dislikes = 0,
                Replies = 0
            };

            _context.ForumPosts.Add(post);
            await _context.SaveChangesAsync();

            if (post.Status == "Active")
            {
                TempData["SuccessMessage"] = "Forum post created and published successfully!";
            }
            else
            {
                TempData["SuccessMessage"] = "Your discussion post has been submitted for admin approval (Status: Pending).";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Like(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Json(new { 
                    success = false, 
                    requireLogin = true, 
                    redirectUrl = Url.Action("Login", "Account", new { returnUrl = "/Forum" }) 
                });
            }

            var post = await _context.ForumPosts.FindAsync(id);
            if (post == null)
            {
                return Json(new { success = false, message = "Post not found." });
            }

            var userId = currentUserId.Value;
            var existingReaction = await _context.ForumPostReactions
                .FirstOrDefaultAsync(r => r.PostId == id && r.UserId == userId);

            string userReaction;

            if (existingReaction != null)
            {
                if (existingReaction.IsLike)
                {
                    // 2nd click on Like -> Undo Like
                    _context.ForumPostReactions.Remove(existingReaction);
                    post.Likes = Math.Max(0, post.Likes - 1);
                    userReaction = "none";
                }
                else
                {
                    // Switched from Dislike to Like
                    existingReaction.IsLike = true;
                    existingReaction.CreatedAt = DateTime.UtcNow;
                    post.Dislikes = Math.Max(0, post.Dislikes - 1);
                    post.Likes += 1;
                    userReaction = "like";
                }
            }
            else
            {
                // First reaction: Add Like
                _context.ForumPostReactions.Add(new ForumPostReaction
                {
                    PostId = id,
                    UserId = userId,
                    IsLike = true,
                    CreatedAt = DateTime.UtcNow
                });
                post.Likes += 1;

                var sender = User.Identity?.Name ?? "Someone";
                if (!string.IsNullOrEmpty(post.Author) && !post.Author.Equals(sender, StringComparison.OrdinalIgnoreCase))
                {
                    var notification = new Notification
                    {
                        User = post.Author,
                        Sender = sender,
                        Post = post.Title,
                        Type = "Like",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        __v = 0
                    };
                    _context.Notifications.Add(notification);
                }

                userReaction = "like";
            }

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                likes = post.Likes, 
                dislikes = post.Dislikes,
                userReaction = userReaction 
            });
        }

        [HttpPost]
        public async Task<IActionResult> Dislike(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return Json(new { 
                    success = false, 
                    requireLogin = true, 
                    redirectUrl = Url.Action("Login", "Account", new { returnUrl = "/Forum" }) 
                });
            }

            var post = await _context.ForumPosts.FindAsync(id);
            if (post == null)
            {
                return Json(new { success = false, message = "Post not found." });
            }

            var userId = currentUserId.Value;
            var existingReaction = await _context.ForumPostReactions
                .FirstOrDefaultAsync(r => r.PostId == id && r.UserId == userId);

            string userReaction;

            if (existingReaction != null)
            {
                if (!existingReaction.IsLike)
                {
                    // 2nd click on Dislike -> Undo Dislike
                    _context.ForumPostReactions.Remove(existingReaction);
                    post.Dislikes = Math.Max(0, post.Dislikes - 1);
                    userReaction = "none";
                }
                else
                {
                    // Switched from Like to Dislike
                    existingReaction.IsLike = false;
                    existingReaction.CreatedAt = DateTime.UtcNow;
                    post.Likes = Math.Max(0, post.Likes - 1);
                    post.Dislikes += 1;
                    userReaction = "dislike";
                }
            }
            else
            {
                // First reaction: Add Dislike
                _context.ForumPostReactions.Add(new ForumPostReaction
                {
                    PostId = id,
                    UserId = userId,
                    IsLike = false,
                    CreatedAt = DateTime.UtcNow
                });
                post.Dislikes += 1;
                userReaction = "dislike";
            }

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                likes = post.Likes, 
                dislikes = post.Dislikes,
                userReaction = userReaction 
            });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetComments(int postId)
        {
            var comments = await _context.ForumComments
                .Where(c => c.PostId == postId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new {
                    c.Id,
                    c.Content,
                    c.Author,
                    c.AuthorRole,
                    createdAt = c.CreatedAt.ToString("M/d/yyyy, h:mm:ss tt")
                })
                .ToListAsync();
            return Json(comments);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(int postId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Comment content cannot be empty." });
            }

            var post = await _context.ForumPosts.FindAsync(postId);
            if (post == null)
            {
                return Json(new { success = false, message = "Post not found." });
            }

            var userName = User.Identity?.Name ?? "Anonymous";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            var comment = new ForumComment
            {
                PostId = postId,
                Content = content.Trim(),
                Author = userName,
                AuthorRole = userRole,
                CreatedAt = DateTime.UtcNow
            };

            _context.ForumComments.Add(comment);
            post.Replies += 1;

            if (!string.IsNullOrEmpty(post.Author) && !post.Author.Equals(userName, StringComparison.OrdinalIgnoreCase))
            {
                var notification = new Notification
                {
                    User = post.Author,
                    Sender = userName,
                    Post = post.Title,
                    Type = "Comment",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    __v = 0
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                replies = post.Replies,
                comment = new {
                    comment.Id,
                    comment.Content,
                    comment.Author,
                    comment.AuthorRole,
                    createdAt = comment.CreatedAt.ToString("M/d/yyyy, h:mm:ss tt")
                }
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetNotifications()
        {
            var userName = User.Identity?.Name;
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(userEmail) && string.IsNullOrEmpty(userId))
                return Json(new List<object>());

            var userPosts = await _context.ForumPosts.ToListAsync();
            var notifications = await _context.Notifications
                .Where(n => (userName != null && n.User == userName) ||
                            (userEmail != null && n.User == userEmail) ||
                            (userId != null && n.User == userId))
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();

            var result = notifications.Select(n => {
                var targetPost = userPosts.FirstOrDefault(p => p.Title == n.Post);
                return new {
                    n.Id,
                    n.Sender,
                    n.Post,
                    n.Type,
                    n.IsRead,
                    postId = targetPost?.Id ?? 0,
                    createdAt = n.CreatedAt.ToString("M/d/yyyy, h:mm tt")
                };
            });

            return Json(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkSingleNotificationAsRead(int id)
        {
            var userName = User.Identity?.Name;
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(userEmail) && string.IsNullOrEmpty(userId))
                return Json(new { success = false });

            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && 
                ((userName != null && n.User == userName) || 
                 (userEmail != null && n.User == userEmail) || 
                 (userId != null && n.User == userId)));

            if (notif != null)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkNotificationsAsRead()
        {
            var userName = User.Identity?.Name;
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(userEmail) && string.IsNullOrEmpty(userId))
                return Json(new { success = false });

            var unread = await _context.Notifications
                .Where(n => !n.IsRead && 
                    ((userName != null && n.User == userName) || 
                     (userEmail != null && n.User == userEmail) || 
                     (userId != null && n.User == userId)))
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
