using Agent_Framework_7_Existin_Agent_as_MCP_Tool.Agent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;
using OpenAI;

#region key and model id

string apiKey = "YOUR_OPENAI_API_KEY";
string model = "gpt-4o-mini";

#endregion

// Create the nutrition agent using direct OpenAI
AIAgent nutritionAgent = new OpenAIClient(apiKey)
    .GetChatClient(model)
    .AsIChatClient()
    .AsAIAgent(
        instructions: "You can fetch recipes and nutrition data.",
        name: "NutritionAgent",
        description: "An agent that searches for high protein recipes and nutrition information.",
        tools: [AIFunctionFactory.Create(NutritionAgent.SearchRecipesAsync)]
    );

// Convert the agent to an AIFunction and then to an MCP tool.
// The agent name and description will be used as the MCP tool name and description.
McpServerTool tool = McpServerTool.Create(nutritionAgent.AsAIFunction());

// Register the MCP server with SSE transport and expose the tool via the server.
var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools([tool]);

var app = builder.Build();

app.MapMcp();

await app.RunAsync();
