namespace AVA.Vault.Core.Graph
{
    public class NoteLink
    {
        public string SourceTitle { get; set; }
        public string TargetTitle { get; set; }

        public NoteLink() { }

        public NoteLink(string source, string target)
        {
            SourceTitle = source;
            TargetTitle = target;
        }

        public override string ToString()
        {
            return $"{SourceTitle} ? {TargetTitle}";
        }
    }
}
