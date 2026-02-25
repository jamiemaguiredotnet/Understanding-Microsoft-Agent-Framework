using Agent_Framework_4_Human_in_the_Loop.Agents;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Net.NetworkInformation;
using System.Reflection;

#pragma warning disable MEAI001

namespace Agent_Framework_4_Human_in_the_Loop
{
    internal class Program
    {
        #region key and model id
        static string apiKey = "YOUR_OPENAI_API_KEY";
        static string model = "gpt-4o-mini";
        #endregion

        static async Task Main(string[] args)
        {
            // Wrap the function with ApprovalRequiredAIFunction
            AIFunction collectpaymentDetailsFunctionWithoutApproval = AIFunctionFactory.Create(PaymentsAgent.CollectPaymentDetailsAsync);
            AIFunction paymentFunctionWithApproval = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(PaymentsAgent.ProcessPaymentAsync));

            AIAgent paymentAgent = new OpenAIClient(apiKey)
                          .GetChatClient(model)
                          .CreateAIAgent(
                              instructions: "You are a helpful payment processing assistant.  You can only use local function tools",
                              name: "IronMind AI",
                              null,
                              tools: [collectpaymentDetailsFunctionWithoutApproval, paymentFunctionWithApproval]
                          );

            await RunChatLoopWithThreadAsync(paymentAgent);
        }

        private static async Task RunChatLoopWithThreadAsync(AIAgent agent)
        {
            AgentThread agentThread = agent.GetNewThread();

            Console.WriteLine("Payment Processing Agent (type 'exit' to quit)");
            Console.WriteLine("Try: 'Process a payment for me'\n");

            // outer chat loop for user input and agent responses
            while (true)
            {
                Console.Write("You: ");
                string? input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                // Run the agent with the user input
                var response = await agent.RunAsync(input, agentThread);
                var userInputRequests = response.UserInputRequests.ToList();

                // inner loop to handle approval requests (human-in-the-loop)
                // there may be multiple so we keep going until there are no more
                while (userInputRequests.Count > 0)
                {
                    var userInputResponses = userInputRequests
                     .OfType<FunctionApprovalRequestContent>()
                     .Select(functionApprovalRequest =>
                     {
                         Console.ForegroundColor = ConsoleColor.Yellow;
                         Console.WriteLine($"\nApproval Required");
                         Console.WriteLine($"Function: {functionApprovalRequest.FunctionCall.Name}");

                         // Display arguments properly
                         if (functionApprovalRequest.FunctionCall.Arguments is IDictionary<string, object> argsDict)
                         {
                             if (argsDict.Count > 0)
                             {
                                 Console.WriteLine("Arguments:");
                                 foreach (var arg in argsDict)
                                 {
                                     Console.WriteLine($"  {arg.Key}: {arg.Value}");
                                 }
                             }
                             else
                             {
                                 Console.WriteLine("Arguments: (none)");
                             }
                         }
                         else
                         {
                             Console.WriteLine($"Arguments: {functionApprovalRequest.FunctionCall.Arguments}");
                         }

                         Console.Write("\nApprove? (yes/no): ");
                         Console.ResetColor();

                         string? approval = Console.ReadLine();
                         bool isApproved = approval?.Trim().Equals("yes", StringComparison.OrdinalIgnoreCase) == true;

                         if (isApproved)
                         {
                             Console.ForegroundColor = ConsoleColor.Green;
                             Console.WriteLine("✓ Approved\n");
                             Console.ResetColor();
                         }
                         else
                         {
                             Console.ForegroundColor = ConsoleColor.Red;
                             Console.WriteLine("✗ Denied\n");
                             Console.ResetColor();
                         }

                         return new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, [functionApprovalRequest.CreateResponse(isApproved)]);
                     }).ToList();

                    // Pass the approval responses back to the agent
                    response = await agent.RunAsync(userInputResponses, agentThread);

                    // Check for any further approval requests in the new response
                    userInputRequests = response.UserInputRequests.ToList();
                }

                // Display the final response
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"IronMind AI: {response}\n");
                Console.ResetColor();
            }
        }
    }
}
