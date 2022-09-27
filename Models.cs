using System.Text.Json.Serialization;

namespace LeetcodeExporter;

public class SubmissionsResponse
{
    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }

    [JsonPropertyName("submissions_dump")]
    public IReadOnlyList<Submission> Submissions { get; set; } = Array.Empty<Submission>();
}

public class Submission
{
    public string Lang { get; set; } = null!;
    
    public int Timestamp { get; set; }
    
    [JsonPropertyName("status_display")]
    public string Status { get; set; } = null!;
    
    public string Runtime { get; set; } = null!;
    
    public string Title { get; set; } = null!;
    
    public string Memory { get; set; } = null!;

    [JsonPropertyName("title_slug")]
    public string TitleSlug { get; set; } = null!;
    
    public string Code { get; set; } = null!;
}