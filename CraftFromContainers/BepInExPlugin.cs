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
    [BepInPlugin("aedenthorn.CraftFromContainers", "Craft From Containers", "0.4.2")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> range;

        public static bool creatingBlock;
        public static Dictionary<int, ItemInstance> foodPickupItems = new Dictionary<int, ItemInstance>();

        public static void Dbgl(string str = "", BepInEx.Logging.LogLevel level = BepInEx.Logging.LogLevel.Debug, bool pref = false)
        {
            if (isDebug.Value)
                context.Logger.Log(level, (pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");
			range = Config.Bind<float>("General", "Range", -1, "Range in meters; set to negative for no range limit.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        public static List<Storage_Small> GetStorages()
        {
            List<Storage_Small> list = new List<Storage_Small>();
            foreach (Storage_Small s in StorageManager.allStorages)
            {
                if (range.Value >= 0 && Vector3.Distance(ComponentManager<Raft_Network>.Value.GetLocalPlayer().transform.position, s.transform.position) > range.Value)
                    continue;
                list.Add(s);
            }
            return list;
        }
        
        public static int GetAmount(Inventory inventory, List<Item_Base> ___items)
        {
            int num = 0;
            for (int i = 0; i < ___items.Count; i++)
            {
                if (___items[i] != null)
                {
                    num += inventory.GetItemCount(___items[i].UniqueName);
                    foreach (Storage_Small s in GetStorages())
                    {
                        num += s.GetInventoryReference().GetItemCount(___items[i].UniqueName);
                    }
                }
            }
            return num; 
        }
        
        
        public static void RemoveCostMultiple(Inventory __instance, CostMultiple[] costMultiple)
        {
            CostMultiple[] array = new CostMultiple[costMultiple.Length];
            for (int i = 0; i < costMultiple.Length; i++)
            {
                array[i] = new CostMultiple(costMultiple[i].items, costMultiple[i].amount);
            }
            __instance.RemoveCostMultiple(array, true);
            foreach(var  item in array)
            {
                Dbgl($"Remain {item.amount} {string.Join(", ", item.items.Select(i => i.UniqueName))}");
            }
            foreach (Storage_Small s in GetStorages())
            {
                s.GetInventoryReference().RemoveCostMultiple(array, true);
            }
        }

		[HarmonyPatch(typeof(BuildingUI_CostBox), nameof(BuildingUI_CostBox.SetAmountInInventory))]
		static class BuildingUI_CostBox_SetAmountInInventory_Patch
        {
            public static bool Prefix(BuildingUI_CostBox __instance, List<Item_Base> ___items, PlayerInventory inventory, bool includeSecondaryInventory)
            {
                if (!modEnabled.Value || GameModeValueManager.GetCurrentGameModeValue().playerSpecificVariables.unlimitedResources)
                    return true;
                int num = GetAmount(inventory, ___items);
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
		[HarmonyPatch(typeof(Hammer), nameof(Hammer.ReinforceBlock))]
		static class Hammer_ReinforceBlock_Patch
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
                Dbgl("Removing cost multiple");
                if (!modEnabled.Value || !creatingBlock)
                    return true;
                Dbgl("override");
                creatingBlock = false;
                RemoveCostMultiple(__instance, costMultiple);
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
                RemoveCostMultiple(__instance, costMultiple);
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
                int num = GetAmount(inventory, __instance.items.ToList());
                __result = (num >= __instance.amount);
                return false;
            }
        }

        // food
        
		[HarmonyPatch(typeof(CookingTable), "HandlePickupFood")]
		static class CookingTable_HandlePickupFood_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling CookingTable_HandlePickupFood");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand is MethodInfo && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(PlayerInventory), nameof(PlayerInventory.GetSelectedHotbarItem)))
                    {
                        Dbgl("adding method to check storages");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetPickupFoodItem))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }
        private static ItemInstance GetPickupFoodItem(ItemInstance item, CookingTable table)
        {
            if (!modEnabled.Value)
                return item;
            Item_Base pickupFoodItem = AccessTools.FieldRefAccess<CookingTable, Item_Base>(table, "pickupFoodItem");
            if (pickupFoodItem == null)
                return item;
            if (item != null && item.UniqueIndex == pickupFoodItem.UniqueIndex)
                return item;
            if(!foodPickupItems.ContainsKey(pickupFoodItem.UniqueIndex))
            {
                foodPickupItems[pickupFoodItem.UniqueIndex] = new ItemInstance(pickupFoodItem, 1, 1);
            }
            int inv = ComponentManager<Raft_Network>.Value.GetLocalPlayer().Inventory.GetItemCount(pickupFoodItem);
            if (inv > 0)
            {
                return foodPickupItems[pickupFoodItem.UniqueIndex];
            }
            foreach (var s in GetStorages())
            {
                int amount = s.GetInventoryReference().GetItemCount(pickupFoodItem.UniqueName);
                if (amount > 0)
                {
                    return foodPickupItems[pickupFoodItem.UniqueIndex];
                }
            }
            return item;
        }

        [HarmonyPatch(typeof(CookingTable), "PickupFood")]
        static class CookingTable_PickupFood_Patch
        {
            public static void Prefix(CookingTable __instance, Network_Player player, Item_Base ___pickupFoodItem, uint ___finishedPortions)
            {
                if (!modEnabled.Value || !player.IsLocalPlayer || ___finishedPortions == 0U || __instance.CurrentRecipe == null || ___pickupFoodItem == null || player.Inventory.GetSelectedHotbarItem()?.UniqueIndex == ___pickupFoodItem.UniqueIndex)
                    return;
                int inv = ComponentManager<Raft_Network>.Value.GetLocalPlayer().Inventory.GetItemCount(___pickupFoodItem);
                if (inv > 0)
                    return;
                foreach (var s in GetStorages())
                {
                    int amount = s.GetInventoryReference().GetItemCount(___pickupFoodItem.UniqueName);
                    if (amount > 0)
                    {
                        Dbgl($"Found food container {___pickupFoodItem.UniqueName} in storage");
                        s.GetInventoryReference().RemoveItem(___pickupFoodItem.UniqueName, 1);
                        return;
                    }
                        
                }
            }
        }



        // fueling

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
                    player.Inventory.RemoveItem(__instance.fuel.fuelItem.UniqueName, inv);
                }
                int remain = incrementAmount - inv;
                foreach (Storage_Small s in GetStorages())
                {
                    int amount = s.GetInventoryReference().GetItemCount(__instance.fuel.fuelItem.UniqueName);
                    if (amount > 0)
                    {
                        var remove = Math.Min(amount, remain);
                        s.GetInventoryReference().RemoveItem(__instance.fuel.fuelItem.UniqueName, remove);

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
        [HarmonyPatch(typeof(FuelNetwork), "AddFuel")]
        static class FuelNetwork_AddFuel_Patch
        {
            public static bool Prefix(FuelNetwork __instance, Network_Player player, int incrementAmount, ref bool __result)
            {
                if (!modEnabled.Value || player == null || incrementAmount <= 0 || __instance.Fuel.HasMaxFuel() || !player.IsLocalPlayer)
                    return true;

                int inv = player.Inventory.GetItemCount(__instance.Fuel.fuelItem);
                if (inv >= incrementAmount)
                    return true;
                if (inv > 0)
                {
                    player.Inventory.RemoveItem(__instance.Fuel.fuelItem.UniqueName, inv);
                }
                int remain = incrementAmount - inv;
                foreach (Storage_Small s in GetStorages())
                {
                    int amount = s.GetInventoryReference().GetItemCount(__instance.Fuel.fuelItem.UniqueName);
                    if (amount > 0)
                    {
                        var remove = Math.Min(amount, remain);
                        s.GetInventoryReference().RemoveItem(__instance.Fuel.fuelItem.UniqueName, remove);

                        remain -= remove;
                        if (remain <= 0)
                            break;
                    }
                }
                __instance.Fuel.AddFuel(incrementAmount);
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
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetFuelItemCount))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Fuel), nameof(Fuel.fuelItem))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Block_CookingStand), nameof(Block_CookingStand.fuel))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                    }
                }

                return codes.AsEnumerable();
            }
        }
        
        [HarmonyPatch(typeof(FuelNetwork), nameof(FuelNetwork.OnIsRayed))]
        static class FuelNetwork_OnIsRayed_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling FuelNetwork.OnIsRayed");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Callvirt && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(Inventory), nameof(Inventory.GetItemCount), new Type[] { typeof(Item_Base) }))
                    {
                        Dbgl("adding method to check storages");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetFuelItemCount))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Fuel), nameof(Fuel.fuelItem))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(FuelNetwork), "fuel")));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                    }
                }

                return codes.AsEnumerable();
            }
        }
        
        [HarmonyPatch(typeof(Tank), "HandleAddFuel")]
        static class Tank_HandleAddFuel_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling Tank_HandleAddFuel");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand is MethodInfo && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(Inventory), nameof(Inventory.GetItemCount), new Type[] { typeof(Item_Base) }))
                    {
                        Dbgl("adding method to check storages");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetFuelItemCount))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Tank), "defaultFuelToAdd")));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                        i += 3;
                    }
                }

                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(Tank), "ModifyTank")]
        static class Tank_ModifyTank_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling Tank.ModifyTank");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Callvirt && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(Inventory), nameof(Inventory.RemoveItemUses)))
                    {
                        Dbgl("adding method to take from storages");
                        codes[i].opcode = OpCodes.Call;
                        codes[i].operand = AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.RemoveItemUses));
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }

        public static void RemoveItemUses(Inventory inventory, string uniqueItemName, int usesToRemove, bool addItemAfterUseToInventory)
        {
            if (!modEnabled.Value)
            {
                inventory.RemoveItemUses(uniqueItemName, usesToRemove, addItemAfterUseToInventory);
                return;
            }
            if (usesToRemove == 0)
            {
                return;
            }
            Item_Base itemByName = ItemManager.GetItemByName(uniqueItemName);
            if (itemByName == null)
            {
                return;
            }
            Slot slot = null;
            var lpi = AccessTools.StaticFieldRefAccess<Inventory, PlayerInventory>("localPlayerInventory");
            foreach (Slot slot2 in inventory.allSlots)
            {
                if (usesToRemove > 0 && !slot2.IsEmpty && slot2.itemInstance.UniqueIndex == itemByName.UniqueIndex)
                {
                    slot = slot2;
                    if (slot2.itemInstance.UsesInStack >= usesToRemove)
                    {
                        slot2.IncrementUses(-usesToRemove, addItemAfterUseToInventory);
                        if (lpi.hotbar.IsSelectedHotSlot(slot2))
                        {
                            lpi.hotbar.ReselectCurrentSlot();
                        }
                        break;
                    }
                    usesToRemove -= slot2.itemInstance.UsesInStack;
                    slot2.IncrementUses(-slot2.itemInstance.UsesInStack, addItemAfterUseToInventory);
                    if (lpi.hotbar.IsSelectedHotSlot(slot2))
                    {
                        lpi.hotbar.ReselectCurrentSlot();
                    }
                }
            }
            if(usesToRemove > 0)
            {
                foreach (Storage_Small s in GetStorages())
                {
                    int amount = s.GetInventoryReference().GetItemCount(uniqueItemName);
                    if (amount > 0)
                    {
                        var remove = Math.Min(amount, usesToRemove);
                        s.GetInventoryReference().RemoveItem(uniqueItemName, remove);

                        usesToRemove -= remove;
                        if (usesToRemove <= 0)
                            break;
                    }
                }
            }
        }

        public static int GetFuelItemCount(int inv, Item_Base fuelItem)
        {
            if (!modEnabled.Value || inv != 0 || fuelItem is null)
                return inv;
            foreach (Storage_Small s in GetStorages())
            {
                if (s.GetInventoryReference().GetItemCount(fuelItem.UniqueName) > 0)
                    return 1;
            }
            return inv;
        }
    }
}
