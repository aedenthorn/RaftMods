using BepInEx;
using BepInEx.Configuration;
using FMODUnity;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace RepairItems
{
    [BepInPlugin("aedenthorn.RepairItems", "RepairItems", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> requireHammer;
        public static ConfigEntry<bool> atLeastOne;
        public static ConfigEntry<float> repairMatsMult;
        public static ConfigEntry<KeyCode> repairModKey;

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
            repairModKey = Config.Bind<KeyCode>("Options", "RepairModKey", KeyCode.LeftAlt, "Key to hold to repair on click");
            repairMatsMult = Config.Bind<float>("Options", "RepairMatsMult", 0.5f, "Fraction of recipe required to repair at full damage");
            requireHammer = Config.Bind<bool>("Options", "RequireHammer", true, "Must be holding hammer to repair");
            atLeastOne = Config.Bind<bool>("Options", "AtLeastOne", true, "Always require at least one of each material to repair");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
        }

        [HarmonyPatch(typeof(Slot), nameof(Slot.OnPointerDown))]
        public static class Slot_OnPointerDown_Patch
        {
            public static bool Prefix(Slot __instance)
            {
                if(!modEnabled.Value || !Input.GetKey(repairModKey.Value)) 
                    return true;
                RepairItem(__instance);
                return false;
            }

        }
        private static void RepairItem(Slot instance)
        {
            if (instance.itemInstance is null)
            {
                Dbgl("No item");
                return;
            }
            if (instance.itemInstance.HasMaxUses)
            {
                Dbgl("Has max uses");
                return;
            }
            if (!new CraftingCategory[] { CraftingCategory.Tools, CraftingCategory.Equipment, CraftingCategory.Weapons }.Contains(instance.itemInstance.settings_recipe.CraftingCategory))
            {
                Dbgl("Not repairable");
                return;
            }
            if (instance.itemInstance.settings_recipe.NewCost?.Length <= 0 || instance.itemInstance.settings_recipe.AmountToCraft > 1)
            {
                Dbgl("No recipe");
                return;
            }
            if (requireHammer.Value)
            {
                if (ComponentManager<PlayerInventory>.Value.GetSelectedHotbarItem().UniqueName != "Hammer")
                {
                    Dbgl("Not holding hammer");
                    return;

                }
            }
            if (repairMatsMult.Value > 0)
            {
                float fraction = (1 - instance.itemInstance.NormalizedUses) * repairMatsMult.Value;
                Dbgl($"Used {Mathf.RoundToInt((1 - instance.itemInstance.NormalizedUses) * 100)}%");
                Dbgl($"Mats required: {Mathf.RoundToInt(fraction * 100)}%");
                List<CostMultiple> costMultiples = new List<CostMultiple>();
                foreach(var cm in instance.itemInstance.settings_recipe.NewCost)
                {
                    int newAmount = Math.Max(atLeastOne.Value ? 1 : 0, Mathf.RoundToInt(cm.amount * fraction));
                    if (newAmount <= 0)
                        continue;
                    var newCost = new CostMultiple(cm.items, newAmount);
                    if (!newCost.HasEnoughInInventory(ComponentManager<PlayerInventory>.Value))
                    {
                        Dbgl($"Not enough {string.Join("/", cm.items.Select(i => i.UniqueName)) + " x" + newAmount}");
                        return;
                    }
                    Dbgl($"Enough {string.Join("/", cm.items.Select(i => i.UniqueName)) + " x" + newAmount}");
                    costMultiples.Add(newCost);
                }
                RemoveCostMultiple(ComponentManager<PlayerInventory>.Value, costMultiples.ToArray(), false);
            }
            instance.itemInstance.SetUsesToMax();
            instance.RefreshComponents();
            RuntimeManager.PlayOneShotSafe("event:/crafting/reinforce", ComponentManager<Raft_Network>.Value.GetLocalPlayer().transform.position);
        }
        public static void RemoveCostMultiple(PlayerInventory inventory, CostMultiple[] costMultiple, bool manipulateCostAmount = false)
        {
            InventoryPickup ip = AccessTools.FieldRefAccess<PlayerInventory, InventoryPickup>(inventory, "inventoryPickup");
            foreach (CostMultiple costMultiple2 in costMultiple)
            {
                int num = costMultiple2.amount;
                for (int j = 0; j < costMultiple2.items.Length; j++)
                {
                    int itemCount = inventory.GetItemCount(costMultiple2.items[j].UniqueName);
                    if (itemCount >= num)
                    {
                        inventory.RemoveItem(costMultiple2.items[j].UniqueName, num);
                        ip.ShowItem(costMultiple2.items[j].UniqueName, -num);
                        num = 0;
                        if (manipulateCostAmount)
                        {
                            costMultiple2.amount = 0;
                        }
                    }
                    else
                    {
                        inventory.RemoveItem(costMultiple2.items[j].UniqueName, itemCount);
                        ip.ShowItem(costMultiple2.items[j].UniqueName, -num);
                        num -= itemCount;
                        if (manipulateCostAmount)
                        {
                            costMultiple2.amount -= itemCount;
                        }
                    }
                    if (num <= 0)
                    {
                        break;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InventoryPickupMenuItem), "SetItem")]
        public static class InventoryPickupMenuItem_SetItem_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling InventoryPickupMenuItem.SetItem");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand is string str && str == "+")
                    {
                        Dbgl("adding method to check for negative amount");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.CheckNegative))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_2));
                        break;
                    }
                }


                return codes.AsEnumerable();
            }
        }

        private static string CheckNegative(string str, int amount)
        {
            return amount < 0 ? "" : str;
        }
    }
}
