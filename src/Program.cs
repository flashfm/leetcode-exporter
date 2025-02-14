using System.Net;
using System.Text.Json;

namespace LeetcodeExporter;

public class Program
{
    private const string CookieName = "LEETCODE_SESSION";
    private const string Host = "leetcode.com";
    private const string dataFileName = "data.json";

    private static readonly TimeSpan DelayBetweenRequests = TimeSpan.FromSeconds(10);
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task Main()
        => await new Program().Run();

    private async Task Run()
    {
        await Download();
        await Extract();
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
        var submissions = new List<Submission>();
        while (true) {
            Console.WriteLine($"Offset {offset}");
            await using var responseStream =
                await client.GetStreamAsync($"https://{Host}/api/submissions/?offset={offset}&limit={limit}");
            var response = JsonSerializer.Deserialize<SubmissionsResponse>(responseStream, JsonSerializerOptions) ??
                       throw new InvalidOperationException("Invalid response.");
            submissions.AddRange(response.Submissions);
            if (!response.HasNext) {
                break;
            }
            offset += limit;
            await Task.Delay(DelayBetweenRequests);
        }
        Console.WriteLine($"Saving to {dataFileName}...");
        await using var dataFile = File.CreateText(dataFileName);
        await dataFile.WriteAsync(JsonSerializer.Serialize(submissions));
        Console.WriteLine();
    }

    private async Task Extract()
    {
        var allSubmissions = JsonSerializer.Deserialize<IReadOnlyList<Submission>>(await File.ReadAllTextAsync(dataFileName))
            ?? throw new InvalidOperationException("Error deserializing.");

        var dir = $"leetcode-export-{DateTime.Now:yyyy-MM-dd}";
        Console.WriteLine($"Saving to ./{dir}/ ...");

        if (!Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }

        var acceptedSubmissions = allSubmissions
            .Where(s => s.Status == "Accepted")
            .GroupBy(s => (s.QuestionId, s.Title))
            .Select(g => new {
                QuestionId = g.Key.QuestionId,
                Title = g.Key.Title,
                ProblemSubmissions = g
                    .OrderBy(s => s.Timestamp)
            });

        foreach (var submissionGroup in acceptedSubmissions) {
            var index = 1;
            foreach(var submission in submissionGroup.ProblemSubmissions) {
                var subdir = $"./{dir}/{submission.QuestionId} - {submission.Title}";
                Directory.CreateDirectory(subdir);
                var fileName = $"{subdir}/{index}.{GetExtension(submission)}";
                var date = DateTimeOffset.FromUnixTimeSeconds(submission.Timestamp);
                Console.WriteLine($"Saving to {fileName}");
                await using var file = File.CreateText(fileName);
                file.WriteLine($"// Copyright (c) {date.Year} Alexey Filatov");
                file.WriteLine($"// {submission.QuestionId} - {submission.Title} (https://{Host}/problems/{submission.TitleSlug})");
                file.WriteLine($"// Date solved: {date}");
                file.WriteLine($"// Memory: {submission.Memory}");
                file.WriteLine($"// Runtime: {submission.Runtime}");
                file.WriteLine($"");
                file.WriteLine();
                file.WriteLine(submission.Code);
                index++;
            }
        }
    }

    private static string GetExtension(Submission submission)
    {
        var lang = submission.Lang;
        return lang == "csharp" ? "cs" : lang; 
    }
}
