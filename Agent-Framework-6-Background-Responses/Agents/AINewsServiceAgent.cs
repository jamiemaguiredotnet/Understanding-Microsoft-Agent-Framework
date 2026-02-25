using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Agent_Framework_6_Background_Responses.Models;

namespace Agent_Framework_6_Background_Responses.Agents
{
    public class AINewsServiceAgent
    {
        private static readonly HttpClient _httpClient = new();
        private static readonly string _lastRunFile = "agent_last_run.json";

        #region api tokens

        private static readonly string? _githubToken = "YOUR_GITHUB_TOKEN";
        
        #endregion

        private static readonly string[] _rssFeeds =
        [
            "https://techcrunch.com/feed/",
            "https://www.wired.com/feed/category/business/latest/rss",
            "https://azure.microsoft.com/en-us/blog/feed/"
        ];

        private const string FoundryBlogUrl = "https://devblogs.microsoft.com/foundry/";

        /// <summary>
        /// Controls whether debug output is shown in console. Set to false to show progress bar instead.
        /// </summary>
        public static bool ShowDebugOutput { get; set; } = false;

        /// <summary>
        /// Current status message for progress display.
        /// </summary>
        public static string CurrentStatus { get; private set; } = "Starting...";

        /// <summary>
        /// Resets the status for a new run.
        /// </summary>
        public static void ResetStatus() => CurrentStatus = "Starting...";

        private static void DebugLog(string message, ConsoleColor? color = null)
        {
            if (!ShowDebugOutput) return;

            if (color.HasValue)
            {
                Console.ForegroundColor = color.Value;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine(message);
            }
        }

        #region function tools

        [Description("Searches for recent AI news and developments from a specific " +
                     "company. Returns items with: Headline, Summary, Url, Source, Company, and PublishedDate (format: 'Jan 15, 2026').")]
        public static async Task<List<NewsItemModel>> SearchCompanyNews([Description("The company to search for: Microsoft, Google, Anthropic, or OpenAI")] string company)
        {
            CurrentStatus = $"Searching {company} news...";
            DebugLog("");
            DebugLog($"[SearchCompanyNews] Starting search for: {company}", ConsoleColor.Cyan);

            var results = new List<NewsItemModel>();

            var keywords = company.ToLower() switch
            {
                "microsoft" => new[] { "microsoft", "copilot", "azure", "bing" },
                "google" => new[] { "google", "gemini", "deepmind", "bard" },
                "anthropic" => new[] { "anthropic", "claude" },
                "openai" => new[] { "openai", "chatgpt", "gpt-4", "gpt-5", "dall-e", "sora" },
                _ => new[] { company.ToLower(), "ai", "artificial intelligence" }
            };

            DebugLog($"[SearchCompanyNews] Keywords: {string.Join(", ", keywords)}");

            // Fetch from RSS feeds
            foreach (var feedUrl in _rssFeeds)
            {
                try
                {
                    DebugLog($"[SearchCompanyNews] Fetching RSS: {feedUrl}");

                    var feedContent = await _httpClient.GetStringAsync(feedUrl);

                    var feedResults = ParseRssFeed(feedContent, company, keywords, feedUrl);

                    DebugLog($"[SearchCompanyNews]   Found {feedResults.Count} matching items");
                    foreach (var item in feedResults)
                    {
                        DebugLog($"[SearchCompanyNews]     - {item.Headline}");
                    }

                    results.AddRange(feedResults);
                }
                catch (Exception ex)
                {
                    DebugLog($"[SearchCompanyNews] Warning: Failed to fetch {feedUrl}: {ex.Message}", ConsoleColor.Yellow);
                }
            }

            var finalResults = results.Take(5).ToList();

            DebugLog($"[SearchCompanyNews] Completed - returning {finalResults.Count} results for {company}:", ConsoleColor.Green);
            foreach (var r in finalResults)
            {
                DebugLog($"[SearchCompanyNews]   [{r.Source}] {r.Headline}");
                DebugLog($"[SearchCompanyNews]       URL: {r.Url}");
                DebugLog($"[SearchCompanyNews]       Date: {(string.IsNullOrEmpty(r.PublishedDate) ? "(none)" : r.PublishedDate)}");
            }

            return finalResults;
        }


        [Description("Searches the Microsoft Foundry blog for the latest AI Foundry updates, tutorials, and announcements." +
                     "Returns items with: Headline, Summary, Url, Source, and PublishedDate (format: 'Jan 15, 2026' or empty if unavailable).")]
        public static async Task<List<NewsItemModel>> SearchFoundryBlog()
        {
            CurrentStatus = "Fetching Microsoft Foundry blog...";

            DebugLog("");
            DebugLog($"[SearchFoundryBlog] Fetching Microsoft Foundry blog...", ConsoleColor.Cyan);

            var results = new List<NewsItemModel>();

            try
            {
                var htmlContent = await _httpClient.GetStringAsync(FoundryBlogUrl);
                var keywords = new[] { "" }; // Match all articles

                results = ParseBlogPage(htmlContent, "Microsoft", keywords, FoundryBlogUrl);

                DebugLog($"[SearchFoundryBlog] Completed - returning {results.Count} results:", ConsoleColor.Green);
                foreach (var r in results)
                {
                    DebugLog($"[SearchFoundryBlog]   {r.Headline}");
                    DebugLog($"[SearchFoundryBlog]       URL: {r.Url}");
                }
            }
            catch (Exception ex)
            {
                DebugLog($"[SearchFoundryBlog] ERROR: {ex.Message}", ConsoleColor.Red);
            }

            return results.Take(10).ToList();
        }


        [Description("Searches the Microsoft Agent Framework GitHub repository for recent code updates, new examples, and releases." +
                     "Returns items with: Headline, Summary, Url, Source, and PublishedDate (format: 'Jan 15, 2026').")]
        public static async Task<List<NewsItemModel>> SearchAgentFrameworkUpdates()
        {
            CurrentStatus = "Fetching GitHub Agent Framework updates...";
            DebugLog("");
            DebugLog($"[SearchAgentFrameworkUpdates] Fetching Microsoft Agent Framework updates...", ConsoleColor.Cyan);

            var results = await FetchGitHubUpdates("microsoft", "agent-framework", "Microsoft");

            DebugLog($"[SearchAgentFrameworkUpdates] Completed - returning {results.Count} results:", ConsoleColor.Green);
            foreach (var r in results)
            {
                DebugLog($"[SearchAgentFrameworkUpdates]   {r.Headline}");
                DebugLog($"[SearchAgentFrameworkUpdates]       URL: {r.Url}");
            }

            return results;
        }


        [Description("Generates a markdown newsletter digest and saves it to file. Call this after gathering news from all companies.")]
        public static string GenerateNewsletter([Description("The newsletter title")] string title,
                                                [Description("The full markdown content of the newsletter")] string markdownContent)
        {
            CurrentStatus = "Generating newsletter...";
            DebugLog("");
            DebugLog($"[GenerateNewsletter] Creating newsletter: {title}", ConsoleColor.Cyan);

            var filename = $"ai_digest_{DateTime.Now:yyyy-MM-dd}.md";

            DebugLog($"[GenerateNewsletter] Filename: {filename}");
            DebugLog($"[GenerateNewsletter] Content length: {markdownContent.Length} characters");
            DebugLog($"[GenerateNewsletter] Content preview:");
            DebugLog("--- START CONTENT ---");
            DebugLog(markdownContent.Length > 1000 ? markdownContent[..1000] + "..." : markdownContent);
            DebugLog("--- END CONTENT ---");

            var header = $"""
                                # {title}

                                **Generated:** {DateTime.Now:dddd, MMMM d, yyyy 'at' h:mm tt}

                                ---

                                """;

            var fullContent = header + markdownContent;
            File.WriteAllText(filename, fullContent);

            // Save the last run time for GitHub tracking
            SaveLastRunTime();

            DebugLog($"[GenerateNewsletter] Newsletter saved successfully", ConsoleColor.Green);

            return $"Newsletter saved to {filename}";
        }
        
        #endregion


        #region helpers

        private static List<NewsItemModel> ParseRssFeed(string feedContent, string company, string[] keywords, string feedUrl)
        {
            var results = new List<NewsItemModel>();
            var doc = XDocument.Parse(feedContent);
            XNamespace atom = "http://www.w3.org/2005/Atom";

            // Handle both RSS 2.0 and Atom feeds
            var items = doc.Descendants("item").ToList();
            var isAtomFeed = false;
            if (!items.Any())
            {
                items = doc.Descendants(atom + "entry").ToList();
                isAtomFeed = true;
            }

            foreach (var item in items)
            {
                var title = GetElementValue(item, "title") ?? "";
                var description = GetElementValue(item, "description")
                                ?? GetElementValue(item, "summary")
                                ?? GetElementValue(item, "content")
                                ?? "";

                // Extract link - Atom feeds use href attribute, RSS uses element text
                var link = "";
                if (isAtomFeed)
                {
                    // Atom format: <link href="url" rel="alternate"/> - prefer alternate link
                    var linkElement = item.Elements(atom + "link")
                        .FirstOrDefault(e => e.Attribute("rel")?.Value == "alternate")
                        ?? item.Elements(atom + "link").FirstOrDefault();
                    link = linkElement?.Attribute("href")?.Value ?? "";
                }
                else
                {
                    // RSS format: <link>url</link>
                    link = GetElementValue(item, "link") ?? "";
                }

                // Get published date (RSS uses pubDate, Atom uses published or updated)
                var pubDateStr = GetElementValue(item, "pubDate")
                                ?? GetElementValue(item, "published")
                                ?? GetElementValue(item, "updated")
                                ?? "";
                var publishedDate = FormatPublishedDate(pubDateStr);

                var searchText = $"{title} {description}".ToLower();

                if (keywords.Any(k => searchText.Contains(k)))
                {
                    var sourceName = feedUrl switch
                    {
                        var u when u.Contains("techcrunch") => "TechCrunch",
                        var u when u.Contains("wired") => "Wired",
                        var u when u.Contains("azure.microsoft.com") => "Azure Blog",
                        _ => "Unknown"
                    };

                    // Clean up description (remove HTML tags)
                    description = System.Text.RegularExpressions.Regex.Replace(description, "<[^>]+>", "");
                    if (description.Length > 300)
                        description = description[..300] + "...";

                    // Prepend date to summary if available
                    var summaryWithDate = !string.IsNullOrEmpty(publishedDate)
                        ? $"*({publishedDate})* {description}"
                        : description;

                    results.Add(new NewsItemModel(
                        Headline: title,
                        Summary: summaryWithDate,
                        Source: sourceName,
                        Url: link,
                        Company: company,
                        PublishedDate: publishedDate
                    ));
                }
            }

            return results;
        }

        private static string? GetElementValue(XElement item, string elementName)
        {
            // Try without namespace
            var element = item.Element(elementName);
            if (element != null) return element.Value;

            // Try with Atom namespace
            XNamespace atom = "http://www.w3.org/2005/Atom";
            element = item.Element(atom + elementName);
            return element?.Value;
        }

        private static string FormatPublishedDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return "";

            try
            {
                if (DateTime.TryParse(dateStr, out var date))
                {
                    return date.ToString("MMM d, yyyy");
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return "";
        }

        private static List<NewsItemModel> ParseBlogPage(string htmlContent, string company, string[] keywords, string blogUrl)
        {
            var results = new List<NewsItemModel>();

            var sourceName = blogUrl switch
            {
                var u when u.Contains("azure.microsoft.com") => "Azure Blog",
                var u when u.Contains("devblogs.microsoft.com/foundry") => "Microsoft Foundry",
                _ => "Microsoft Blog"
            };

            // For Microsoft Foundry blog, all content is AI-related, so include all articles
            var isFoundryBlog = blogUrl.Contains("devblogs.microsoft.com/foundry");

            DebugLog($"[ParseBlogPage] Parsing {sourceName}, isFoundryBlog={isFoundryBlog}");

            var foundUrls = new HashSet<string>();

            // Pattern for DevBlogs with class "single-click excerpt-title"
            var devBlogsPattern = new Regex(
                @"<a[^>]*class=[""'][^""']*(?:excerpt-title|single-click)[^""']*[""'][^>]*href=[""']([^""']+)[""'][^>]*>([^<]+)</a>",
                RegexOptions.IgnoreCase);

            // Also try href before class
            var devBlogsPattern2 = new Regex(
                @"<a[^>]*href=[""']([^""']+)[""'][^>]*class=[""'][^""']*(?:excerpt-title|single-click)[^""']*[""'][^>]*>([^<]+)</a>",
                RegexOptions.IgnoreCase);

            // Pattern for article links with titles - matches common blog patterns
            var articlePattern = new Regex(
                @"<a[^>]*href=[""']([^""']*(?:blog|post|article)[^""']*)[""'][^>]*>([^<]+)</a>",
                RegexOptions.IgnoreCase);

            // Also try matching h2/h3 tags with links inside (common blog pattern)
            var headingPattern = new Regex(
                @"<h[23][^>]*>.*?<a[^>]*href=[""']([^""']+)[""'][^>]*>([^<]+)</a>.*?</h[23]>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Try to find article cards/entries with title and description
            var cardPattern = new Regex(
                @"<article[^>]*>.*?<a[^>]*href=[""']([^""']+)[""'][^>]*>.*?<h[23][^>]*>([^<]+)</h[23]>.*?(?:<p[^>]*>([^<]*)</p>)?.*?</article>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Try DevBlogs specific patterns first
            foreach (var pattern in new[] { devBlogsPattern, devBlogsPattern2 })
            {
                DebugLog($"[ParseBlogPage] Trying DevBlogs pattern...");
                foreach (Match match in pattern.Matches(htmlContent))
                {
                    var url = match.Groups[1].Value;
                    var title = match.Groups[2].Value.Trim();

                    DebugLog($"[ParseBlogPage]   Found: {title}");

                    if (string.IsNullOrEmpty(title) || foundUrls.Contains(url)) continue;

                    var searchText = title.ToLower();
                    // For Foundry blog, include all articles; otherwise filter by keywords
                    if (isFoundryBlog || keywords.Any(k => searchText.Contains(k)))
                    {
                        foundUrls.Add(url);
                        if (!url.StartsWith("http"))
                            url = new Uri(new Uri(blogUrl), url).ToString();

                        results.Add(new NewsItemModel(
                            Headline: System.Net.WebUtility.HtmlDecode(title),
                            Summary: "",
                            Source: sourceName,
                            Url: url,
                            Company: company
                        ));
                    }
                }
            }

            // Try card pattern
            DebugLog($"[ParseBlogPage] Trying card pattern...");
            foreach (Match match in cardPattern.Matches(htmlContent))
            {
                var url = match.Groups[1].Value;
                var title = match.Groups[2].Value.Trim();
                var description = match.Groups.Count > 3 ? match.Groups[3].Value.Trim() : "";

                if (string.IsNullOrEmpty(title) || foundUrls.Contains(url)) continue;

                var searchText = $"{title} {description}".ToLower();
                if (isFoundryBlog || keywords.Any(k => searchText.Contains(k)))
                {
                    foundUrls.Add(url);
                    if (!url.StartsWith("http"))
                        url = new Uri(new Uri(blogUrl), url).ToString();

                    results.Add(new NewsItemModel(
                        Headline: System.Net.WebUtility.HtmlDecode(title),
                        Summary: System.Net.WebUtility.HtmlDecode(description),
                        Source: sourceName,
                        Url: url,
                        Company: company
                    ));
                }
            }

            // Try heading pattern
            DebugLog($"[ParseBlogPage] Trying heading pattern...");
            foreach (Match match in headingPattern.Matches(htmlContent))
            {
                var url = match.Groups[1].Value;
                var title = match.Groups[2].Value.Trim();

                if (string.IsNullOrEmpty(title) || foundUrls.Contains(url)) continue;

                var searchText = title.ToLower();
                if (isFoundryBlog || keywords.Any(k => searchText.Contains(k)))
                {
                    foundUrls.Add(url);
                    if (!url.StartsWith("http"))
                        url = new Uri(new Uri(blogUrl), url).ToString();

                    results.Add(new NewsItemModel(
                        Headline: System.Net.WebUtility.HtmlDecode(title),
                        Summary: "",
                        Source: sourceName,
                        Url: url,
                        Company: company
                    ));
                }
            }

            // Fallback to simpler article pattern
            DebugLog($"[ParseBlogPage] Trying article pattern...");
            foreach (Match match in articlePattern.Matches(htmlContent))
            {
                var url = match.Groups[1].Value;
                var title = match.Groups[2].Value.Trim();

                if (string.IsNullOrEmpty(title) || foundUrls.Contains(url) || title.Length < 10) continue;

                var searchText = title.ToLower();
                if (isFoundryBlog || keywords.Any(k => searchText.Contains(k)))
                {
                    foundUrls.Add(url);
                    if (!url.StartsWith("http"))
                        url = new Uri(new Uri(blogUrl), url).ToString();

                    results.Add(new NewsItemModel(
                        Headline: System.Net.WebUtility.HtmlDecode(title),
                        Summary: "",
                        Source: sourceName,
                        Url: url,
                        Company: company
                    ));
                }
            }

            DebugLog($"[ParseBlogPage] Total results found: {results.Count}");
            return results.Take(5).ToList();
        }

        private static async Task<List<NewsItemModel>> FetchGitHubUpdates(string owner, string repo, string company)
        {
            var results = new List<NewsItemModel>();

            // Always look at last 7 days to ensure we have content for the newsletter
            var since = DateTime.UtcNow.AddDays(-7);

            DebugLog($"[GitHub] Fetching commits from last 7 days (since {since:yyyy-MM-dd})...");

            // Set up HTTP client with GitHub API headers
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/repos/{owner}/{repo}/commits?since={since:yyyy-MM-ddTHH:mm:ssZ}&per_page=20");
            request.Headers.Add("User-Agent", "AI-News-Agent");
            request.Headers.Add("Accept", "application/vnd.github.v3+json");
            if (!string.IsNullOrEmpty(_githubToken))
            {
                request.Headers.Add("Authorization", $"Bearer {_githubToken}");
                DebugLog($"[GitHub] Using authenticated request");
            }
            else
            {
                DebugLog($"[GitHub] Warning: No GITHUB_TOKEN set - rate limits may apply (60 requests/hour)", ConsoleColor.Yellow);
            }
            var response = await _httpClient.SendAsync(request);
            DebugLog($"[GitHub] Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                DebugLog($"[GitHub] Error response: {errorContent}", ConsoleColor.Red);
                return results;
            }

            var commits = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (commits.ValueKind == JsonValueKind.Array)
            {
                DebugLog($"[GitHub] Found {commits.GetArrayLength()} commits");

                foreach (var commit in commits.EnumerateArray())
                {
                    var sha = commit.GetProperty("sha").GetString()?[..7] ?? "";
                    var message = commit.GetProperty("commit").GetProperty("message").GetString() ?? "";
                    var firstLine = message.Split('\n')[0];
                    var url = commit.GetProperty("html_url").GetString() ?? "";
                    var date = commit.GetProperty("commit").GetProperty("author").GetProperty("date").GetString() ?? "";

                    // Check if commit touches example/sample code
                    var isExampleCommit = await IsExampleRelatedCommit(firstLine, sha, owner, repo);

                    var publishedDate = FormatPublishedDate(date);

                    // Format summary with date
                    var datePrefix = !string.IsNullOrEmpty(publishedDate) ? $"*({publishedDate})* " : "";

                    if (isExampleCommit)
                    {
                        DebugLog($"[GitHub]   Example commit: {firstLine}");
                        results.Add(new NewsItemModel(
                            Headline: $"[Code] {firstLine}",
                            Summary: $"{datePrefix}New example/sample code in {owner}/{repo}. Commit: {sha}",
                            Source: "GitHub",
                            Url: url,
                            Company: company,
                            PublishedDate: publishedDate
                        ));
                    }
                    else
                    {
                        // Include significant commits (features, fixes)
                        var lowerMessage = firstLine.ToLower();
                        if (lowerMessage.Contains("feat") || lowerMessage.Contains("add") ||
                            lowerMessage.Contains("new") || lowerMessage.Contains("example") ||
                            lowerMessage.Contains("sample") || lowerMessage.Contains("demo"))
                        {
                            DebugLog($"[GitHub]   Feature commit: {firstLine}");
                            results.Add(new NewsItemModel(
                                Headline: $"[Code] {firstLine}",
                                Summary: $"{datePrefix}Update in {owner}/{repo}. Commit: {sha}",
                                Source: "GitHub",
                                Url: url,
                                Company: company,
                                PublishedDate: publishedDate
                            ));
                        }
                    }
                }
            }

            // Also check for new releases (last 7 days)
            await FetchGitHubReleases(owner, repo, company, since, results);

            return results;
        }

        private static async Task FetchGitHubReleases(string owner, string repo, string company, DateTime since, List<NewsItemModel> results)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    $"https://api.github.com/repos/{owner}/{repo}/releases?per_page=5");
                request.Headers.Add("User-Agent", "AI-News-Agent");
                request.Headers.Add("Accept", "application/vnd.github.v3+json");
                if (!string.IsNullOrEmpty(_githubToken))
                    request.Headers.Add("Authorization", $"Bearer {_githubToken}");

                DebugLog($"[GitHub] Fetching releases...");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode) return;

                var releases = await response.Content.ReadFromJsonAsync<JsonElement>();

                if (releases.ValueKind == JsonValueKind.Array)
                {
                    foreach (var release in releases.EnumerateArray())
                    {
                        var publishedAt = release.GetProperty("published_at").GetString();
                        if (string.IsNullOrEmpty(publishedAt)) continue;

                        var releaseDate = DateTime.Parse(publishedAt);
                        if (releaseDate <= since) continue;

                        var name = release.GetProperty("name").GetString() ?? release.GetProperty("tag_name").GetString() ?? "";
                        var body = release.GetProperty("body").GetString() ?? "";
                        var url = release.GetProperty("html_url").GetString() ?? "";

                        if (body.Length > 200) body = body[..200] + "...";

                        var releaseDateStr = releaseDate.ToString("MMM d, yyyy");
                        var summaryWithDate = $"*({releaseDateStr})* {body}";

                        DebugLog($"[GitHub]   New release: {name}");
                        results.Add(new NewsItemModel(
                            Headline: $"[Release] {owner}/{repo} - {name}",
                            Summary: summaryWithDate,
                            Source: "GitHub",
                            Url: url,
                            Company: company,
                            PublishedDate: releaseDateStr
                        ));
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog($"[GitHub] Warning: Failed to fetch releases: {ex.Message}", ConsoleColor.Yellow);
            }
        }

        private static async Task<bool> IsExampleRelatedCommit(string message, string sha, string owner, string repo)
        {
            var lowerMessage = message.ToLower();

            // Quick check based on commit message
            if (lowerMessage.Contains("example") || lowerMessage.Contains("sample") ||
                lowerMessage.Contains("demo") || lowerMessage.Contains("tutorial"))
            {
                return true;
            }

            // Check if commit touches example directories
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    $"https://api.github.com/repos/{owner}/{repo}/commits/{sha}");
                request.Headers.Add("User-Agent", "AI-News-Agent");
                request.Headers.Add("Accept", "application/vnd.github.v3+json");
                if (!string.IsNullOrEmpty(_githubToken))
                    request.Headers.Add("Authorization", $"Bearer {_githubToken}");

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return false;

                var commitDetails = await response.Content.ReadFromJsonAsync<JsonElement>();

                if (commitDetails.TryGetProperty("files", out var files) && files.ValueKind == JsonValueKind.Array)
                {
                    foreach (var file in files.EnumerateArray())
                    {
                        var filename = file.GetProperty("filename").GetString()?.ToLower() ?? "";
                        if (filename.Contains("example") || filename.Contains("sample") ||
                            filename.Contains("demo") || filename.Contains("tutorial") ||
                            filename.Contains("quickstart"))
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors when checking commit details
            }

            return false;
        }

        private static void SaveLastRunTime()
        {
            try
            {
                var data = new { lastRun = DateTime.UtcNow.ToString("o") };
                File.WriteAllText(_lastRunFile, JsonSerializer.Serialize(data));
                DebugLog($"[GitHub] Saved last run time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            }
            catch (Exception ex)
            {
                DebugLog($"[GitHub] Warning: Could not save last run time: {ex.Message}", ConsoleColor.Yellow);
            }
        }
        
        #endregion
    }
}
