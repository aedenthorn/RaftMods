using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace CraftFromContainers
{
    [BepInPlugin("aedenthorn.CraftFromContainers", "Craft From Containers", "0.2.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> range;
        public static bool creatingBlock;

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
		[HarmonyPatch(typeof(BlockCreator), nameof(BlockCreator.CreateBlock))]
		static class BlockCreator_CreateBlock_Patch
        {
            public static void Prefix()
            {
                if (!modEnabled.Value)
                    return;
                creatingBlock = true;
            }
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                creatingBlock = false;
            }
        }
		[HarmonyPatch(typeof(BuildingUI_Costbox_Sub_Crafting), nameof(BuildingUI_Costbox_Sub_Crafting.OnQuickCraft))]
		static class BuildingUI_Costbox_Sub_Crafting_OnQuickCraft_Patch
        {
            public static void Prefix()
            {
                if (!modEnabled.Value)
                    return;
                creatingBlock = true;
            }
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                creatingBlock = false;
            }
        }
		[HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveCostMultiple))]
		static class Inventory_RemoveCostMultiple_Patch
        {
            public static bool Prefix(Inventory __instance, CostMultiple[] costMultiple)
            {
                if (!modEnabled.Value || !creatingBlock)
                    return true;
                Dbgl("Removing cost multiple");
                creatingBlock = false;
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
		[HarmonyPatch(typeof(Block_CookingStand), nameof(Block_CookingStand.IncrementFuel))]
		static class Block_CookingStand_IncrementFuel_Patch
        {
            public static bool Prefix(Block_CookingStand __instance, int incrementAmount, Network_Player player, ref bool __result)
            {
                if (!modEnabled.Value || player == null || incrementAmount <= 0 || __instance.fuel.HasMaxFuel() || !player.IsLocalPlayer)
                    return true;

                int inv = player.Inventory.GetItemCount(__instance.fuel.fuelItem);
                if (inv >= incrementAmount)
                    return true;
                if(inv > 0)
                {
                    player.Inventory.RemoveItem(__instance.fuel.fuelItem.name, inv);
                }
                int remain = incrementAmount - inv;
                foreach (Storage_Small s in StorageManager.allStorages)
                {
                    int amount = s.GetInventoryReference().GetItemCount(__instance.fuel.fuelItem.UniqueName);
                    if (amount > 0)
                    {
                        var remove = Math.Min(amount, remain);
                        s.GetInventoryReference().RemoveItem(__instance.fuel.fuelItem.name, remove);

                        remain -= remove;
                        if (remain <= 0)
                            break;
                    }
                }
                __instance.fuel.AddFuel(incrementAmount);
                player.Animator.SetAnimation(PlayerAnimation.Trigger_Plant, true);
                __result = true;
                return false;
            }
        }
        [HarmonyPatch(typeof(Block_CookingStand), nameof(Block_CookingStand.OnIsRayed))]
        static class Block_CookingStand_OnIsRayed_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling Block_CookingStand.OnIsRayed");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Callvirt && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(Inventory), nameof(Inventory.GetItemCount), new Type[] { typeof(Item_Base) }))
                    {
                        Dbgl("adding method to check storages");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetItemCount))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Fuel), nameof(Fuel.fuelItem))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Block_CookingStand), nameof(Block_CookingStand.fuel))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                    }
                }

                return codes.AsEnumerable();
            }
        }

        private static int GetItemCount(int inv, Item_Base fuelItem)
        {
            if (!modEnabled.Value || inv != 0 || fuelItem is null)
                return inv;
            foreach (Storage_Small s in StorageManager.allStorages)
            {
                if (s.GetInventoryReference().GetItemCount(fuelItem.UniqueName) > 0)
                    return 1;
            }
            return inv;
        }
    }
}
