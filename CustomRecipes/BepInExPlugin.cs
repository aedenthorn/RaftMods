using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using UnityEngine;

namespace CustomRecipes
{
    [BepInPlugin("aedenthorn.CustomRecipes", "CustomRecipes", "0.1.0")]
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
			reloadKey = Config.Bind<KeyCode>("Options", "ReloadKey", KeyCode.PageDown, "Enable debug");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
        }

        public void Update()
        {
            if (Input.GetKeyDown(reloadKey.Value))
            {
                AccessTools.Field(typeof(CookingTable), "allRecipes").SetValue(null, null);
                AccessTools.Field(typeof(ItemManager), "allAvailableItems").SetValue(null, null);
                ReloadCookingRecipes();
            }
        }


        public static void LoadAllItems()
        {
            List<Item_Base> ___allAvailableItems = (List<Item_Base>)AccessTools.Field(typeof(ItemManager), "allAvailableItems").GetValue(null);

            if (___allAvailableItems == null || ___allAvailableItems.Count == 0)
            {
                ___allAvailableItems = Resources.LoadAll<Item_Base>("IItems").ToList<Item_Base>();
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
                                items = c.items.Select(i => i.UniqueName),
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
                string assetDir = AedenthornUtils.GetAssetPath(context, true);
                string vanilla = Path.Combine(assetDir, "vanilla_crafting.json");
                string custom = Path.Combine(assetDir, "custom_crafting.json");
                if (!File.Exists(vanilla))
                {
                    File.WriteAllText(vanilla, JsonSerializer.Serialize(infos, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
                }
                if (!File.Exists(custom))
                {
                    File.WriteAllText(custom, "{}");
                }
                else
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
                                AccessTools.Field(typeof(ItemInstance_Recipe), "newCostToCraft").SetValue(recipe, r.newCostToCraft.Select(c => c.ToCostMultiple()).ToArray());

                            AccessTools.Field(typeof(ItemInstance_Recipe), "amountToCraft").SetValue(recipe, r.amountToCraft);

                            AccessTools.Field(typeof(ItemInstance_Recipe), "_hiddenInResearchTable").SetValue(recipe, r._hiddenInResearchTable);

                            if (r.blueprintItem != null)
                                AccessTools.Field(typeof(ItemInstance_Recipe), "blueprintItem").SetValue(recipe, ItemManager.GetItemByName(r.blueprintItem));

                            if (r.extraBlueprintItems != null)
                                AccessTools.Field(typeof(ItemInstance_Recipe), "extraBlueprintItems").SetValue(recipe, r.extraBlueprintItems.Select(c => ItemManager.GetItemByName(c)).ToArray());

                            AccessTools.Field(typeof(ItemInstance_Recipe), "learnedViaBlueprint").SetValue(recipe, r.learnedViaBlueprint);
                            ___allAvailableItems.Find(i => i.UniqueName == kvp.Key).settings_recipe = recipe;
                            Dbgl($"Imported crafting recipe for {kvp.Key}");

                        }
                        catch (Exception e)
                        {
                            Dbgl($"Error importing recipe for {kvp.Key}:\n\n\t" + e.StackTrace, BepInEx.Logging.LogLevel.Warning);
                        }

                    }
                }
            }
        }


        public static void ReloadCookingRecipes()
        {
            SO_CookingTable_Recipe[] ___allRecipes = (SO_CookingTable_Recipe[]) AccessTools.Field(typeof(CookingTable), "allRecipes").GetValue(null);

            if (___allRecipes == null)
            {
                ___allRecipes = Resources.LoadAll<SO_CookingTable_Recipe>("SO_CookingRecipes");
                List<RecipeInfo> infos = new List<RecipeInfo>();
                for (int i = 0; i < ___allRecipes.Length; i++)
                {
                    var r = ___allRecipes[i];
                    infos.Add(new RecipeInfo()
                    {
                        index = i,
                        recipeType = r.RecipeType,
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
                string assetDir = AedenthornUtils.GetAssetPath(context, true);
                string vanilla = Path.Combine(assetDir, "vanilla_cooking.json");
                string custom = Path.Combine(assetDir, "custom_cooking.json");
                if (!File.Exists(vanilla))
                {
                    File.WriteAllText(vanilla, JsonSerializer.Serialize(infos, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
                }
                if (!File.Exists(custom))
                {
                    File.WriteAllText(custom, "[]");
                }
                else
                {
                    List<RecipeInfo> customInfos = JsonSerializer.Deserialize<List<RecipeInfo>>(File.ReadAllText(custom), new JsonSerializerOptions() { IncludeFields = true });
                    foreach (var r in customInfos)
                    {
                        try
                        {
                            var recipe = new SO_CookingTable_Recipe();
                            foreach (var f in r.GetType().GetFields())
                            {
                                if (f.Name == "recipeCost")
                                {
                                    var costList = (IEnumerable<RecipeCost>)f.GetValue(r);
                                    AccessTools.Field(typeof(SO_CookingTable_Recipe), f.Name)?.SetValue(recipe, costList.Select(c => c.ToCostMultiple()).ToArray());
                                }
                                else if (f.Name == "result")
                                {
                                    AccessTools.Field(typeof(SO_CookingTable_Recipe), f.Name)?.SetValue(recipe, ItemManager.GetItemByName(r.result));
                                }
                                else
                                {
                                    AccessTools.Field(typeof(SO_CookingTable_Recipe), f.Name)?.SetValue(recipe, f.GetValue(r));
                                }
                            }
                            if (r.index >= 0 && r.index < ___allRecipes.Length)
                            {
                                ___allRecipes[r.index] = recipe;
                                Dbgl($"Changed recipe for {recipe.Result.UniqueName}");
                            }
                            else
                            {
                                ___allRecipes.Append(recipe);
                                Dbgl($"Added recipe for {recipe.Result.UniqueName}");
                            }
                        }
                        catch (Exception e)
                        {
                            Dbgl($"Error importing recipe {r.index} for {r.result}:\n\n\t" + e.StackTrace, BepInEx.Logging.LogLevel.Warning);
                        }

                    }
                }
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
        [HarmonyPatch(typeof(ItemManager), "LoadAllItems")]
        public static class ItemManager_LoadAllItems_Patch
        {
            public static bool Prefix()
            {
                if(!modEnabled.Value) 
                    return true;
                LoadAllItems();
                return false;
            }

        }
    }
}
