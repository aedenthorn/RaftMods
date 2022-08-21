using BepInEx;
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
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> returnFortification;
        public static ConfigEntry<float> returnPercent;

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
			returnFortification = Config.Bind<bool>("Options", "ReturnFortification", true, "Return fortificiation costs");
			returnPercent = Config.Bind<float>("Options", "ReturnPercent", 1f, "Decimal portion to return");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
		[HarmonyPatch(typeof(RemovePlaceables), nameof(RemovePlaceables.ReturnItemsFromBlock))]
		static class RemovePlaceables_ReturnItemsFromBlock_Patch
        {
            public static void Prefix(Block block, Network_Player player, bool giveItems)
            {
                if (!modEnabled.Value || !giveItems || !block.Reinforced || GameModeValueManager.GetCurrentGameModeValue().playerSpecificVariables.unlimitedResources)
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
                        codes[i].opcode = OpCodes.Call;
                        codes[i].operand = AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetPortion));
                    }
                }

                return codes.AsEnumerable();
            }
        }

        private static float GetPortion()
        {
            return returnPercent.Value;
        }
    }
}
