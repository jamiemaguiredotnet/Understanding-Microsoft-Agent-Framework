using Agent_Framework_9_Basic_RAG.Agents;
using OpenAI;

#region Setup OpenAI Client and Agent
string apiKey = "YOUR_OPENAI_API_KEY";
string model = "gpt-4o-mini";
string embeddingModel = "text-embedding-3-large";
#endregion

OpenAIClient openAIClient = new(apiKey);

var ironMind = new IronMindRagAgent(openAIClient, embeddingModel);

var agent = await ironMind.CreateAgentAsync(openAIClient, model);
var session = await agent.CreateSessionAsync();

Console.WriteLine("Iron Mind AI - Personal Trainer");
Console.WriteLine("Ask me anything about training, nutrition, or recovery. Type 'exit' to quit.\n");

while (true)
{
    Console.Write("You: ");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }
        
    Console.WriteLine();
    Console.WriteLine(await agent.RunAsync(input, session));
    Console.WriteLine();
}
