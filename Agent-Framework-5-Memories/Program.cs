using Agent_Framework_3_Agent_as_Funciton_Tools.Agents;
using Agent_Framework_5_Memories.MemoryComponents;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Threading;


namespace Agent_Framework_5_Memories
{
    internal class Program
    {

        #region key and model id
        
        static string apiKey = "YOUR_OPENAI_API_KEY";
        static string model = "gpt-4o-mini";

        #endregion

        static async Task Main(string[] args)
        {
            // Create the OpenAI chat client
            ChatClient chatClient = new OpenAIClient(apiKey)
                .GetChatClient(model);

            // Create the agent with your custom memory component
            AIAgent agent = chatClient.CreateAIAgent(new ChatClientAgentOptions()
            {
                Name = "IronMind AI",
                ChatOptions = new()
                {
                    Instructions = "You are a personal trainer. " +
                                   "You can only answer questions related to health, fitness and 5x5 stronglift. " +
                                   "Use local function tools",
                    Tools = [AIFunctionFactory.Create(PersonalTrainerAgent.GetTimeToReachWeight)]
                },
                AIContextProviderFactory = ctx => new ClientDetailsMemory(
                    chatClient.AsIChatClient(),
                    ctx.SerializedState,
                    ctx.JsonSerializerOptions),
            });

            await RunChatLoopWithThreadAsync(agent);
        }

        private static async Task RunChatLoopWithThreadAsync(AIAgent agent)
        {
            AgentThread agentThread = agent.GetNewThread();

            while (true)
            {
                Console.Write("You: ");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;

                if(input.Equals("save"))
                {
                    // Explicitly serialize the thread
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("--- Testing Serialization ---");
                    var serializedThread = agentThread.Serialize();
                    Console.WriteLine($"Serialized thread: {serializedThread}");
                    Console.ResetColor();

                    Console.WriteLine();
                }

                UserChatMessage chatMessage = new(input);

                AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates =
                    agent.RunStreamingAsync([chatMessage], agentThread);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("IronMind AI: ");

                await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    if (completionUpdate.ContentUpdate.Count > 0)
                    {
                        Console.Write(completionUpdate.ContentUpdate[0].Text);
                    }
                }

                Console.WriteLine();
                Console.ResetColor();
                Console.WriteLine();

                // Access memory after streaming completes
                var memory = agentThread.GetService<ClientDetailsMemory>();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("--- Agent Memory Contents ---");
                Console.WriteLine($"Person Name: {memory?.UserInfo?.Name}");
                Console.WriteLine($"Current Weight: {memory?.UserInfo?.CurrentWeight}");
                Console.WriteLine($"Desired Weight: {memory?.UserInfo?.DesiredWeight}");
                Console.WriteLine("-----------------------------");
                Console.ResetColor();
                Console.WriteLine();
            }
        }

    }
}

