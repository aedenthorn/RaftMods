using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static Pomp.Animation.AnimationParameterController.AnimationParamData;

namespace EnableCheats
{
    [BepInPlugin("aedenthorn.EnableCheats", "Enable Cheats", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> cheatCommandsEnabled;
        public static ConfigEntry<bool> cheatKeysEnabled;
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
			cheatCommandsEnabled = Config.Bind<bool>("General", "CheatCommandsEnabled", true, "Enable cheat commands");
			cheatKeysEnabled = Config.Bind<bool>("General", "CheatKeysEnabled", true, "Enable cheat keys");
			devEnabled = Config.Bind<bool>("General", "DevEnabled", false, "Enable dev mode");
			cheatSprintKey = Config.Bind<KeyCode>("General", "CheatSprintKey", KeyCode.Mouse4, "Set a different sprint key");

            cheatKeysEnabled.SettingChanged += CheatKeysEnabled_SettingChanged;

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        private void CheatKeysEnabled_SettingChanged(object sender, EventArgs e)
        {
            Cheat.UseCheats = cheatKeysEnabled.Value;
        }

        //[HarmonyPatch(typeof(ItemManager), "LoadAllItems")]
        public static class LoadAllItemsPatch
        {
            public static void Postfix(List<Item_Base> ___allAvailableItems)
            {
                Dbgl(string.Join("\n", ___allAvailableItems.Select(i => i.UniqueName)));

            }
        }
        
        [HarmonyPatch(typeof(Cheat), nameof(Cheat.AllowCheatsForLocalPlayer))]
        public static class Cheat_AllowCheatsForLocalPlayer_Patch
        {
            public static bool Prefix(ref bool __result)
            {
                if (!modEnabled.Value)
                    return true; 
                __result = cheatKeysEnabled.Value;
                return false;
            }
        }


        [HarmonyPatch(typeof(Cheat), nameof(Cheat.AllowCheatsForUser))]
        public static class Cheat_AllowCheatsForUser_Patch
        {
            public static bool Prefix(ref bool __result, CSteamID cSteamID)
            {
                if (!modEnabled.Value || ComponentManager<Network_Player>.Value == null || ComponentManager<Network_Player>.Value.steamID != cSteamID)
                    return true;
                __result = cheatCommandsEnabled.Value;
                return false;
            }
        }

        [HarmonyPatch(typeof(RemoteConfigManager), nameof(RemoteConfigManager.CheckIfUserIsDev))]
		public static class RemoteConfigManager_CheckIfUserIsDev_Patch
        {
            public static void Postfix(CSteamID id, ref bool __result)
            {
                if (!modEnabled.Value || __result || ComponentManager<Network_Player>.Value == null || ComponentManager<Network_Player>.Value.steamID != id)
                    return;
                __result = devEnabled.Value;
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
