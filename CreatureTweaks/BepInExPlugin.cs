using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace CreatureTweaks
{
    [BepInPlugin("aedenthorn.CreatureTweaks", "Creature Tweaks", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> animalsNeverAttackPlayer;
        public static ConfigEntry<bool> birdsNeverDropStones;
        public static ConfigEntry<bool> sharkNeverBitePlayer;
        public static ConfigEntry<bool> sharkNeverBiteBlocks;
        public static ConfigEntry<bool> bearNeverAttackPlayer;
        public static ConfigEntry<bool> boarNeverAttackPlayer;
        public static ConfigEntry<bool> pufferFishNeverExplode;

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
            animalsNeverAttackPlayer = Config.Bind<bool>("Options", "AnimalsNeverAttackPlayer", true, "Prevent various animals from attacking players");
            birdsNeverDropStones = Config.Bind<bool>("Options", "BirdsNeverDropStones", true, "Prevent birds from dropping stones on players");
            bearNeverAttackPlayer = Config.Bind<bool>("Options", "BearNeverAttackPlayer", true, "Prevent bears attacking players");
            boarNeverAttackPlayer = Config.Bind<bool>("Options", "BoarNeverAttackPlayer", true, "Prevent boars attacking players");
            pufferFishNeverExplode = Config.Bind<bool>("Options", "PufferFishNeverExplode", true, "Prevent pufferfish from exploding");
            sharkNeverBitePlayer = Config.Bind<bool>("Options", "SharkNeverBitePlayer", true, "Prevent sharks biting players");
            sharkNeverBiteBlocks = Config.Bind<bool>("Options", "SharkNeverBiteBlocks", true, "Prevent sharks biting blocks");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        [HarmonyPatch(typeof(AI_State_Attack_Entity_Shark), nameof(AI_State_Attack_Entity_Shark.AttemptAttack))]
        static class AI_State_Attack_Entity_Shark_AttemptAttack_Patch
        {
            static bool Prefix(AI_State_Attack_Entity_Shark __instance, bool ___damageTreshHoldReached)
            {
                if (!modEnabled.Value || !sharkNeverBitePlayer.Value)
                    return true;

                Network_Player network_Player = Helper.ClosestPlayerInWaterToPoint(__instance.stateMachine.transform.position, __instance.stateMachineShark.playerVisionRange, false);
                if (network_Player == null)
                {
                    return false;
                }
                if (!Raft_Network.IsHost)
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
                if (!modEnabled.Value || !sharkNeverBiteBlocks.Value)
                    return true;

                __result = null;
                return false;
            }
        }
        [HarmonyPatch(typeof(AI_State_MeleeAttack), "SwitchTargetPlayer")]
        static class AI_State_MeleeAttack_SwitchTargetPlayer_Patch
        {
            static void Prefix(ref Network_Player player)
            {
                if (!modEnabled.Value || !animalsNeverAttackPlayer.Value)
                    return;
                player = null;
            }
        }
        [HarmonyPatch(typeof(AI_State_Bear_Decide_AttackState), nameof(AI_State_Bear_Decide_AttackState.DecideState))]
        static class AI_State_Bear_Decide_AttackState_Patch
        {
            static bool Prefix(AI_State_Bear_Decide_AttackState __instance)
            {
                if (!modEnabled.Value || !bearNeverAttackPlayer.Value)
                    return true;
                var ptr = AccessTools.Method(typeof(AI_State_DecideState), "DecideState").MethodHandle.GetFunctionPointer();
                var baseMethod = (Action)Activator.CreateInstance(typeof(Action), __instance, ptr);
                baseMethod();

                return false;
            }
        }
        [HarmonyPatch(typeof(AI_State_Boar_Circulate_Walk), "PlayerIsWithinRange")]
        static class AI_State_Boar_Circulate_Walk_PlayerIsWithinRange_Patch
        {
            static bool Prefix()
            {
                if (!modEnabled.Value || !boarNeverAttackPlayer.Value)
                    return true;

                return false;
            }
        }
        [HarmonyPatch(typeof(AI_State_Boar_Circulate_Run), "PlayerIsWithinRange")]
        static class AI_State_Boar_Circulate_Run_PlayerIsWithinRange_Patch
        {
            static bool Prefix()
            {
                if (!modEnabled.Value || !boarNeverAttackPlayer.Value)
                    return true;

                return false;
            }
        }
        [HarmonyPatch(typeof(AI_State_PufferFish_Explode), nameof(AI_State_PufferFish_Explode.Explode))]
        static class AI_State_PufferFish_Explode_Explode_Patch
        {
            static bool Prefix(AI_State_PufferFish_Explode __instance)
            {
                if (!modEnabled.Value || !pufferFishNeverExplode.Value)
                    return true;
                __instance.stateMachine.ChangeState(__instance.stateMachine.previousState);
                return false;
            }
        }
        [HarmonyPatch(typeof(AI_State_StoneBird_GrabStone), nameof(AI_State_StoneBird_GrabStone.UpdateState))]
        static class AI_State_StoneBird_GrabStone_UpdateState_Patch
        {
            static bool Prefix(AI_State_StoneBird_GrabStone __instance)
            {
                if (!modEnabled.Value || !birdsNeverDropStones.Value)
                    return true;
                __instance.grabStoneTimer = 0;
                var state = (__instance.stateMachine as AI_StateMachine_StoneBird).dropStoneState;
                AccessTools.FieldRefAccess<AI_State_StoneBird_DropStone, float>(state, "dropStoneTimer") = AccessTools.FieldRefAccess<AI_State_StoneBird_DropStone, float>(state, "dropStoneCooldown");
                (__instance.stateMachine as AI_StateMachine_StoneBird).animController.SetBool("Flying", true);
                (__instance.stateMachine as AI_StateMachine_StoneBird).animController.SetBool("GrabbingStone", false); 
                __instance.stateMachine.ChangeState(AccessTools.FieldRefAccess<AI_State_StoneBird_DropStone, AI_State_CirculateSpawn_Air_StoneBird>(state, "circulateState"));
                return false;
            }
        }
    }
}
