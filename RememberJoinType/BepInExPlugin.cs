using BepInEx;
using BepInEx.Configuration;
using FMODUnity;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UltimateWater;
using UnityEngine;
using System.Linq;
using System.Reflection.Emit;
using System.Collections;
using System.IO;
using UnityEngine.UI;
using System;

namespace RememberJoinType
{
    [BepInPlugin("aedenthorn.RememberJoinType", "Remember Join Type", "0.1.0")]
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

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


        [HarmonyPatch(typeof(LoadGameBox), "Button_SelectLoad")]
        public static class LoadGameBox_Button_SelectLoad_Patch
        {
            public static void Postfix(LoadGameBox __instance, Dropdown ___authSettingDropdown)
            {
                if (!modEnabled.Value)
                    return;
                var path = Path.Combine(AedenthornUtils.GetAssetPath(context, true), $"{SaveAndLoad.WorldToLoad.name}");
                if (File.Exists(path) && Enum.TryParse<RequestJoinAuthSetting>(File.ReadAllText(path), out RequestJoinAuthSetting setting))
                {
                    for(int i = 0; i < ___authSettingDropdown.options.Count; i++)
                    {
                        ___authSettingDropdown.value = i;
                        if (__instance.CheckAuthSettingFromDropdown() == setting)
                        {
                            return;
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Raft_Network), nameof(Raft_Network.HostGame))]
        public static class Raft_Network_HostGame_Patch
        {
            public static void Prefix(RequestJoinAuthSetting joinAuthSetting)
            {
                if (!modEnabled.Value || SaveAndLoad.WorldToLoad == null)
                    return;
                File.WriteAllText(Path.Combine(AedenthornUtils.GetAssetPath(context, true), $"{SaveAndLoad.WorldToLoad.name}"), joinAuthSetting.ToString());
            }
        }
    }
}
