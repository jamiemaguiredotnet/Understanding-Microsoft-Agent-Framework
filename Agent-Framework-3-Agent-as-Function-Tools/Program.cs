using Agent_Framework_3_Agent_as_Funciton_Tools.Agents;
using Agent_Framework_3_Agent_as_Funciton_Tools.FunctionTools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Agent_Framework_3_Agent_as_Funciton_Tools
{
    internal class Program
    {

        #region key and model id

        static string apiKey = "YOUR_OPENAI_API_KEY";
        static string model = "gpt-4o-mini";

        #endregion

        static async Task Main(string[] args)
        {
            AIAgent nutritionAgent = new OpenAIClient(apiKey)
                              .GetChatClient(model)
                              .CreateAIAgent(instructions: "You can fetch recipes and nutrition data.",
                                             name: "IronMind AI", null,
                                             tools: [AIFunctionFactory.Create(NutritionAgent.SearchRecipesAsync)]
                                            );


            AIAgent agent = new OpenAIClient(apiKey)
                    .GetChatClient(model)
                    .CreateAIAgent(instructions: "You are a personal trainer." +
                                                 "You can answer questions related to health, fitness and 5x5 stronglift. " +
                                                 "You shuld only use function tools in the PersonalTrainerAgent or NutritionAgent.",
                                   name: "IronMind AI", null,
                                   tools: [AIFunctionFactory.Create(PersonalTrainerAgent.GetNextAvailableDate),
                                          AIFunctionFactory.Create(PersonalTrainerAgent.CancelAppointment),
                                          AIFunctionFactory.Create(PersonalTrainerAgent.BookAppointment),
                                          AIFunctionFactory.Create(PersonalTrainerAgent.ListBookedAppointments),
                                          nutritionAgent.AsAIFunction()]
                                  );


            await RunChatLoopWithThreadAsync(agent);
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

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("IronMind AI: ");

                await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    if (completionUpdate.ContentUpdate.Count > 0)
                    {
                        Console.Write(completionUpdate.ContentUpdate[0].Text);
                    }
                }

                Console.ResetColor();
                Console.WriteLine();
            }

        }
    }
}
