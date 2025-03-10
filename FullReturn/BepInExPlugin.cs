﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace FullReturn
{
    [BepInPlugin("aedenthorn.FullReturn", "Full Return", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> returnFortification;
        public static ConfigEntry<float> returnPercent;

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
			returnFortification = Config.Bind<bool>("Options", "ReturnFortification", true, "Return fortificiation costs");
			returnPercent = Config.Bind<float>("Options", "ReturnPercent", 1f, "Decimal portion to return");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
		[HarmonyPatch(typeof(RemovePlaceables), nameof(RemovePlaceables.ReturnItemsFromBlock))]
        public static class RemovePlaceables_ReturnItemsFromBlock_Patch
        {
            public static void Postfix(Block block, Network_Player player, bool giveItems)
            {
                if (!modEnabled.Value || !giveItems || !block.Reinforced || !returnFortification.Value || GameModeValueManager.GetCurrentGameModeValue().playerSpecificVariables.unlimitedResources)
                    return;
                var item = ItemManager.GetAllItems().FirstOrDefault(i => i.UniqueName.Equals("Block_FoundationArmor"));
                if (item is null)
                    return;
                foreach (CostMultiple costMultiple in item.settings_recipe.NewCost)
                {
                    player.Inventory.AddItem(costMultiple.items[0].UniqueName, Mathf.CeilToInt(costMultiple.amount * returnPercent.Value));
                }
            }
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling RemovePlaceables.ReturnItemsFromBlock");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.5f)
                    {
                        Dbgl("replacing 0.5 with method");
                        codes.Insert(i+1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetPortion))));
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }

        public static float GetPortion(float value)
        {
            if (!modEnabled.Value)
                return value;
            return returnPercent.Value;
        }
    }
}
