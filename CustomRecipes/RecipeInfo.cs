using System.Collections.Generic;
using System.Linq;

namespace CustomRecipes
{
    public class RecipeInfo
    {
        public int index { get; set; } = -1;
        public CookingRecipeType recipeType;
        public string result;
        public bool isBuff;
        public uint portions;
        public float cookTime;
        public IEnumerable<RecipeCost> recipeCost;

    }

    public class RecipeCost
    {
        public IEnumerable<string> items;
        public int amount;

        public CostMultiple ToCostMultiple()
        {
            var itemList = items.Select(n => ItemManager.GetItemByName(n));
            return new CostMultiple(itemList.ToArray(), amount);
        }
    }
}