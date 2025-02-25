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
    [BepInPlugin("aedenthorn.CreatureTweaks", "Creature Tweaks", "0.2.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> animalsNeverAttackPlayer;
        public static ConfigEntry<bool> birdsNeverDropStones;
        public static ConfigEntry<bool> sharkNeverBitePlayer;
        public static ConfigEntry<bool> sharkNeverBiteBlocks;
        public static ConfigEntry<bool> bearNeverAttackPlayer;
        public static ConfigEntry<bool> boarNeverAttackPlayer;
        public static ConfigEntry<bool> pufferFishNeverExplode;

        public static ConfigEntry<float> sharkBitePlayerIntervalMult;
        public static ConfigEntry<float> sharkBiteBlockIntervalMult;

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
            animalsNeverAttackPlayer = Config.Bind<bool>("Options", "AnimalsNeverAttackPlayer", true, "Prevent various animals from attacking players");
            birdsNeverDropStones = Config.Bind<bool>("Options", "BirdsNeverDropStones", true, "Prevent birds from dropping stones on players");
            bearNeverAttackPlayer = Config.Bind<bool>("Options", "BearNeverAttackPlayer", true, "Prevent bears attacking players");
            boarNeverAttackPlayer = Config.Bind<bool>("Options", "BoarNeverAttackPlayer", true, "Prevent boars attacking players");
            pufferFishNeverExplode = Config.Bind<bool>("Options", "PufferFishNeverExplode", true, "Prevent pufferfish from exploding");
            sharkNeverBitePlayer = Config.Bind<bool>("Options", "SharkNeverBitePlayer", true, "Prevent sharks biting players");
            sharkBitePlayerIntervalMult = Config.Bind<float>("Options", "SharkBitePlayerIntervalMult", 1, "Multiplier for delay between biting players");
            sharkNeverBiteBlocks = Config.Bind<bool>("Options", "SharkNeverBiteBlocks", true, "Prevent sharks biting blocks");
            sharkBiteBlockIntervalMult = Config.Bind<float>("Options", "SharkBiteBlockIntervalMult", 1, "Multiplier for delay between biting blocks");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        [HarmonyPatch(typeof(AI_State_Attack_Entity_Shark), nameof(AI_State_Attack_Entity_Shark.UpdateState))]
        static class AI_State_Attack_Entity_Shark_UpdateState_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling AI_State_Attack_Entity_Shark.UpdateState");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(AI_State_Attack_Entity_Shark), "driveByTimer") && codes[i + 1].opcode == OpCodes.Call && (MethodInfo)codes[i + 1].operand == AccessTools.PropertyGetter(typeof(Time), nameof(Time.deltaTime)) && codes[i + 2].opcode == OpCodes.Add)
                    {
                        Dbgl("adding method to affect driveby timer");
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetDrivebyTimerIncrement))));
                    }
                }

                return codes.AsEnumerable();
            }
        }

        [HarmonyPatch(typeof(Helper), nameof(Helper.ClosestPlayerInWaterToPoint))]
        static class Helper_ClosestPlayerInWaterToPoint_UpdateState_Patch
        {
            static bool Prefix()
            {
                return (!modEnabled.Value || !sharkNeverBitePlayer.Value || !Environment.StackTrace.Contains("Shark"));
            }
        }
        public static float GetDrivebyTimerIncrement(float time)
        {
            if (!modEnabled.Value)
                return time;
            return time / sharkBitePlayerIntervalMult.Value;
        }
        [HarmonyPatch(typeof(AI_StateMachine_Shark), "UpdateStateMachine")]
        static class AI_StateMachine_Shark_UpdateStateMachine_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling AI_StateMachine_Shark.UpdateStateMachine");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(AI_StateMachine_Shark), "searchBlockProgress") && codes[i + 1].opcode == OpCodes.Call && (MethodInfo)codes[i + 1].operand == AccessTools.PropertyGetter(typeof(Time), nameof(Time.deltaTime)) && codes[i + 2].opcode == OpCodes.Add)
                    {
                        Dbgl("adding method to affect block search timer");
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetBlockSearchTimerIncrement))));
                    }
                }

                return codes.AsEnumerable();
            }
        }

        public static float GetBlockSearchTimerIncrement(float time)
        {
            if (!modEnabled.Value)
                return time;
            return time / sharkBiteBlockIntervalMult.Value;
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
