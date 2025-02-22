using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace EnableCheats
{
    [BepInPlugin("aedenthorn.EnableCheats", "Enable Cheats", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> cheatsEnabled;
        public static ConfigEntry<bool> devEnabled;
        public static ConfigEntry<KeyCode> cheatSprintKey;

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
			cheatsEnabled = Config.Bind<bool>("General", "CheatsEnabled", true, "Enable Cheats");
			devEnabled = Config.Bind<bool>("General", "DevEnabled", false, "Enable dev mode");
			cheatSprintKey = Config.Bind<KeyCode>("General", "CheatSprintKey", KeyCode.Mouse4, "Set a different sprint key");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

		[HarmonyPatch(typeof(Cheat), nameof(Cheat.AllowCheatsForUser))]
		public static class Cheat_AllowCheatsForUser_Patch
        {
            public static bool Prefix(ref bool __result)
            {
                if (!modEnabled.Value)
                    return true;
                __result = cheatsEnabled.Value;
                return false;
            }
        }
		[HarmonyPatch(typeof(Cheat), nameof(Cheat.IsLocalPlayerDev))]
		public static class Cheat_IsLocalPlayerDev_Patch
        {
            public static bool Prefix(ref bool __result)
            {
                if (!modEnabled.Value)
                    return true;
                __result = devEnabled.Value;
                return false;
            }
        }
        [HarmonyPatch(typeof(PersonController), "Update")]
        static class PersonController_Update_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling PersonController_Update");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(Input), nameof(Input.GetMouseButtonDown)))
                    {
                        Dbgl("adding method to override cheat sprint");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetSprintKeyDown))));
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }

        private static bool GetSprintKeyDown(bool down)
        {
            if (!modEnabled.Value || cheatSprintKey.Value == KeyCode.None)
            {
                if (down)
                {
                    Dbgl("default down");
                }
                return down;
            }
            down = Input.GetKeyDown(cheatSprintKey.Value);
            if (down)
            {
                Dbgl("key down");
            }
            return down;

        }
    }
}
