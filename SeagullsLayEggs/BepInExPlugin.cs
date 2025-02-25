using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace SeagullsLayEggs
{
    [BepInPlugin("aedenthorn.SeagullsLayEggs", "Seagulls Lay Eggs", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> range;

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

		[HarmonyPatch(typeof(Seagull), "LeaveNest")]
		public static class Seagull_LeaveNest_Patch
        {
            public static void Prefix(bool layEggs, ref bool ___dropEggInNest)
			{
                if (!modEnabled.Value || !layEggs)
					return;
                Dbgl("Laying egg");
                ___dropEggInNest = true;
            }
        }
    }
}
