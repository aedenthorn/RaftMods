using System.Collections.Generic;
using System.Linq;

namespace CustomRecipes
{
    public class RecipeInfo
    {
        public CookingRecipeType recipeType;
        public uint recipeIndex;
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

        public CostMultiple ToCostMultiple(List<Item_Base> ___allAvailableItems)
        {
            var itemList = items.Select(n => ___allAvailableItems.FirstOrDefault((Item_Base i) => i.UniqueName == n));
            return new CostMultiple(itemList.ToArray(), amount);
        }
    }
}