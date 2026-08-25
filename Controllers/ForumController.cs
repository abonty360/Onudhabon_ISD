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

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var posts = await _context.ForumPosts
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(posts);
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

            var post = new ForumPost
            {
                Title = title.Trim(),
                Content = content.Trim(),
                Author = userName,
                AuthorRole = userRole,
                Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(),
                Tags = string.IsNullOrWhiteSpace(tags) ? "#discussion" : (tags.StartsWith("#") ? tags.Trim() : "#" + tags.Trim()),
                CreatedAt = DateTime.UtcNow,
                Likes = 0,
                Dislikes = 0,
                Replies = 0
            };

            _context.ForumPosts.Add(post);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Forum post created successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Like(int id)
        {
            var post = await _context.ForumPosts.FindAsync(id);
            if (post != null)
            {
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

                await _context.SaveChangesAsync();
                return Json(new { success = true, likes = post.Likes });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Dislike(int id)
        {
            var post = await _context.ForumPosts.FindAsync(id);
            if (post != null)
            {
                post.Dislikes += 1;
                await _context.SaveChangesAsync();
                return Json(new { success = true, dislikes = post.Dislikes });
            }
            return Json(new { success = false });
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
            if (string.IsNullOrEmpty(userName)) return Json(new List<object>());

            var userPosts = await _context.ForumPosts.ToListAsync();
            var notifications = await _context.Notifications
                .Where(n => n.User == userName)
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
            if (string.IsNullOrEmpty(userName)) return Json(new { success = false });

            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.User == userName);
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
            if (string.IsNullOrEmpty(userName)) return Json(new { success = false });

            var unread = await _context.Notifications
                .Where(n => n.User == userName && !n.IsRead)
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
