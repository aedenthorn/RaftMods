using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ReuseBowlsAndGlasses
{
    [BepInPlugin("aedenthorn.ReuseBowlsAndGlasses", "Reuse Bowls And Glasses", "0.1.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;


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

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


        // cooking

        [HarmonyPatch(typeof(Slot), nameof(Slot.IncrementUses))]
        public static class Slot_IncrementUses_Patch
        {
            public static void Prefix(Slot __instance, int amountOfUsesToAdd, ref bool addItemAfterUseToInventory)
            {
                if (!modEnabled.Value || __instance.IsEmpty || amountOfUsesToAdd > 0 || __instance?.itemInstance?.settings_consumeable?.FoodType != FoodType.Food || (addItemAfterUseToInventory && __instance.itemInstance.settings_consumeable.ItemAfterUse?.item != null))
                {
                    return;
                }
                var allRecipes = AccessTools.StaticFieldRefAccess<CookingTable, SO_CookingTable_Recipe[]>("allRecipes");
                if (allRecipes == null)
                {
                    allRecipes = Resources.LoadAll<SO_CookingTable_Recipe>("SO_CookingRecipes");
                    AccessTools.StaticFieldRefAccess<CookingTable, SO_CookingTable_Recipe[]>("allRecipes") = allRecipes;
                }
                foreach (SO_CookingTable_Recipe recipe in allRecipes)
                {
                    if (recipe.IsValid && recipe.Result.UniqueIndex == __instance.itemInstance.UniqueIndex)
                    {
                        ComponentManager<Network_Player>.Value.Inventory.AddItem(recipe.RecipeType == CookingRecipeType.CookingPot ? "Claybowl_Empty" : "DrinkingGlass", 1);
                        return;
                    }
                }
            }
        }
    }
}
