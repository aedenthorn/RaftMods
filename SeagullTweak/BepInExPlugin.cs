using BepInEx;
using BepInEx.Configuration;
using FMODUnity;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SharkTweak
{
    [BepInPlugin("aedenthorn.SeagullTweak", "Seagull Tweak", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<float> speedMult;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> neverAttackScarecrow;
        public static ConfigEntry<bool> neverAttackCrops;

        public static double lastTime = 1;
        public static bool pausedMenu = false;
        public static bool wasActive = false; 

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        } 
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");
			neverAttackScarecrow = Config.Bind<bool>("General", "NeverAttackScarecrow", true, "Prevent attacking scarecrows");
			neverAttackCrops = Config.Bind<bool>("General", "NeverAttackCrops", true, "Prevent attacking crops");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public void Update()
        {
        }

		[HarmonyPatch(typeof(Seagull), "CheckVisionToPoint")]

        static class AI_State_Attack_Entity_Shark_AttemptAttack_Patch
		{
			static bool Prefix(Seagull __instance, Vector3 point, string tagToCheck, float ___searchScarecrowRadius, ref bool __result)
			{
				if (!modEnabled.Value || tagToCheck != "Cropplot")
					return true;
                if (neverAttackCrops.Value)
                {
                    __result = false;
                    return false;
                }
                if (neverAttackScarecrow.Value && Traverse.Create(__instance).Method("GetScarecrowInVicinity", new object[] { point, ___searchScarecrowRadius, false }).GetValue<Scarecrow>() != null)
                { 
                    __result = false;
                    return false;
                }

                return true;
            }
        }
	}
}
