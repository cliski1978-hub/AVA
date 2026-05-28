namespace AVA.Vault.Core.Interfaces
{
    public interface IMarkdownRenderer
    {
        /// <summary>
        /// Converts raw markdown into a renderable UI format (HTML or FlowDocument).
        /// </summary>
        /// <param name="markdown">The markdown text.</param>
        /// <returns>A string (HTML) or object (UI element), depending on platform.</returns>
        object Render(string markdown);
    }
}
