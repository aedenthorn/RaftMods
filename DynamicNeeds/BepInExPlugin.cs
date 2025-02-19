using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace DynamicNeeds
{
    [BepInPlugin("aedenthorn.DynamicNeeds", "Dynamic Needs", "0.1.0")]
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
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
        }
        [HarmonyPatch(typeof(PersonController), "GroundControll")]
        private static class PersonController_GroundControll_Patch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling PersonController_GroundControll");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(Stat_WellBeing), nameof(Stat_WellBeing.groundSpeedMultiplier)))
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
        private static class PersonController_WaterControll_Patch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling PersonController_WaterControll");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(Stat_WellBeing), nameof(Stat_WellBeing.swimSpeedMultiplier)))
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
        private static class Stat_Oxygen_Update_Patch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling Stat_Oxygen_Update");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(Stat_WellBeing), nameof(Stat_WellBeing.oxygenLostMultiplier)))
                    {
                        Dbgl("adding method to modify oxygenLostMultiplier");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetStatWellBeingMultiplier))));
                        i++;
                    }
                }

                return codes.AsEnumerable();
            }
        }


        private static float GetGroundSpeedMultiplier(float multiplier, PersonController pc)
        {
            if(pc.crouching) 
                return multiplier;
            return GetStatWellBeingMultiplier(multiplier);
        }

        private static float GetStatWellBeingMultiplier(float multiplier)
        {
            if (!modEnabled.Value)
                return multiplier;
            Stat_WellBeing stat = FindObjectOfType<Stat_WellBeing>();
            if (stat == null)
                return multiplier;
            var stat_thirst = AccessTools.FieldRefAccess<Stat_WellBeing, Stat_Consumable>(stat, "stat_thirst");
            var stat_hunger = AccessTools.FieldRefAccess<Stat_WellBeing, Stat_Consumable>(stat, "stat_hunger");
            float fraction = ((stat_thirst.NormalValue < stat_hunger.NormalValue) ? stat_thirst.NormalValue : stat_hunger.NormalValue) / Stat_WellBeing.WellBeingLimit;
            if (multiplier < 1)
            {
                return multiplier + (fraction * (1 - multiplier));
            }
            return multiplier - (fraction * (multiplier - 1));
        }
    }
}
