using BepInEx;
using BepInEx.Configuration;
using FMODUnity;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace QuickRemove
{
    [BepInPlugin("aedenthorn.QuickRemove", "Quick Remove", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<KeyCode> modKey;

        public static void Dbgl(object obj, BepInEx.Logging.LogLevel level = BepInEx.Logging.LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(level, obj);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");
            modKey = Config.Bind<KeyCode>("Options", "ModKey", KeyCode.LeftShift, "Key to hold to quick remove");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
        }

        [HarmonyPatch(typeof(RemovePlaceables), "Update")]
        public static class RemovePlaceables_Update_Patch
        {
            public static void Prefix(RemovePlaceables __instance, Network_Player ___playerNetwork, ref float ___removeTimer, float ___removeTime, Block ___currentBlock)
            {
                if(!modEnabled.Value || CanvasHelper.ActiveMenu != MenuType.None || !___playerNetwork.IsLocalPlayer || ChatTextFieldController.IsChatWindowSelected || ___currentBlock == null || !MyInput.GetButton("Remove") || !Input.GetKey(modKey.Value))
                    return;
                ___removeTimer = ___removeTime;
            }
        }

    }
}
