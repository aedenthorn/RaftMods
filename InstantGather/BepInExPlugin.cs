using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;

namespace InstantGather
{
    [BepInPlugin("aedenthorn.InstantGather", "Instant Gather", "0.2.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> hookGatherTime;
        public static ConfigEntry<float> shovelGatherTime;
        public static ConfigEntry<float> corpseGatherTime;

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
			hookGatherTime = Config.Bind<float>("Options", "HookGatherTime", 0.0001f, "Hook gather time");
            shovelGatherTime = Config.Bind<float>("Options", "ShovelGatherTime", 0.0001f, "Shover gather time");
            corpseGatherTime = Config.Bind<float>("Options", "CorpseGatherTime", 0.0001f, "Corpse gather time");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

		[HarmonyPatch(typeof(Hook), "Awake")]
		public static class Hook_Awake_Patch
        {
            public static void Prefix(Hook __instance, ref float ___gatherTime)
			{
				if (!modEnabled.Value)
					return;
                ___gatherTime = hookGatherTime.Value;
            }
        }

		[HarmonyPatch(typeof(Shovel), "Start")]
		public static class Shovel_Start_Patch
        {
            public static void Prefix(ref float ___originItemChannelTime)
			{
				if (!modEnabled.Value)
					return;
                ___originItemChannelTime = shovelGatherTime.Value;
            }
        }

		[HarmonyPatch(typeof(PickupChanneling), "Awake")]
		public static class _Patch
        {
            public static void Prefix(ref float ___pickupTime)
			{
				if (!modEnabled.Value)
					return;
                ___pickupTime = corpseGatherTime.Value;
            }
        }
    }
}
