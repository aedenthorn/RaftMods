using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;

namespace ResearchTitaniumTools
{
    [BepInPlugin("aedenthorn.ResearchTitaniumTools", "Research Titanium Tools", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

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

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(ItemInstance_Recipe), nameof(ItemInstance_Recipe.LearnedViaBlueprint))]
        [HarmonyPatch(MethodType.Getter)]
        public static class ItemInstance_Recipe_LearnedViaBlueprint_Patch
        {
            public static void Postfix(ItemInstance_Recipe __instance, ref bool __result)
            {
                if (!modEnabled.Value || !__result)
                    return;
                if(__instance.SubCategory == "Hooks" || __instance.SubCategory == "Weapons" || __instance.SubCategory == "Hooks" || __instance.SubCategory == "BowCategory" || __instance.SubCategory == "Axes")
                {
                    __result = false;
                }
            }
        }
    }
}
