using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Agent_Framework_4_Human_in_the_Loop.Services;

namespace Agent_Framework_4_Human_in_the_Loop.Agents
{
    public class PaymentsAgent
    {

        [Description("You can collect payment details. You dont make a purchase.")]
        public static string CollectPaymentDetailsAsync([Description("Credit card number (16 digits)")] string cardNumber)
        {
            Console.WriteLine("Collecting payment details...");

            // return a string summarizing the payment details
            var result = $"Collected payment details using card ending in {cardNumber.Substring(Math.Max(0, cardNumber.Length - 4))}";

            return result;
        }


        [Description("Make the actual payment")]
        public static async Task<string> ProcessPaymentAsync()
        {
            PaymentsService service = new PaymentsService();

            // simulate payment details
            string productName = "Premium Subscription";
            decimal amount = 4999; // Amount in cents ($49.99)
            string currency = "USD";
            string cardNumber = "4111111111111111";
            int expiryMonth = 12;
            int expiryYear = 2025;
            string cvv = "123";
            string cardholderName = "John Doe";

            var result = await service.ProcessPaymentAsync(productName, amount, currency,
                                                           cardNumber, expiryMonth, expiryYear,
                                                           cvv, cardholderName);

            return result;
        }
    }
}
