using Agent_Framework_5_Memories.Models;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agent_Framework_5_Memories.MemoryComponents
{
    public class ClientDetailsMemory : AIContextProvider
    {
        
        private readonly IChatClient _chatClient;
        public ClientDetailsModels UserInfo { get; set; }


        public ClientDetailsMemory(IChatClient chatClient, JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions = null)
        {
            this._chatClient = chatClient;

            this.UserInfo = serializedState.ValueKind == JsonValueKind.Object ?
                serializedState.Deserialize<ClientDetailsModels>(jsonSerializerOptions)! :
                new ClientDetailsModels();
        }


        public override async ValueTask InvokedAsync(InvokedContext context, CancellationToken cancellationToken = default)
        {
            if ((this.UserInfo.CurrentWeight is null || this.UserInfo.DesiredWeight is null || this.UserInfo.Name is null)
                && context.RequestMessages.Any(x => x.Role == ChatRole.User))
            {
                Console.WriteLine();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[INVOKED] Attempting to extract missing user details...");
                Console.WriteLine($"[BEFORE EXTRACTION] Name: '{UserInfo.Name}'," +
                                  $"Current: '{UserInfo.CurrentWeight}'," +
                                  $"Desired: '{UserInfo.DesiredWeight}'");

                Console.ResetColor();

                var result = await this._chatClient.GetResponseAsync<ClientDetailsModels>(
                    context.RequestMessages,
                    new ChatOptions()
                    {
                        Instructions = "Extract the user's name, current weight and desired weight from the conversation. " +
                                       "Rules: " +
                                       "1. The FIRST weight mentioned is the current weight. " +
                                       "2. The SECOND weight mentioned is the desired weight. " +
                                       "3. If only one weight is mentioned, it's the current weight. " +
                                       "4. Return empty string or omit the property for values not found. DO NOT return the word 'null'."
                    },
                    cancellationToken: cancellationToken);

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[EXTRACTION RESULT] Name: '{result.Result.Name}', " +
                                  $"Current: '{result.Result.CurrentWeight}', " +
                                  $"Desired: '{result.Result.DesiredWeight}'");
                
                Console.ResetColor();

                // Sanitize: convert "null" string or empty to actual null
                this.UserInfo.CurrentWeight ??= SanitizeValue(result.Result.CurrentWeight);
                this.UserInfo.DesiredWeight ??= SanitizeValue(result.Result.DesiredWeight);
                this.UserInfo.Name ??= SanitizeValue(result.Result.Name);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[AFTER UPDATE] Name: '{UserInfo.Name}', Current: '{UserInfo.CurrentWeight}', Desired: '{UserInfo.DesiredWeight}'");
                Console.ResetColor();
            }
        }

        private static string? SanitizeValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return value.Trim();
        }


        public override ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
        {
            StringBuilder instructions = new();
            instructions
                .AppendLine(this.UserInfo.CurrentWeight is null ?
                            "Ask the user for their current weight and politely decline to answer any questions until they provide it." :
                            $"The user's current weight is {this.UserInfo.CurrentWeight}.")
                .AppendLine(this.UserInfo.DesiredWeight is null ?
                            "Ask the user for their desired weight and politely decline to answer any questions until they provide it." :
                            $"The user's desired weight is {this.UserInfo.DesiredWeight}.")
                .AppendLine(this.UserInfo.Name is null ?
                            "Ask the user for their name." :
                            $"The user's name is {this.UserInfo.Name}.");

            return new ValueTask<AIContext>(new AIContext
            {
                Instructions = instructions.ToString()
            });
        }


        //Serialization only happens when persistence is needed (e.g., saving to a database or file)
        public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
        {
            var serialized = JsonSerializer.SerializeToElement(this.UserInfo, jsonSerializerOptions);

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[SERIALIZE CALLED] Returning: {serialized}");
            Console.WriteLine($"[SERIALIZE] Name: '{UserInfo.Name}', Current: '{UserInfo.CurrentWeight}', Desired: '{UserInfo.DesiredWeight}'");
            Console.ResetColor();

            return serialized;
        }
    }
}