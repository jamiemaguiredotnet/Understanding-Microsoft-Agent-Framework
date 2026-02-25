using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Agent_Framework_3_Agent_as_Funciton_Tools.Models;

namespace Agent_Framework_3_Agent_as_Funciton_Tools.FunctionTools
{
    public class NutritionService
    {
        private readonly HttpClient _httpClient;
        private readonly HtmlWeb _htmlWeb;
        private readonly int _delayMs;

        public NutritionService(int delayMs = 1500)
        {
            _delayMs = delayMs;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) RecipeBot/1.0");
            _htmlWeb = new HtmlWeb();
        }

        /// <summary>
        /// Searches Jamie Oliver's site and returns recipe URLs
        /// </summary>
        public async Task<List<string>> SearchRecipesAsync(string searchTerm)
        {
            var urls = new List<string>();
            var searchUrl = $"https://www.jamieoliver.com/search/{Uri.EscapeDataString(searchTerm)}";

            await Task.Delay(_delayMs);
            var doc = await _htmlWeb.LoadFromWebAsync(searchUrl);

            var linkNodes = doc.DocumentNode.SelectNodes("//a[contains(@href,'/recipes/')]");

            if (linkNodes != null)
            {
                foreach (var linkNode in linkNodes)
                {
                    var href = linkNode.GetAttributeValue("href", "");
                    if (!string.IsNullOrWhiteSpace(href))
                    {
                        if (href.StartsWith("/"))
                            href = $"https://www.jamieoliver.com{href}";

                        if (IsRecipeUrl(href) && !urls.Contains(href))
                            urls.Add(href);
                    }
                }
            }

            Console.WriteLine($"Found {urls.Count} recipe URLs for search term '{searchTerm}'.");

            return urls;
        }

        /// <summary>
        /// Scrapes a single recipe from URL
        /// </summary>
        public async Task<Recipe?> ScrapeRecipeAsync(string url)
        {
            await Task.Delay(_delayMs);
            var doc = await _htmlWeb.LoadFromWebAsync(url);

            // Try JSON-LD first, fallback to HTML
            return ParseJsonLdRecipe(doc, url) ?? ParseHtmlRecipe(doc, url);
        }

        /// <summary>
        /// Filters recipes by minimum protein content
        /// </summary>
        public List<Recipe> FilterByProtein(List<Recipe> recipes, int minProteinGrams)
        {
            return recipes
                .Where(r => r.Nutrition != null && r.Nutrition.ProteinGrams >= minProteinGrams)
                .OrderByDescending(r => r.Nutrition!.ProteinGrams)
                .ToList();
        }

        #region Parsing Helpers

        private Recipe? ParseJsonLdRecipe(HtmlDocument doc, string url)
        {
            var scriptNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
            if (scriptNodes == null) return null;

            foreach (var scriptNode in scriptNodes)
            {
                var json = scriptNode.InnerText;
                if (!json.Contains("\"@type\":\"Recipe\"")) continue;

                try
                {
                    using var jsonDoc = JsonDocument.Parse(json);
                    var root = jsonDoc.RootElement;
                    var recipeElement = root.ValueKind == JsonValueKind.Array ? root[0] : root;

                    var recipe = new Recipe
                    {
                        Url = url,
                        Title = GetJsonString(recipeElement, "name"),
                        PrepTime = GetJsonString(recipeElement, "prepTime"),
                        CookTime = GetJsonString(recipeElement, "cookTime")
                    };

                    // Servings
                    if (recipeElement.TryGetProperty("recipeYield", out var yieldElement))
                    {
                        recipe.Servings = yieldElement.ValueKind == JsonValueKind.Number
                            ? yieldElement.GetInt32()
                            : ExtractNumber(yieldElement.GetString() ?? "");
                    }

                    // Ingredients
                    if (recipeElement.TryGetProperty("recipeIngredient", out var ingredients))
                    {
                        foreach (var ingredient in ingredients.EnumerateArray())
                            recipe.Ingredients.Add(ingredient.GetString() ?? "");
                    }

                    // Instructions
                    if (recipeElement.TryGetProperty("recipeInstructions", out var instructions))
                    {
                        int step = 1;
                        foreach (var instruction in instructions.EnumerateArray())
                        {
                            string text = instruction.ValueKind == JsonValueKind.Object
                                ? GetJsonString(instruction, "text")
                                : instruction.GetString() ?? "";

                            if (!string.IsNullOrWhiteSpace(text))
                                recipe.Instructions.Add($"{step++}. {text}");
                        }
                    }

                    // Nutrition
                    if (recipeElement.TryGetProperty("nutrition", out var nutrition))
                    {
                        recipe.Nutrition = new NutritionInfo
                        {
                            Calories = ExtractNumber(GetJsonString(nutrition, "calories")),
                            ProteinGrams = ExtractNumber(GetJsonString(nutrition, "proteinContent")),
                            CarbsGrams = ExtractNumber(GetJsonString(nutrition, "carbohydrateContent")),
                            FatGrams = ExtractNumber(GetJsonString(nutrition, "fatContent"))
                        };
                    }

                    return recipe;
                }
                catch (JsonException)
                {
                    continue;
                }
            }

            return null;
        }

        private Recipe? ParseHtmlRecipe(HtmlDocument doc, string url)
        {
            var recipe = new Recipe { Url = url };

            // Title
            var titleNode = doc.DocumentNode.SelectSingleNode("//h1");
            recipe.Title = CleanText(titleNode?.InnerText ?? "Unknown Recipe");

            // Ingredients
            var ingredientNodes = doc.DocumentNode.SelectNodes(
                "//ul[contains(@class,'ingred')]//li | //div[contains(@class,'ingredient')]//li");

            if (ingredientNodes != null)
            {
                foreach (var node in ingredientNodes)
                {
                    var ingredient = CleanText(node.InnerText);
                    if (!string.IsNullOrWhiteSpace(ingredient))
                        recipe.Ingredients.Add(ingredient);
                }
            }

            // Instructions
            var instructionNodes = doc.DocumentNode.SelectNodes(
                "//ol[contains(@class,'method')]//li | //div[contains(@class,'method')]//p");

            if (instructionNodes != null)
            {
                int step = 1;
                foreach (var node in instructionNodes)
                {
                    var instruction = CleanText(node.InnerText);
                    if (!string.IsNullOrWhiteSpace(instruction) && instruction.Length > 20)
                        recipe.Instructions.Add($"{step++}. {instruction}");
                }
            }

            return recipe.Ingredients.Any() ? recipe : null;
        }

        private bool IsRecipeUrl(string url) =>
            url.Contains("/recipes/") &&
            !url.Contains("/recipes/category") &&
            !url.Contains("/recipes/course") &&
            !url.EndsWith("/recipes/");

        private string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            text = HtmlEntity.DeEntitize(text);
            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }

        private string GetJsonString(JsonElement element, string propertyName) =>
            element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? "" : "";

        private int ExtractNumber(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;
            var match = Regex.Match(text, @"\d+");
            return match.Success && int.TryParse(match.Value, out var number) ? number : 0;
        }

        #endregion
    }
}