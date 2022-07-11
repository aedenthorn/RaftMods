using BepInEx;
using BepInEx.Configuration;
using FMODUnity;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UltimateWater;
using UnityEngine;
using System.Linq;
using System.Reflection.Emit;

namespace CraftFromContainers
{
    [BepInPlugin("aedenthorn.CraftFromContainers", "Craft From Containers", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> range;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        } 
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

		[HarmonyPatch(typeof(BuildingUI_CostBox), nameof(BuildingUI_CostBox.SetAmountInInventory))]
		static class BuildingUI_CostBox_SetAmountInInventory_Patch
        {
            public static bool Prefix(BuildingUI_CostBox __instance, List<Item_Base> ___items, PlayerInventory inventory, bool includeSecondaryInventory)
            {
                if (!modEnabled.Value || GameModeValueManager.GetCurrentGameModeValue().playerSpecificVariables.unlimitedResources)
                    return true;
                int num = 0;
                for (int i = 0; i < ___items.Count; i++)
                {
                    if (___items[i] != null)
                    {
                        num += inventory.GetItemCount(___items[i].UniqueName);
                        foreach(Storage_Small s in StorageManager.allStorages)
                        {
                            num += s.GetInventoryReference().GetItemCount(___items[i].UniqueName);
                        }
                    }
                }
                __instance.SetAmount(num);
                return false;
            }
        }
		[HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveCostMultipleIncludeSecondaryInventories))]
		static class Inventory_RemoveCostMultipleIncludeSecondaryInventories_Patch
        {
            public static bool Prefix(Inventory __instance, CostMultiple[] costMultiple)
            {
                if (!modEnabled.Value || GameModeValueManager.GetCurrentGameModeValue().playerSpecificVariables.unlimitedResources)
                    return true;
                CostMultiple[] array = new CostMultiple[costMultiple.Length];
                for (int i = 0; i < costMultiple.Length; i++)
                {
                    array[i] = new CostMultiple(costMultiple[i].items, costMultiple[i].amount);
                }
                __instance.RemoveCostMultiple(array, true);
                foreach (Storage_Small s in StorageManager.allStorages)
                {
                    s.GetInventoryReference().RemoveCostMultiple(array, true);
                }
                return false;
            }
        }
		[HarmonyPatch(typeof(CostMultiple), nameof(CostMultiple.HasEnoughInInventory))]
		static class CostMultiple_HasEnoughInInventory_Patch
        {
            public static bool Prefix(CostMultiple __instance, Inventory inventory, ref bool __result)
            {
                if (!modEnabled.Value)
                    return true;
                int num = 0;
                for (int i = 0; i < __instance.items.Length; i++)
                {
                    if (__instance.items[i] != null)
                    {
                        num += inventory.GetItemCount(__instance.items[i].UniqueName);
                        foreach (Storage_Small s in StorageManager.allStorages)
                        {
                            num += s.GetInventoryReference().GetItemCount(__instance.items[i].UniqueName);
                        }
                    }
                }
                __result = (num >= __instance.amount);
                return false;
            }
        }
    }
}
