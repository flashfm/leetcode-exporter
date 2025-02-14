using System.Text.Json.Serialization;

namespace LeetcodeExporter;

public class SubmissionsResponse
{
    [JsonPropertyName("has_next")]
    public bool HasNext { get; set; }

    [JsonPropertyName("submissions_dump")]
    public IReadOnlyList<Submission> Submissions { get; set; } = [];
}

public class Submission
{
    public string Lang { get; set; } = "";
    
    public int Timestamp { get; set; }
    
    [JsonPropertyName("status_display")]
    public string Status { get; set; } = "";

    [JsonPropertyName("question_id")]
    public int QuestionId { get; set; }
    
    public string Runtime { get; set; } = "";
    
    public string Title { get; set; } = "";
    
    public string Memory { get; set; } = "";

    [JsonPropertyName("title_slug")]
    public string TitleSlug { get; set; } = "";
    
    public string Code { get; set; } = "";
}