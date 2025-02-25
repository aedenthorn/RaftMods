using BepInEx;
using BepInEx.Configuration;
using FMODUnity;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace GradualNeeds
{
    [BepInPlugin("aedenthorn.GradualNeeds", "Gradual Needs", "0.2.2")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> maxSoundInterval;

        public static float statWellBeingFraction;
        public static float statHealthFraction;
        public static float timeAtLastHealthCheck;
        public static float timeAtLastWellbeingCheck;

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
			maxSoundInterval = Config.Bind<float>("General", "MaxSoundInterval", 10, "Max sound interval");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            InvokeRepeating("UpdateFractions", 1, 1);

        }


        public void UpdateFractions()
        {
            if (!modEnabled.Value || ComponentManager<Raft_Network>.Value?.GetLocalPlayer() == null)
                return;

            PlayerStats stats = ComponentManager<Raft_Network>.Value.GetLocalPlayer().Stats;
            statHealthFraction = Mathf.Clamp01( stats.stat_health.NormalValue / Stat_WellBeing.WellBeingLimit);

            statWellBeingFraction = Mathf.Clamp01(((stats.stat_thirst.normalConsumable.NormalValue < stats.stat_hunger.normalConsumable.NormalValue) ? stats.stat_thirst.normalConsumable.NormalValue : stats.stat_hunger.normalConsumable.NormalValue) / Stat_WellBeing.WellBeingLimit);
            //Dbgl($"health {statHealthFraction}, wb {statWellBeingFraction}");
        }

        [HarmonyPatch(typeof(PlayerStats), "HandleSoundFeedback")]
        public static class PlayerStats_HandleSoundFeedback_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling PlayerStats_HandleSoundFeedback");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(PlayerStats), "PlaySoundOnInterval"))
                    {
                        Dbgl("replacing method to modify soundIntervall");
                        codes[i].operand = AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.PlaySoundOnInterval));
                        codes.RemoveAt(i - 3);
                    }
                }
                return codes.AsEnumerable();
            }
        }

        public static IEnumerator PlaySoundOnInterval(StudioEventEmitter emitter)
        {
            for ( ; ; )
            {
                yield return new WaitForSeconds(GetSoundInterval(3, emitter));
                emitter.Play();
            }
        }

        public static float GetSoundInterval(float interval, StudioEventEmitter emitter)
        {
            if (!modEnabled.Value)
                return interval;

            PlayerStats stats = ComponentManager<Raft_Network>.Value.GetLocalPlayer().Stats;
            if (emitter.Event == "event:/char/thirsty")
            {
                interval += ((maxSoundInterval.Value - interval) * (stats.stat_thirst.normalConsumable.NormalValue / Stat_WellBeing.WellBeingLimit));
            }
            else if (emitter.Event == "event:/char/hungry")
            {
                interval += interval + ((maxSoundInterval.Value - interval) * (stats.stat_hunger.normalConsumable.NormalValue / Stat_WellBeing.WellBeingLimit));
            }
            return interval;
        }
        [HarmonyPatch(typeof(PlayerStats), "HandleUIFeedback")]

        public static class PlayerStats_HandleUIFeedback_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling PlayerStats_HandleUIFeedback");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand is FieldInfo && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(PlayerStats), "vignetteInterval"))
                    {
                        Dbgl("adding method to modify vignetteInterval");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetHungerThirstInterval))));
                        i += 1;
                    }
                    else if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand is FieldInfo && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(PlayerStats), "aberationInterval") && codes[i + 3].opcode == OpCodes.Call)
                    {
                        Dbgl("adding method to modify aberationInterval");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetHungerThirstInterval))));
                        i += 1;
                    }
                    else if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand is FieldInfo && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(PlayerStats), "saturationInterval") && codes[i + 3].opcode == OpCodes.Call)
                    {
                        Dbgl("adding method to modify saturationInterval");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetHealthFeedback))));
                        i += 1;
                    }
                }

                return codes.AsEnumerable();
            }
        }


        private static Interval_Float GetHealthFeedback(Interval_Float value)
        {
            if (!modEnabled.Value)
                return value;
            return new Interval_Float(value.minValue - value.minValue * statHealthFraction, value.maxValue - value.maxValue * statHealthFraction);
        }

        public static Interval_Float GetHungerThirstInterval(Interval_Float value)
        {
            if (!modEnabled.Value)
                return value;
            return new Interval_Float(value.minValue - value.minValue * statWellBeingFraction, value.maxValue - value.maxValue * statWellBeingFraction);
        }


        [HarmonyPatch(typeof(PlayerStats), "Update")]
        public static class PlayerStats_Update_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling PlayerStats_Update");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldfld && codes[i].operand is FieldInfo && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(PlayerStats), "healthLostPerSecondWellbeing"))
                    {
                        Dbgl("adding method to modify healthLostPerSecondWellbeing");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetHealthLostPerSecondWellbeing))));
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }

        public static float GetHealthLostPerSecondWellbeing(float amount)
        {
            if (!modEnabled.Value)
                return amount;
            amount -= amount * GetStatWellBeingMultiplier(1);
            return amount;
        }

        [HarmonyPatch(typeof(PersonController), "GroundControll")]
        public static class PersonController_GroundControll_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling PersonController_GroundControll");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(Stat_WellBeing), nameof(Stat_WellBeing.groundSpeedMultiplier)))
                    {
                        Dbgl("adding method to modify groundSpeedMultiplier");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetGroundSpeedMultiplier))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_0));
                        i += 2;
                    }
                }

                return codes.AsEnumerable();
            }
        }
        
        [HarmonyPatch(typeof(PersonController), "WaterControll")]
        public static class PersonController_WaterControll_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling PersonController_WaterControll");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(Stat_WellBeing), nameof(Stat_WellBeing.swimSpeedMultiplier)))
                    {
                        Dbgl("adding method to modify swimSpeedMultiplier");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetStatWellBeingMultiplier))));
                        i++;
                    }
                }

                return codes.AsEnumerable();
            }
        }
        
        [HarmonyPatch(typeof(Stat_Oxygen), "Update")]
        public static class Stat_Oxygen_Update_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling Stat_Oxygen_Update");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(Stat_WellBeing), nameof(Stat_WellBeing.oxygenLostMultiplier)))
                    {
                        Dbgl("adding method to modify oxygenLostMultiplier");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetStatWellBeingMultiplier))));
                        i++;
                    }
                }

                return codes.AsEnumerable();
            }
        }


        public static float GetGroundSpeedMultiplier(float multiplier, PersonController pc)
        {
            if(pc.crouching) 
                return multiplier;
            return GetStatWellBeingMultiplier(multiplier);
        }

        public static float GetStatWellBeingMultiplier(float multiplier)
        {
            if (!modEnabled.Value)
                return multiplier;

            if (multiplier < 1)
            {
                return multiplier + (statWellBeingFraction * (1 - multiplier));
            }
            else if (multiplier == 1)
            {
                return statWellBeingFraction;
            }
            return multiplier - (statWellBeingFraction * (multiplier - 1));
        }
    }
}
