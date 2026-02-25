using Agent_Framework_3_Agent_as_Funciton_Tools.FunctionTools;
using Agent_Framework_3_Agent_as_Funciton_Tools.Models;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agent_Framework_3_Agent_as_Funciton_Tools.Agents
{
    public class NutritionAgent
    {
        [Description("Searches for high protein recipes optimized for strength training. " +
                     "Returns recipes with detailed nutrition information. " +
                     "Best for finding recipes for 5x5 training, muscle building, or post-workout meals.")]
        public static async Task<List<string>> SearchRecipesAsync([Description("Search term (e.g., 'chicken', 'beef', 'protein', 'breakfast')")] string searchTerm,
                                                 [Description("Minimum protein grams per serving (default 20)")] int minProtein = 20,
                                                 [Description("Maximum number of recipes to return (default 3)")] int maxResults = 3)
        {
            var service = new NutritionService();

            Console.WriteLine($"Searching for recipes with term '{searchTerm}' and minimum {minProtein}g protein...");

            var recipes = await service.SearchRecipesAsync(searchTerm);

            var suitableRecipes = new List<Recipe>();

            // Scrape each recipe for details
            foreach (var recipeUrl in recipes)
            {
                Console.WriteLine($"Scraping recipe details from {recipeUrl}...");
                var recipe = await service.ScrapeRecipeAsync(recipeUrl);

                if(suitableRecipes.Count >= maxResults)
                {
                    break;
                }

                if (recipe != null && recipe.Nutrition.ProteinGrams >= minProtein)
                {
                    Console.WriteLine($"Found suitable recipe: {recipe.Title} with {recipe.Nutrition.ProteinGrams}g protein.");
                    suitableRecipes.Add(recipe);
                }
            }

            return suitableRecipes.Select(r => JsonSerializer.Serialize(r)).ToList();
        }
    }
}
