using System.Net;
using System.Text.Json;

namespace LeetcodeExporter;

public class Program
{
    private const string CookieName = "LEETCODE_SESSION";
    private const string Host = "leetcode.com";
    private static readonly TimeSpan DelayBetweenRequests = TimeSpan.FromSeconds(2);
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly List<Submission> allSubmissions = new();

    public static async Task Main() => await new Program().Run();
    
    private async Task Run()
    {
        await Download();
        await Save();
    }

    private async Task Download()
    {
        Console.WriteLine($"Enter the {CookieName} cookie value:");
        var cookieValue = Console.ReadLine();
        Console.WriteLine("Downloading...");

        var cookieContainer = new CookieContainer();
        cookieContainer.Add(new Cookie(CookieName, cookieValue) {
            Domain = $"{Host}"
        });
        using var client = new HttpClient(new HttpClientHandler {
            CookieContainer = cookieContainer
        });
        const int limit = 20;
        var offset = 0;
        SubmissionsResponse response;
        while (true) {
            Console.Write(".");
            await using var responseStream =
                await client.GetStreamAsync($"https://{Host}/api/submissions/?offset={offset}&limit={limit}");
            response = JsonSerializer.Deserialize<SubmissionsResponse>(responseStream, JsonSerializerOptions) ??
                       throw new InvalidOperationException("Invalid response.");
            allSubmissions.AddRange(response.Submissions);
            if (!response.HasNext) {
                break;
            }
            offset += limit;
            await Task.Delay(DelayBetweenRequests);
        }
        Console.WriteLine();        
    }

    private async Task Save()
    {
        var dir = $"leetcode-export-{DateTime.Now:yyyy-MM-dd}";
        Console.WriteLine($"Saving to ./{dir}/ ...");
        
        if (!Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        var acceptedSubmissions = allSubmissions
            .Where(s => s.Status == "Accepted")
            .GroupBy(s => s.TitleSlug)
            .Select(g => g.Select(s => new {
                Submission = s,
                RuntimeMillisecond = ParseMetric(s.Runtime, "ms"),
                MemoryMb = ParseMetric(s.Memory, "MB")
            }).OrderBy(s => s.RuntimeMillisecond).ThenBy(s => s.MemoryMb).Select(t => t.Submission).First());
        
        foreach (var submission in acceptedSubmissions) {
            await using var file = File.CreateText($"./{dir}/{submission.TitleSlug}.{GetExtension(submission)}");
            file.WriteLine($"// {submission.Title}");
            file.WriteLine($"// https://{Host}/problems/{submission.TitleSlug}");
            file.WriteLine($"// Date solved: {DateTimeOffset.FromUnixTimeSeconds(submission.Timestamp)}");
            file.WriteLine();
            file.WriteLine(submission.Code);
        }        
    }

    private string GetExtension(Submission submission)
    {
        var ext = submission.Lang;
        if (ext == "csharp") {
            ext = "cs";
        }
        return ext;
    }

    private decimal ParseMetric(string metric, string expectedMeasure)
    {
        var parts = metric.Split(' ');
        if (parts.Length != 2 || parts[1] != expectedMeasure) {
            throw new ArgumentException($"Error parsing metric value.", nameof(metric));
        }
        return decimal.Parse(parts[0]);
    }
}
