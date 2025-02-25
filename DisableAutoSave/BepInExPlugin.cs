using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace DisableAutoSave
{
    [BepInPlugin("aedenthorn.DisableAutoSave", "Disable AutoSave", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> saveEnabled;
        public static ConfigEntry<string> hotkey;

        public static void Dbgl(string str = "", BepInEx.Logging.LogLevel level = BepInEx.Logging.LogLevel.Debug, bool pref = false)
        {
            if (isDebug.Value)
                context.Logger.Log(level, (pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
            saveEnabled = Config.Bind<bool>("General", "SaveEnabled", true, "Enable saving");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");
			hotkey = Config.Bind<string>("Options", "Hotkey", "end", "Hotkey to trigger quick store");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public void Update()
        {
            if (!modEnabled.Value || ComponentManager<PlayerInventory>.Value == null)
                return;
            if (AedenthornUtils.CheckKeyDown(hotkey.Value))
            {
                saveEnabled.Value = !saveEnabled.Value;
                Dbgl($"Saving enabled: {saveEnabled.Value}");
            }
        }
        [HarmonyPatch(typeof(SaveAndLoad), nameof(SaveAndLoad.SaveGame))]
        public static class SaveAndLoad_SaveGame_Patch
        {
            public static bool Prefix()
            {
                if (!modEnabled.Value || saveEnabled.Value)
                    return true;
                if (!Environment.StackTrace.Contains("OnWorldRecieved") && !Environment.StackTrace.Contains("PauseMenu"))
                {
                    Dbgl($"Preventing save");
                    return false;
                }
                return true;
            }
        }
    }
}
