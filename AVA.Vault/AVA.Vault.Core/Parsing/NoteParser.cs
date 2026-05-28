using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AVA.Vault.Core.Models;

namespace AVA.Vault.Core.Parsing
{
    public class NoteParser
    {
        private static readonly Regex TagRegex = new(@"#(\w+)", RegexOptions.Compiled);
        private static readonly Regex LinkRegex = new(@"\[\[([^\]]+)\]\]", RegexOptions.Compiled);
        private static readonly Regex TitleRegex = new(@"^#\s(.+)$", RegexOptions.Compiled | RegexOptions.Multiline);

        public MarkdownNote Parse(string markdown)
        {
            var note = new MarkdownNote
            {
                Content = markdown,
                Title = ExtractTitle(markdown),
                Tags = ExtractTags(markdown),
                Links = ExtractLinks(markdown),
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };

            return note;
        }

        private string ExtractTitle(string markdown)
        {
            var match = TitleRegex.Match(markdown);
            return match.Success ? match.Groups[1].Value.Trim() : "Untitled Note";
        }

        private List<string> ExtractTags(string markdown)
        {
            var tags = new HashSet<string>();
            foreach (Match match in TagRegex.Matches(markdown))
            {
                tags.Add(match.Groups[1].Value.Trim());
            }
            return new List<string>(tags);
        }

        private List<string> ExtractLinks(string markdown)
        {
            var links = new HashSet<string>();
            foreach (Match match in LinkRegex.Matches(markdown))
            {
                links.Add(match.Groups[1].Value.Trim());
            }
            return new List<string>(links);
        }
    }
}
