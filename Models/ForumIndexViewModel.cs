namespace Onudhabon_ISD.Models
{
    public class ForumIndexViewModel
    {
        public IEnumerable<ForumPost> Posts { get; set; } = Array.Empty<ForumPost>();
        public Dictionary<string, ForumAuthorPreviewViewModel> AuthorPreviews { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class ForumAuthorPreviewViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string? Picture { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Education { get; set; } = string.Empty;
        public DateTime MemberSince { get; set; }
        public int ForumPostCount { get; set; }
        public int LectureCount { get; set; }
    }
}