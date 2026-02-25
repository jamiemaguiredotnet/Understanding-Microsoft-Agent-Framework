namespace Agent_Framework_7_Existin_Agent_as_MCP_Tool.Models
{
    public class Recipe
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public List<string> Ingredients { get; set; } = new();
        public List<string> Instructions { get; set; } = new();
        public NutritionInfo? Nutrition { get; set; }
        public string PrepTime { get; set; } = string.Empty;
        public string CookTime { get; set; } = string.Empty;
        public int Servings { get; set; }
    }
}
