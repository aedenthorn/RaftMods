using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Speed
{
    [BepInPlugin("aedenthorn.Speed", "Speed", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> increaseHotkey;
        public static ConfigEntry<string> decreaseHotkey;
        public static ConfigEntry<string> swimModHotkey;
        public static ConfigEntry<double> swimSpeedMult;
        public static ConfigEntry<double> moveSpeedMult;

        public static void Dbgl(string str = "", BepInEx.Logging.LogLevel level = BepInEx.Logging.LogLevel.Debug, bool pref = true)
        {
            if (isDebug.Value)
                context.Logger.Log(level, (pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");
            moveSpeedMult = Config.Bind<double>("Speeds", "MoveSpeedMult", 1.5, "Move speed multiplier");
			swimSpeedMult = Config.Bind<double>("Speeds", "SwimSpeedMult", 1.5, "Swim speed multiplier");
            increaseHotkey = Config.Bind<string>("Options", "IncreaseHotkey", "=", "Hotkey to increase speed.");
            decreaseHotkey = Config.Bind<string>("Options", "DecreaseHotkey", "-", "Hotkey to decrease speed.");
            swimModHotkey = Config.Bind<string>("Options", "SwimModHotkey", "left alt", "Hotkey to switch to swim speed.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
        }
        public void Update()
        {
            if (!modEnabled.Value || ComponentManager<Raft_Network>.Value.GetLocalPlayer() == null)
                return;
            if (AedenthornUtils.CheckKeyDown(increaseHotkey.Value))
            {
                if (AedenthornUtils.CheckKeyHeld(swimModHotkey.Value))
                {

                    swimSpeedMult.Value = Math.Round(swimSpeedMult.Value + 0.1, 1);
                    Dbgl($"swim mult {swimSpeedMult.Value}");
                }
                else
                {
                    moveSpeedMult.Value = Math.Round(moveSpeedMult.Value + 0.1, 1);
                    Dbgl($"move mult {moveSpeedMult.Value}");
                }
            }
            else if (AedenthornUtils.CheckKeyDown(decreaseHotkey.Value))
            {
                if (AedenthornUtils.CheckKeyHeld(swimModHotkey.Value))
                {
                    swimSpeedMult.Value = Math.Round(Math.Max(0.1, swimSpeedMult.Value - 0.1),1);
                    Dbgl($"swim mult {swimSpeedMult.Value}");
                }
                else
                {
                    moveSpeedMult.Value = Math.Round(Math.Max(0.1, moveSpeedMult.Value - 0.1), 1);
                    Dbgl($"move mult {moveSpeedMult.Value}");
                }
            }
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
                    if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(PersonController), nameof(PersonController.normalSpeed)))
                    {
                        Dbgl("adding method to modify normalSpeed");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.MoveMultiply))));
                        i += 1;
                    }
                    if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(PersonController), nameof(PersonController.sprintSpeed)))
                    {
                        Dbgl("adding method to modify sprintSpeed");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.MoveMultiply))));
                        i += 1;
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
                    if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(PersonController), nameof(PersonController.swimSpeed)))
                    {
                        Dbgl("adding method to modify swimSpeed");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.SwimMultiply))));
                        i += 1;
                    }
                }


                return codes.AsEnumerable();
            }
        }
        public static float MoveMultiply(float value)
        {
            if (!modEnabled.Value)
                return value;
            return value * (float)moveSpeedMult.Value;
        }
        public static float SwimMultiply(float value)
        {
            if (!modEnabled.Value)
                return value;
            return value * (float)swimSpeedMult.Value;
        }
    }
}
