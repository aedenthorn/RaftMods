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

namespace CancelHook
{
    [BepInPlugin("aedenthorn.CancelHook", "Cancel Hook", "0.2.0")]
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
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Hook), "Update")]
        static class HandleLocalClient_Patch
        {
            static void Postfix(Hook __instance, Network_Player ___playerNetwork)
            {
                if (!modEnabled.Value)
                    return;

                if (___playerNetwork.IsLocalPlayer)
                {
                    if (CanvasHelper.ActiveMenu != MenuType.None)
                    {
                        return;
                    }
                    if (__instance.throwable.IsLocked)
                    {
                        return;
                    }
                    if (__instance.throwable.InHand)
                    {
                        if (MyInput.GetButton("LMB") && MyInput.GetButton("RMB"))
                        {
                            __instance.throwable.ResetCanThrow();
                            __instance.GetType().GetMethod("ResetHookToPlayer", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { });
                        }
                    }

                }
            }
        }
    }
}
