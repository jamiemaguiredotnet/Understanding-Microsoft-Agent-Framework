# AI News Digest Agent

An AI-powered agent that automatically researches and generates weekly newsletter digests covering the latest developments in AI from major companies.

## Overview

This agent uses the Microsoft Agent Framework with **OpenAI Responses Agent** to:
- Search multiple news sources for AI-related content
- Monitor the Microsoft Foundry blog for platform updates
- Track the Microsoft Agent Framework GitHub repository for code changes
- Generate a formatted markdown newsletter with all findings

## Features

- **Multi-source news aggregation** - Pulls from RSS feeds, blogs, and GitHub
- **Dedicated sections** - Separate sections for Foundry, Agent Framework, and general company news
- **Smart filtering** - Filters articles by company-specific keywords
- **GitHub integration** - Tracks commits, releases, and example code updates
- **Markdown output** - Generates a formatted newsletter saved to disk
- **Background Responses** - Non-blocking execution with continuation token polling
- **Progress Bar** - Animated progress indicator with status updates
- **Predictions with Confidence Scores** - AI-generated predictions for the next 90 days

## Background Responses

This project demonstrates the **Background Responses** feature of the Microsoft Agent Framework using the **OpenAI Responses Agent**, which enables non-blocking agent execution for long-running tasks.

### Why Use OpenAI Responses Agent?

The OpenAI Responses Agent (via `GetOpenAIResponseClient()`) supports the continuation token pattern for background responses. This is different from the Chat-based agent (`GetChatClient()`) which executes synchronously.

Background responses allow:
- **Non-blocking execution** - The client can poll for updates while the agent works
- **Progress indication** - Show users real-time status updates
- **Responsive UI** - The application remains responsive during long operations

### How It Works

1. **Create the OpenAI Responses Agent**

   ```csharp
   #pragma warning disable OPENAI001 // OpenAI Responses API is in preview
   var responseClient = new OpenAIClient(apiKey)
       .GetOpenAIResponseClient(model);

   AIAgent agent = responseClient.CreateAIAgent(
       name: "AI News Digest Agent",
       instructions: "...",
       tools: [
           AIFunctionFactory.Create(MyTool.Method1),
           AIFunctionFactory.Create(MyTool.Method2)
       ]);
   ```

2. **Enable Background Responses**

   ```csharp
   AgentRunOptions options = new() { AllowBackgroundResponses = true };
   ```

3. **Poll with Continuation Token**

   When the agent is still processing, it returns a `ContinuationToken`. The client polls for completion:

   ```csharp
   AgentRunResponse response = await agent.RunAsync(input, thread, options);

   while (response.ContinuationToken is { } token)
   {
       UpdateProgressBar(stopwatch);  // Show progress
       await Task.Delay(TimeSpan.FromMilliseconds(200));

       options.ContinuationToken = token;
       response = await agent.RunAsync(thread, options);
   }
   ```

4. **Process Final Response**

   Once `ContinuationToken` is null, the agent has completed:

   ```csharp
   Console.WriteLine($"Agent: {response.Text}");
   options.ContinuationToken = null;  // Reset for next request
   ```

### Execution Flow

```
User Input
    │
    ▼
┌─────────────────────────────────────────────────────────┐
│  agent.RunAsync(input, thread, options)                 │
│  AllowBackgroundResponses = true                        │
│  Using: OpenAI Responses Agent                          │
└─────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────┐
│  Agent starts processing (returns ContinuationToken)    │
│  - Calls SearchFoundryBlog()                            │
│  - Calls SearchAgentFrameworkUpdates()                  │
│  - Calls SearchCompanyNews() for each company           │
└─────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────┐
│  Client polls with ContinuationToken                    │
│  while (response.ContinuationToken != null)             │
│  {                                                      │
│      UpdateProgressBar();                               │
│      await Task.Delay(200ms);                           │
│      response = await agent.RunAsync(thread, options);  │
│  }                                                      │
└─────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────┐
│  Agent completes (ContinuationToken = null)             │
│  - Calls GenerateNewsletter()                           │
│  - Returns final response text                          │
└─────────────────────────────────────────────────────────┘
    │
    ▼
Final Response to User
```

### Console Output During Execution

When the agent is working, users see an animated progress bar with elapsed time and current status:

```
You: Generate this week's digest
Researching [   ███                          ] 00:15
Searching Microsoft news...
```

The progress block animates back and forth while displaying:
- Elapsed time (mm:ss format)
- Current operation status

When complete:

```
Completed in 01:23

Agent: I've generated this week's AI news digest...
```

### Debug Output

Debug output is disabled by default to allow the progress bar to display cleanly. To enable verbose logging:

```csharp
AINewsServiceAgent.ShowDebugOutput = true;
```

This shows detailed information about each tool call:
- `[SearchCompanyNews]` - RSS feed searches
- `[SearchFoundryBlog]` - Blog parsing progress
- `[GitHub]` - GitHub API responses

## Data Sources

### RSS Feeds
- TechCrunch
- Wired
- Azure Blog

### Blog Pages
- Microsoft Foundry DevBlog (https://devblogs.microsoft.com/foundry/)

### GitHub Repositories
- microsoft/agent-framework - Tracks commits and releases from the last 7 days

## Prerequisites

- .NET 8.0 SDK
- OpenAI API key
- (Optional) GitHub Personal Access Token for higher rate limits

## Configuration

### OpenAI API Key

Update the `apiKey` in `Program.cs`:

```csharp
static string apiKey = "your-openai-api-key";
static string model = "gpt-4o-mini";
```

### GitHub Token (Optional)

To avoid GitHub API rate limits (60 requests/hour unauthenticated), set a GitHub Personal Access Token:

1. Create a token at https://github.com/settings/tokens
2. Select scope: `public_repo` (read-only access)
3. Update `_githubToken` in `AINewsServiceAgent.cs`:

```csharp
private static readonly string? _githubToken = "ghp_your_token_here";
```

## Running the Agent

```bash
cd Agent-Framework-6-Background-Responses
dotnet run
```

### Commands

Once running, you can use commands like:
- `Generate this week's digest` - Creates a full newsletter
- `What's new from Anthropic?` - Search specific company news
- `Create a newsletter focused on AI agents` - Themed digest

## Output

The agent generates a markdown file named `ai_digest_YYYY-MM-DD.md` with the following sections:

1. **Executive Summary** - Key themes and highlights
2. **Microsoft AI Foundry** - Latest Foundry blog posts
3. **Microsoft Agent Framework** - Recent GitHub commits and releases (C#/.NET only)
4. **Microsoft** - General Microsoft AI news
5. **Google** - Google AI news
6. **Anthropic** - Anthropic/Claude news
7. **OpenAI** - OpenAI/ChatGPT news
8. **What to Watch** - Emerging trends
9. **Predictions for the Next 90 Days** - Future predictions with confidence scores

### Predictions Format

Each prediction includes a confidence score based on evidence strength:

```markdown
- **Prediction:** [prediction text] **Confidence:** [Low/Medium/High] - [reasoning]
```

Example:
```markdown
- **Prediction:** Microsoft will release new Agent Framework features for multi-agent orchestration
  **Confidence:** High - Multiple recent commits indicate active development in this area
```

## Project Structure

```
Agent-Framework-6-Background-Responses/
├── Program.cs                 # Main entry point and agent configuration
├── Agents/
│   └── AINewsServiceAgent.cs  # News fetching and parsing logic
├── Models/
│   └── NewsItemModel.cs       # Data model for news items
└── README.md                  # This file
```

## Available Tools

The agent has access to these tools:

| Tool | Description |
|------|-------------|
| `SearchFoundryBlog()` | Fetches latest Microsoft Foundry blog posts |
| `SearchAgentFrameworkUpdates()` | Gets recent GitHub commits and releases |
| `SearchCompanyNews(company)` | Searches RSS feeds for company-specific news |
| `GenerateNewsletter(title, content)` | Saves the newsletter to a markdown file |

## Customization

### Adding New RSS Feeds

Edit the `_rssFeeds` array in `AINewsServiceAgent.cs`:

```csharp
private static readonly string[] _rssFeeds =
[
    "https://techcrunch.com/feed/",
    "https://www.wired.com/feed/category/business/latest/rss",
    "https://azure.microsoft.com/en-us/blog/feed/",
    "https://your-new-feed.com/rss",  // Add new feeds here
];
```

The parser supports both RSS 2.0 and Atom feed formats.

### Adding New Companies

Update the keywords dictionary in `SearchCompanyNews`:

```csharp
var keywords = company.ToLower() switch
{
    "newcompany" => new[] { "newcompany", "product1", "product2" },
    // ...
};
```

### Changing the Lookback Period

GitHub commits are fetched from the last 7 days. To change this, edit `FetchGitHubUpdates`:

```csharp
var since = DateTime.UtcNow.AddDays(-14); // Last 14 days
```

## Troubleshooting

### GitHub Rate Limit Exceeded

If you see `API rate limit exceeded`, either:
- Wait for the rate limit to reset (1 hour)
- Add a GitHub Personal Access Token (5,000 requests/hour)

### Progress Bar Not Showing

Ensure `ShowDebugOutput` is set to `false` (default). Debug output interferes with the progress bar display.

### Empty Newsletter Sections

Ensure the agent instructions in `Program.cs` include all search functions in the workflow.

## Dependencies

- `Microsoft.Agents.AI` - Agent framework
- `Microsoft.Agents.AI.OpenAI` - OpenAI integration
- `OpenAI` - OpenAI SDK
- `System.ClientModel` - Client model utilities

## References

- [OpenAI Responses Agents | Microsoft Learn](https://learn.microsoft.com/en-us/agent-framework/user-guide/agents/agent-types/openai-responses-agent)
- [Agent Background Responses | Microsoft Learn](https://learn.microsoft.com/en-us/agent-framework/user-guide/agents/agent-background-responses?pivots=programming-language-csharp)
- [Using function tools with an agent | Microsoft Learn](https://learn.microsoft.com/en-us/agent-framework/tutorials/agents/function-tools)
