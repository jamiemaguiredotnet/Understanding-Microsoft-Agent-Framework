using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Agent_Framework_4_Human_in_the_Loop.Services
{
    public class PaymentsService
    {

          public async Task<string> ProcessPaymentAsync(
            string productName,
            decimal amount,
            string currency,
            string cardNumber,
            int expiryMonth,
            int expiryYear,
            string cvv,
            string cardholderName)
        {
            Console.WriteLine($"Processing payment for {productName} - {currency} {amount / 100m:F2}...");
            // Simulate payment processing
            await Task.Delay(1000);
            var transactionId = Guid.NewGuid().ToString();
            
            var result = new
            {
                success = true,
                transactionId = transactionId,
                product = productName,
                amount = amount / 100m,
                currency = currency,
                cardLast4 = cardNumber.Substring(Math.Max(0, cardNumber.Length - 4)),
                timestamp = DateTime.UtcNow
            };

            Console.WriteLine($"Payment successful! Transaction ID: {transactionId}");

            return JsonSerializer.Serialize(result);
        }

    }
}