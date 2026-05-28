using System;
using System.Collections.Generic;

namespace AVA.Vault.Core.Models
{
    public class MarkdownNote
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Content { get; set; }

        public List<string> Tags { get; set; } = new();
        public List<string> Links { get; set; } = new();

        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime Modified { get; set; } = DateTime.UtcNow;

        public MarkdownNote() { }

        public MarkdownNote(string title, string content)
        {
            Title = title;
            Content = content;
            Created = DateTime.UtcNow;
            Modified = DateTime.UtcNow;
        }
    }
}
