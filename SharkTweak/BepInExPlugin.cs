using BepInEx;
using BepInEx.Configuration;
using FMODUnity;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SharkTweak
{
    [BepInPlugin("aedenthorn.SharkTweak", "Shark Tweak", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<float> speedMult;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> neverBitePlayer;
        public static ConfigEntry<bool> neverBiteBlocks;

        public static double lastTime = 1;
        public static bool pausedMenu = false; 
        public static bool wasActive = false;

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
			neverBitePlayer = Config.Bind<bool>("General", "NeverBitePlayer", true, "Prevent biting players");
			neverBiteBlocks = Config.Bind<bool>("General", "NeverBiteBlocks", true, "Prevent biting blocks");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

		[HarmonyPatch(typeof(AI_State_Attack_Entity_Shark), nameof(AI_State_Attack_Entity_Shark.AttemptAttack))]
		static class AI_State_Attack_Entity_Shark_AttemptAttack_Patch
		{
			static bool Prefix(AI_State_Attack_Entity_Shark __instance, bool ___damageTreshHoldReached)
			{
				if (!modEnabled.Value || !neverBitePlayer.Value)
					return true;

                Network_Player network_Player = Helper.ClosestPlayerInWaterToPoint(__instance.stateMachine.transform.position, __instance.stateMachineShark.playerVisionRange, false);
                if (network_Player == null)
                {
                    return false;
                }
                if (!Semih_Network.IsHost)
                {
                    return false;
                }
                if (___damageTreshHoldReached)
                {
                    return false;
                }
                __instance.ForceDriveBy(true);
                return false;
            }
        }
		[HarmonyPatch(typeof(AI_State_Attack_Block_Shark), "FindBlockToAttack")]
		static class AI_State_Attack_Block_Patch
        {
			static bool Prefix(ref Block __result)
			{
				if (!modEnabled.Value || !neverBiteBlocks.Value)
					return true;

                __result = null;
                return false;
            }
        }
	}
}
