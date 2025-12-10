using System.Collections.Generic;
using System.Linq;

namespace CustomRecipes
{
    public class CraftInfo
    {
        public CraftingCategory craftingCategory;
        public string subCategory;
        public int subCategoryOrder;
        public string baseSkinItem;
        public string[] skins;
        public RecipeCost[] newCostToCraft;
        public int amountToCraft;
        public bool learned;
        public bool learnedFromBeginning;
        public bool _hiddenInResearchTable;
        public string blueprintItem;
        public string[] extraBlueprintItems;
        public bool learnedViaBlueprint;

    }
}