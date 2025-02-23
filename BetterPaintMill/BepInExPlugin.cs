using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace BetterPaintMill
{
    [BepInPlugin("aedenthorn.BetterPaintMill", "Better Paint Mill", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;


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

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(ColorMachine), "Update")]
        public static class ColorMachine_Update_Patch
        {
            public static void Prefix(ColorMachine __instance, Block_CookingStand ___stand, Raft ___raft)
            {
                if (!modEnabled.Value || ___stand == null)
                {
                    return;
                }
                if (!___raft.Moving)
                {
                    var wt = FindObjectOfType<WindTurbine>();
                    if (wt == null)
                    {
                        ___stand.fuel.SetFuelCount(0);
                        ___stand.fuel.StopBurning();
                    }
                    else
                    {
                        ___stand.fuel.SetFuelCount(int.MaxValue);
                   }
                }
                AccessTools.Method(typeof(ColorMachine), "HandleAnimation").Invoke(__instance, new object[] { });
            }
        }
    }
}
