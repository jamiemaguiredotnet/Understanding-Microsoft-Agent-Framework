using Agent_Framework_8_AWeber_Newsletter.Agents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using System.Diagnostics;

#pragma warning disable OPENAI001 // OpenAI Responses API is in preview

namespace Agent_Framework_8_AWeber_Newsletter
{
    internal class Program
    {
        #region key and model id

        static string apiKey = "YOUR_OPENAI_API_KEY";
        static string model = "gpt-4o-mini";

        #endregion

        #region progress bar

        private static int _progressPosition = 0;
        private static readonly int _progressWidth = 30;

        private static void UpdateProgressBar(Stopwatch stopwatch)
        {
            // Create animated progress bar
            _progressPosition = (_progressPosition + 1) % (_progressWidth * 2);
            var barPosition = _progressPosition < _progressWidth ? _progressPosition : (_progressWidth * 2) - _progressPosition;

            var bar = new string(' ', _progressWidth);
            var barChars = bar.ToCharArray();

            // Create a 3-character moving block
            for (int i = 0; i < 3; i++)
            {
                var pos = (barPosition + i) % _progressWidth;
                if (pos < _progressWidth) barChars[pos] = '█';
            }

            // Get current status from agent
            var status = AINewsServiceAgent.CurrentStatus;
            if (status.Length > 45) status = status[..42] + "...";

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"\rResearching [{new string(barChars)}] {stopwatch.Elapsed:mm\\:ss}");
            Console.ResetColor();
            Console.Write($"\n\r{status,-50}");
            Console.SetCursorPosition(0, Console.CursorTop - 1);  // Move back up
        }

        private static void ClearProgressBar()
        {
            Console.Write($"\r{new string(' ', 60)}\r");
            Console.Write($"\n\r{new string(' ', 50)}\r");
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }

        #endregion

        static async Task Main(string[] args)
        {
            // Create the OpenAI Responses client (supports background responses)
            var responseClient = new OpenAIClient(apiKey)
                .GetOpenAIResponseClient(model);

            AIAgent agent = responseClient.CreateAIAgent(
                name: "AI News Digest Agent",
                instructions: """
                              You are an AI news research agent that creates weekly digest newsletters and sends them via AWeber.

                              Workflow:
                              1. Call SearchFoundryBlog() to get Microsoft AI Foundry updates
                              2. Call SearchAgentFrameworkUpdates() to get Microsoft Agent Framework code updates
                              3. Call SearchCompanyNews for each company: Microsoft, Google, Anthropic, OpenAI
                              4. Call SearchJamieMaguireBlog() to get the latest blog post from jamiemaguire.net
                              5. Call CreateAndSendNewsletter to send the newsletter via AWeber. You MUST call this function tool - do NOT output the newsletter as text. Pass the subject, markdownContent, and scheduleMinutesFromNow (use 0 for draft only). This step is mandatory.

                              CRITICAL: When generating the newsletter content for CreateAndSendNewsletter:
                              - You MUST include ALL headlines returned from ALL search functions
                              - For each news item, include: Headline, PublishedDate (if available), Summary, and Url
                              - IMPORTANT: Each headline MUST be paired with its MATCHING Url, Summary, and PublishedDate from the same result
                              - Do NOT mix up links - the Url for "Article A" must only appear with "Article A"
                              - Format URLs as markdown links: [Read more](Url)
                              - ALWAYS include the PublishedDate field when it is not empty
                              - Do NOT skip or omit any results - include everything returned

                              Newsletter format (use these exact section headers):
                              1. Overview (2-3 sentences about key themes)
                              2. "## Microsoft AI Foundry" section - ALL results from SearchFoundryBlog
                              3. "## Microsoft Agent Framework" section - ALL results from SearchAgentFrameworkUpdates. Only C# and .NET. Do not include Python code.
                              4. "## Microsoft" section - results from SearchCompanyNews("Microsoft")
                              5. "## Google" section - results from SearchCompanyNews("Google")
                              6. "## Anthropic" section - results from SearchCompanyNews("Anthropic")
                              7. "## OpenAI" section - results from SearchCompanyNews("OpenAI")
                              8. "## From the Editor" section - the latest post from SearchJamieMaguireBlog
                              9. "## What to Watch" section with emerging trends
                              10. "## Predictions for the Next 90 Days" section - REQUIRED: Based on the news gathered, list 3-5 specific predictions about what might happen in AI over the next 90 days. For EACH prediction, include a confidence score (Low/Medium/High) based on the strength of evidence from the news. Format each prediction as:
                                 - **Prediction:** [Your prediction] **Confidence:** [Low/Medium/High] - [Brief reasoning for the confidence level]

                              Each story format (IMPORTANT - include blank line after each item):

                              WITH DATE (when PublishedDate field is not empty):
                              **Headline** *(Jan 15, 2026)* - Summary [Read more](url)

                              WITHOUT DATE (when PublishedDate field is empty):
                              **Headline** - Summary [Read more](url)

                              RULES:
                              - Check each item's PublishedDate field - if it has a value like "Jan 15, 2026", include it in italics
                              - The blank line between items is CRITICAL for proper markdown rendering
                              - Copy the PublishedDate exactly as provided in the search results

                              Style:
                              - Professional, concise
                              - Include ALL source URLs as markdown links
                              - Do not invent or fabricate news - only use results from the search functions
                              - MUST have a blank line between each news item for proper formatting
                              """,
                tools: [
                    AIFunctionFactory.Create(AINewsServiceAgent.SearchFoundryBlog),
                    AIFunctionFactory.Create(AINewsServiceAgent.SearchAgentFrameworkUpdates),
                    AIFunctionFactory.Create(AINewsServiceAgent.SearchCompanyNews),
                    AIFunctionFactory.Create(AINewsServiceAgent.SearchJamieMaguireBlog),
                    AIFunctionFactory.Create(AINewsServiceAgent.CreateAndSendNewsletter)
                ]);

            AgentRunOptions options = new() { AllowBackgroundResponses = true };
            AgentThread thread = agent.GetNewThread();

            Console.WriteLine("=== AI News Digest Agent (AWeber Newsletter) ===");
            Console.WriteLine("Commands:");
            Console.WriteLine("  'Generate this week's digest'");
            Console.WriteLine("  'What's new from Anthropic?'");
            Console.WriteLine("  'Create a newsletter focused on AI agents'");
            Console.WriteLine();

            while (true)
            {
                Console.Write("You: ");
                string? input = Console.ReadLine();
                if (string.IsNullOrEmpty(input)) break;

                var stopwatch = Stopwatch.StartNew();
                _progressPosition = 0;
                AINewsServiceAgent.ResetStatus();

                // Initial call - may return with continuation token if still processing
                AgentRunResponse response = await agent.RunAsync(input, thread, options);

                // Poll with continuation token until complete (framework's background responses pattern)
                while (response.ContinuationToken is { } token)
                {
                    UpdateProgressBar(stopwatch);

                    await Task.Delay(TimeSpan.FromMilliseconds(200));

                    options.ContinuationToken = token;
                    response = await agent.RunAsync(thread, options);
                }

                stopwatch.Stop();

                ClearProgressBar();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Completed in {stopwatch.Elapsed:mm\\:ss}");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine($"Agent: {response.Text}");
                Console.WriteLine();

                options.ContinuationToken = null;
            }
        }
    }
}
