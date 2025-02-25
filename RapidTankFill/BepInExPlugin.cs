using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static SO_TradingPost_Buyable;

namespace RapidTankFill
{
    [BepInPlugin("aedenthorn.RapidTankFill", "Rapid Tank Fill", "0.2.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<KeyCode> fuelModKey;

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
            fuelModKey = Config.Bind<KeyCode>("General", "ModKey", KeyCode.LeftShift, "Mod key to hold for rapid filling");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        
        [HarmonyPatch(typeof(Tank), "HandleAddFuel")]
        static class Tank_HandleAddFuel_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling Tank_HandleAddFuel");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(MyInput), nameof(MyInput.GetButtonDown)))
                    {
                        Dbgl("adding method to add repeatedly");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetKeyHeld))));
                        i++;
                    }
                }

                return codes.AsEnumerable();
            }
        }

        public static bool GetKeyHeld(bool result)
        {
            if (!modEnabled.Value || result || !MyInput.GetButton("Interact") || !Input.GetKey(fuelModKey.Value))
                return result;
            return true;
        }
    }
}
