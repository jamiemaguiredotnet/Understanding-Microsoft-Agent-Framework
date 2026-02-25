using Agent_Framework_7_Existin_Agent_as_MCP_Tool.Models;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text.Json;

namespace Agent_Framework_7_Existin_Agent_as_MCP_Tool.Agent
{
    public class NutritionAgent
    {
        private static readonly List<Recipe> _stubRecipes =
        [
            new Recipe
            {
                Title = "Grilled Chicken Breast with Quinoa",
                Url = "https://example.com/grilled-chicken-quinoa",
                Ingredients = ["500g chicken breast", "200g quinoa", "1 lemon", "2 cloves garlic", "olive oil", "salt and pepper"],
                Instructions = ["1. Season chicken with garlic, lemon, salt and pepper.", "2. Grill chicken for 6-7 minutes per side.", "3. Cook quinoa according to package instructions.", "4. Serve chicken over quinoa."],
                Nutrition = new NutritionInfo { Calories = 450, ProteinGrams = 52, CarbsGrams = 35, FatGrams = 10 },
                PrepTime = "PT10M",
                CookTime = "PT20M",
                Servings = 2
            },
            new Recipe
            {
                Title = "Beef Stir Fry with Brown Rice",
                Url = "https://example.com/beef-stir-fry",
                Ingredients = ["400g lean beef strips", "300g brown rice", "1 red pepper", "1 broccoli head", "soy sauce", "ginger"],
                Instructions = ["1. Cook brown rice.", "2. Stir fry beef strips on high heat for 3 minutes.", "3. Add vegetables and cook for 5 minutes.", "4. Add soy sauce and ginger, serve over rice."],
                Nutrition = new NutritionInfo { Calories = 520, ProteinGrams = 45, CarbsGrams = 50, FatGrams = 14 },
                PrepTime = "PT15M",
                CookTime = "PT25M",
                Servings = 2
            },
            new Recipe
            {
                Title = "Salmon with Sweet Potato Mash",
                Url = "https://example.com/salmon-sweet-potato",
                Ingredients = ["2 salmon fillets", "3 sweet potatoes", "butter", "dill", "lemon", "salt"],
                Instructions = ["1. Boil sweet potatoes until tender, mash with butter.", "2. Season salmon with dill and lemon.", "3. Pan-sear salmon for 4 minutes per side.", "4. Serve salmon on sweet potato mash."],
                Nutrition = new NutritionInfo { Calories = 480, ProteinGrams = 38, CarbsGrams = 42, FatGrams = 18 },
                PrepTime = "PT10M",
                CookTime = "PT20M",
                Servings = 2
            },
            new Recipe
            {
                Title = "Protein Pancakes with Greek Yogurt",
                Url = "https://example.com/protein-pancakes",
                Ingredients = ["2 scoops whey protein", "2 eggs", "1 banana", "50g oats", "200g Greek yogurt", "blueberries"],
                Instructions = ["1. Blend oats, protein powder, eggs and banana.", "2. Cook pancakes on medium heat.", "3. Top with Greek yogurt and blueberries."],
                Nutrition = new NutritionInfo { Calories = 380, ProteinGrams = 42, CarbsGrams = 38, FatGrams = 8 },
                PrepTime = "PT5M",
                CookTime = "PT10M",
                Servings = 1
            },
            new Recipe
            {
                Title = "Turkey Meatballs with Pasta",
                Url = "https://example.com/turkey-meatballs",
                Ingredients = ["500g turkey mince", "200g wholemeal pasta", "1 egg", "breadcrumbs", "tomato sauce", "parmesan"],
                Instructions = ["1. Mix turkey mince, egg and breadcrumbs, form into meatballs.", "2. Bake meatballs at 200C for 20 minutes.", "3. Cook pasta, heat tomato sauce.", "4. Serve meatballs over pasta with sauce and parmesan."],
                Nutrition = new NutritionInfo { Calories = 510, ProteinGrams = 48, CarbsGrams = 45, FatGrams = 12 },
                PrepTime = "PT15M",
                CookTime = "PT25M",
                Servings = 2
            }
        ];

        [Description("Searches for high protein recipes optimized for strength training. " +
                     "Returns recipes with detailed nutrition information. " +
                     "Best for finding recipes for 5x5 training, muscle building, or post-workout meals.")]
        public static Task<List<string>> SearchRecipesAsync(
            [Description("Search term (e.g., 'chicken', 'beef', 'protein', 'breakfast')")] string searchTerm,
            [Description("Minimum protein grams per serving (default 20)")] int minProtein = 20,
            [Description("Maximum number of recipes to return (default 3)")] int maxResults = 3)
        {
            var results = _stubRecipes
                .Where(r => r.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            r.Ingredients.Any(i => i.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .Where(r => r.Nutrition != null && r.Nutrition.ProteinGrams >= minProtein)
                .Take(maxResults)
                .Select(r => JsonSerializer.Serialize(r))
                .ToList();

            return Task.FromResult(results);
        }
    }
}
