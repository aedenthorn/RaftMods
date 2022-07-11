using BepInEx;
using BepInEx.Configuration;
using FMODUnity;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UltimateWater;
using UnityEngine;

namespace InstantGather
{
    [BepInPlugin("aedenthorn.InstantGather", "Instant Gather", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

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

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

		[HarmonyPatch(typeof(Hook), "Awake")]
		static class Hook_Awake_Patch
        {
			static void Prefix(Hook __instance, ref float ___gatherTime)
			{
				if (!modEnabled.Value)
					return;
                ___gatherTime = 0.0001f;
            }
        }
    }
}
