using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace RemoveBuildPiece
{
    [BepInPlugin("aedenthorn.RemoveBuildPiece", "RemoveBuildPiece", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> returnPercent;
        public static bool removing = false;

        public static void Dbgl(string str = "", BepInEx.Logging.LogLevel level = BepInEx.Logging.LogLevel.Debug, bool pref = true)
        {
            if (isDebug.Value)
                context.Logger.Log(level, (pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

		[HarmonyPatch(typeof(ItemInstance_Buildable), nameof(ItemInstance_Buildable.Placeable))]
		[HarmonyPatch(MethodType.Getter)]
        public static class ItemInstance_Buildable_Placeable_Getter_Patch
        {
            public static bool Prefix(ref bool __result)
            {
                if (!modEnabled.Value || !removing)
                    return true;
                __result = true;
                return false;
            }
        }
		[HarmonyPatch(typeof(RemovePlaceables), "Update")]
        public static class RemovePlaceables_Update_Patch
        {
            public static void Prefix()
            {
                if (!modEnabled.Value)
                {
                    return;
                }
                removing = true;
            }
            public static void Postfix()
            {
                removing = false;
            }
        }
        [HarmonyPatch(typeof(RemovePlaceables), "ReturnItemsFromBlock")]
        public static class RemovePlaceables_ReturnItemsFromBlock_Patch
        {
            public static void Prefix(ref bool giveItems)
            {
                if (modEnabled.Value && removing)
                    giveItems = true;

            }
            public static void Postfix(Block block, Network_Player player)
            {
                if (modEnabled.Value && removing)
                {
                    AchievementHandler.AddBuildRemoveCount(1);
                    BlockCreator.RemoveBlock(block, player, true);
                }
            }
        }
    }
}
