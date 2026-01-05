using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UltimateWater;
using UnityEngine;

namespace CustomRecipes
{
    [BepInPlugin("aedenthorn.CustomRecipes", "CustomRecipes", "0.2.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<KeyCode> reloadKey;

        public static void Dbgl(object obj, BepInEx.Logging.LogLevel level = BepInEx.Logging.LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(level, obj);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");
			reloadKey = Config.Bind<KeyCode>("Options", "ReloadKey", KeyCode.PageDown, "Key to reload recipes");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
        }

        public void Update()
        {
            if (Input.GetKeyDown(reloadKey.Value))
            {
                Dbgl("Reloading recipes");
                AccessTools.Field(typeof(CookingTable), "allRecipes").SetValue(null, null);
                AccessTools.Field(typeof(ItemManager), "allAvailableItems").SetValue(null, null);
                ItemManager.GetAllItems();
                ReloadCookingRecipes();
            }
        }


        public static void ReloadCookingRecipes()
        {
            SO_CookingTable_Recipe[] ___allRecipes = (SO_CookingTable_Recipe[]) AccessTools.Field(typeof(CookingTable), "allRecipes").GetValue(null);

            if (___allRecipes == null)
            {
                ___allRecipes = Resources.LoadAll<SO_CookingTable_Recipe>("SO_CookingRecipes");
                string assetDir = AedenthornUtils.GetAssetPath(context, true);
                string vanilla = Path.Combine(assetDir, "vanilla_cooking.json");
                if (!File.Exists(vanilla))
                {
                    List<RecipeInfo> infos = new List<RecipeInfo>();
                    for (int i = 0; i < ___allRecipes.Length; i++)
                    {
                        var r = ___allRecipes[i];
                        infos.Add(new RecipeInfo()
                        {
                            recipeType = r.RecipeType,
                            recipeIndex = (int)r.RecipeIndex,
                            result = r.Result.UniqueName,
                            isBuff = r.IsBuff,
                            portions = r.Portions,
                            cookTime = r.CookTime,
                            recipeCost = r.RecipeCost.Select(c => new RecipeCost()
                            {
                                items = c.items.Select(item => item.UniqueName),
                                amount = c.amount
                            }),
                        });
                    }
                    File.WriteAllText(vanilla, JsonSerializer.Serialize(infos, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
                }
                List<uint> indexes = ___allRecipes.Select(r => r.RecipeIndex).ToList();
                foreach (var custom in Directory.GetFiles(assetDir, "custom_cooking*.json", SearchOption.AllDirectories))
                {

                    try
                    {
                        List<RecipeInfo> customInfos = JsonSerializer.Deserialize<List<RecipeInfo>>(File.ReadAllText(custom), new JsonSerializerOptions() { IncludeFields = true });

                        foreach (var r in customInfos)
                        {
                            try
                            {
                                var recipe = (SO_CookingTable_Recipe)ScriptableObject.CreateInstance(typeof(SO_CookingTable_Recipe));
                                foreach (var f in r.GetType().GetFields())
                                {
                                    if (f.Name == "recipeCost")
                                    {
                                        var costList = (IEnumerable<RecipeCost>)f.GetValue(r);
                                        AccessTools.Field(typeof(SO_CookingTable_Recipe), f.Name)?.SetValue(recipe, costList.Select(c => c.ToCostMultiple((List<Item_Base>)AccessTools.Field(typeof(ItemManager), "allAvailableItems").GetValue(null))).ToArray());
                                    }
                                    else if (f.Name == "result")
                                    {
                                        AccessTools.Field(typeof(SO_CookingTable_Recipe), f.Name)?.SetValue(recipe, ItemManager.GetItemByName(r.result));
                                    }
                                    else if (f.Name == "recipeIndex")
                                    {
                                        int idx = (int)f.GetValue(r);
                                        if (idx < 0)
                                        {
                                            uint newIndex = 0;
                                            while (true)
                                            {
                                                if (!indexes.Contains(newIndex))
                                                {
                                                    indexes.Add(newIndex);
                                                    idx = (int)newIndex;
                                                    break;
                                                }
                                                newIndex++;
                                            }
                                        }
                                        AccessTools.Field(typeof(SO_CookingTable_Recipe), f.Name)?.SetValue(recipe, (uint)idx);

                                    }
                                    else
                                    {
                                        AccessTools.Field(typeof(SO_CookingTable_Recipe), f.Name)?.SetValue(recipe, f.GetValue(r));
                                    }
                                }
                                Dbgl($"New recipe for {recipe.Result.UniqueName}; valid {recipe.IsValid}, type {recipe.RecipeType}, cost: {recipe.RecipeCost.Select(m => m.items.Select(i => i?.UniqueName).Join(null, "/") + $" x{m.amount}").Join(null, ", ")}");
                                int index = ___allRecipes.ToList().FindIndex(cr => cr.RecipeIndex == r.recipeIndex);
                                if (index >= 0)
                                {
                                    recipe.name = ___allRecipes[index].name;
                                    ___allRecipes[index] = recipe;
                                    Dbgl($"Changed existing recipe {recipe.RecipeIndex}");
                                }
                                else
                                {
                                    recipe.name = $"SO_{recipe.RecipeIndex}_{recipe.Result.UniqueName}";
                                    ___allRecipes = ___allRecipes.Append(recipe).ToArray();
                                    Dbgl($"Added new recipe {recipe.RecipeIndex}");
                                }
                            }
                            catch (Exception e)
                            {
                                Dbgl($"Error importing recipe {r.recipeIndex} for {r.result}:\n\n\t" + e.StackTrace, BepInEx.Logging.LogLevel.Warning);
                            }

                        }
                    }

                    catch (Exception e)
                    {
                        Dbgl($"Error importing file {custom}:\n\n\t" + e.StackTrace, BepInEx.Logging.LogLevel.Warning);
                    }
                }
                Dbgl($"total recipes now {___allRecipes.Length}");
                AccessTools.Field(typeof(CookingTable), "allRecipes").SetValue(null, ___allRecipes);
            }
        }

        [HarmonyPatch(typeof(CookingTable), "RetrieveAllRecipes")]
        public static class CookingTable_RetrieveAllRecipes_Patch
        {
            public static bool Prefix()
            {
                if (!modEnabled.Value)
                    return true;
                ReloadCookingRecipes();
                return false;
            }
        }

        [HarmonyPatch(typeof(CookingTable), "OnSlotInsertItem")]
        public static class CookingTable_OnSlotInsertItem_Patch
        {
            public static void Postfix(CookingTable __instance, SO_CookingTable_Recipe[] ___allRecipes)
            {
                if (!modEnabled.Value)
                    return;
                CheckIngredients(__instance, ___allRecipes);
            }
        }

        [HarmonyPatch(typeof(CookingTable), "OnSlotPickupItem")]
        public static class CookingTable_OnSlotPickupItem_Patch
        {
            public static void Postfix(CookingTable __instance, SO_CookingTable_Recipe[] ___allRecipes)
            {
                if (!modEnabled.Value)
                    return;
                CheckIngredients(__instance, ___allRecipes);
            }
        }

        public static void CheckIngredients(CookingTable instance, SO_CookingTable_Recipe[] allRecipes)
        {
            var list = instance.Slots.Where(s => s.HasItem);
            Dbgl($"Current ingredients: {list.Select(s => s.CurrentItem.baseItem.UniqueName).Join(null, ", ")}, {allRecipes.Length} recipes");
            foreach (SO_CookingTable_Recipe r in allRecipes)
            {
                Dbgl($"Checking recipe for {r.Result.UniqueName}");
                if (r.DoesIngredientsCompleteRecipe(list.Select(s => s.CurrentItem.baseItem).ToArray()))
                {
                    Dbgl($"\tRecipe fulfilled; valid {r.IsValid}, type {r.RecipeType == instance.cookingType}");
                }
            }
        }

        [HarmonyPatch(typeof(ItemManager), "LoadAllItems")]
        public static class ItemManager_LoadAllItems_Patch
        {
            public static void Prefix(ref bool __state, List<Item_Base> ___allAvailableItems)
            {
                if(modEnabled.Value && (___allAvailableItems == null || ___allAvailableItems.Count == 0))
                    __state = true;
            }
            public static void Postfix(bool __state, List<Item_Base> ___allAvailableItems)
            {
                if(!__state) 
                    return;
                Dbgl($"adding recipes to loaded items");

                string assetDir = AedenthornUtils.GetAssetPath(context, true);
                string vanilla = Path.Combine(assetDir, "vanilla_crafting.json");
                if (!File.Exists(vanilla))
                {
                    Dictionary<string, CraftInfo> infos = new Dictionary<string, CraftInfo>();
                    foreach (var item in ___allAvailableItems)
                    {
                        if (item?.settings_recipe != null)
                        {
                            var r = item.settings_recipe;
                            infos[item.UniqueName] = new CraftInfo()
                            {

                                craftingCategory = r.CraftingCategory,
                                subCategory = r.SubCategory,
                                subCategoryOrder = r.SubCategoryOrder,
                                baseSkinItem = r.baseSkinItem?.UniqueName,
                                skins = r.Skins?.Select(i => i.UniqueName).ToArray(),

                                newCostToCraft = r.NewCost?.Select(c => new RecipeCost()
                                {
                                    items = c.items?.Select(i => i.UniqueName),
                                    amount = c.amount
                                }).ToArray(),

                                amountToCraft = r.AmountToCraft,
                                learned = r.Learned,
                                learnedFromBeginning = r.LearnedFromBeginning,
                                _hiddenInResearchTable = r.HiddenInResearchTable,
                                blueprintItem = r.BlueprintItem?.UniqueName,
                                extraBlueprintItems = r.ExtraBlueprintItems?.Select(i => i.UniqueName).ToArray(),
                                learnedViaBlueprint = r.LearnedViaBlueprint
                            };
                        }
                    }
                    File.WriteAllText(vanilla, JsonSerializer.Serialize(infos, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
                }
                bool changed = false;
                foreach (var custom in Directory.GetFiles(assetDir, "custom_crafting*.json", SearchOption.AllDirectories))
                {
                    try
                    {
                        Dictionary<string, CraftInfo> customInfos = JsonSerializer.Deserialize<Dictionary<string, CraftInfo>>(File.ReadAllText(custom), new JsonSerializerOptions() { IncludeFields = true });
                        foreach (var kvp in customInfos)
                        {
                            try
                            {
                                var r = kvp.Value;
                                var recipe = new ItemInstance_Recipe(r.craftingCategory, r.learned, r.learnedFromBeginning, r.subCategory, r.subCategoryOrder);

                                if (r.baseSkinItem != null)
                                    AccessTools.Field(typeof(ItemInstance_Recipe), "baseSkinItem").SetValue(recipe, ItemManager.GetItemByName(r.baseSkinItem));

                                if (r.skins != null)
                                    AccessTools.Field(typeof(ItemInstance_Recipe), "skins").SetValue(recipe, r.skins.Select(c => ItemManager.GetItemByName(c)).ToArray());

                                if (r.newCostToCraft != null)
                                    AccessTools.Field(typeof(ItemInstance_Recipe), "newCostToCraft").SetValue(recipe, r.newCostToCraft.Select(c => c.ToCostMultiple(___allAvailableItems)).ToArray());

                                AccessTools.Field(typeof(ItemInstance_Recipe), "amountToCraft").SetValue(recipe, r.amountToCraft);

                                AccessTools.Field(typeof(ItemInstance_Recipe), "_hiddenInResearchTable").SetValue(recipe, r._hiddenInResearchTable);

                                if (r.blueprintItem != null)
                                    AccessTools.Field(typeof(ItemInstance_Recipe), "blueprintItem").SetValue(recipe, ItemManager.GetItemByName(r.blueprintItem));

                                if (r.extraBlueprintItems != null)
                                    AccessTools.Field(typeof(ItemInstance_Recipe), "extraBlueprintItems").SetValue(recipe, r.extraBlueprintItems.Select(c => ItemManager.GetItemByName(c)).ToArray());

                                AccessTools.Field(typeof(ItemInstance_Recipe), "learnedViaBlueprint").SetValue(recipe, r.learnedViaBlueprint);
                                ___allAvailableItems.Find(i => i.UniqueName == kvp.Key).settings_recipe = recipe;
                                Dbgl($"Imported crafting recipe for {kvp.Key}");
                                changed = true;
                            }
                            catch (Exception e)
                            {
                                Dbgl($"Error importing recipe for {kvp.Key}:\n\n\t" + e.StackTrace, BepInEx.Logging.LogLevel.Warning);
                            }
                        }


                    }
                    catch (Exception e)
                    {
                        Dbgl($"Error importing file {custom}:\n\n\t" + e.StackTrace, BepInEx.Logging.LogLevel.Warning);
                    }
                }
                if (changed)
                {
                    var cm = ComponentManager<CraftingMenu>.Value;
                    if (cm != null)
                    {
                        Dbgl("Resetting crafting menu");

                        AccessTools.Field(typeof(CraftingMenu), "allRecipes").SetValue(cm, new Dictionary<CraftingCategory, List<RecipeItem>>());
                        AccessTools.Field(typeof(CraftingMenu), "recipeMenuItems").SetValue(cm, new List<RecipeMenuItem>());
                        while (cm.menuItemSortParent.transform.childCount > 0)
                        {
                            DestroyImmediate(cm.menuItemSortParent.transform.GetChild(0).gameObject);
                        }
                        AccessTools.Method(typeof(CraftingMenu), "Awake").Invoke(cm, new object[] { });
                    }
                }
            }
        }
    }
}
