using System.ClientModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

namespace Agent_Framework
{
    internal class Program
    {
        #region key and model id

        static string apiKey = "YOUR_OPENAI_API_KEY";
        static string model = "gpt-4o-mini";

        #endregion

        static async Task Main(string[] args)
        {
            AIAgent agent = new OpenAIClient(apiKey)
                          .GetChatClient(model)
                          .CreateAIAgent(instructions: "You are a personal trainer. " +
                                                       "You can only answer questions related to health, fitness and 5x5 stronglift.",
                                         name: "IronMind AI");

            await RunChatLoopWithThreadSerialiseandDeserializeAsync(agent);
        }


        private static async Task RunChatLoopWithoutThreadAsync(AIAgent agent)
        {
            while (true)
            {
                Console.Write("You: ");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                UserChatMessage chatMessage = new(input);

                AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates =
                    agent.RunStreamingAsync([chatMessage]);

                Console.Write("IronMind AI: ");

                await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    if (completionUpdate.ContentUpdate.Count > 0)
                    {
                        Console.Write(completionUpdate.ContentUpdate[0].Text);
                    }
                }

                Console.WriteLine();
            }
        }

        private static async Task RunChatLoopWithThreadAsync(AIAgent agent)
        {
            // Start a separate thread to handle user input
            AgentThread agentThread = agent.GetNewThread();
            
            while (true)
            {
                Console.Write("You: ");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                UserChatMessage chatMessage = new(input);

                AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates =
                    agent.RunStreamingAsync([chatMessage], agentThread);

                Console.Write("IronMind AI: ");

                await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    if (completionUpdate.ContentUpdate.Count > 0)
                    {
                        Console.Write(completionUpdate.ContentUpdate[0].Text);
                    }
                }

                Console.WriteLine();
            }

        }

        private static async Task RunChatLoopWithThreadSerialiseandDeserializeAsync(AIAgent agent)
        {
            // Start a separate thread to handle user input
            AgentThread agentThread = agent.GetNewThread();

            while (true)
            {
                Console.Write("You: ");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                UserChatMessage chatMessage = new(input);

                AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates =
                    agent.RunStreamingAsync([chatMessage], agentThread);

                Console.Write("IronMind AI: ");

                await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    if (completionUpdate.ContentUpdate.Count > 0)
                    {
                        Console.Write(completionUpdate.ContentUpdate[0].Text);
                    }
                }

                var serializedThreadElement = agentThread.Serialize();

                string serializedThread = JsonSerializer.Serialize(
                    serializedThreadElement,
                    new JsonSerializerOptions { WriteIndented = true });

                Console.WriteLine(serializedThread);

                var deserializedThreadElement = JsonSerializer.Deserialize<JsonElement>(serializedThread);
                AgentThread deserializedAgentThread = agent.DeserializeThread(deserializedThreadElement);

                var response = await agent.RunAsync("Hello, how are you?", deserializedAgentThread);

                Console.WriteLine();
            }

        }
    }
}