namespace Agent_Framework_9_Basic_RAG;

/// <summary>
/// Represents a document that can be stored and searched in a vector store.
/// </summary>
public class TextSearchDocument
{
    public string SourceId { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string SourceLink { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
