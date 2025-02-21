using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Steamworks;
using System;
using System.Reflection;
using UnityEngine;

namespace GlobalBatteryCharge
{
    [BepInPlugin("aedenthorn.GlobalBatteryCharge", "Global Battery Charge", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> range;

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
            range = Config.Bind<float>("Options", "Range", -1f, "Max range to charge");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

		[HarmonyPatch(typeof(WindTurbine), "ChargeBatteries")]
		public static class WindTurbine_ChargeBatteries_Patch
        {
            public static bool Prefix(WindTurbine __instance)
			{
                if (!modEnabled.Value)
					return true;
                foreach(var battery in FindObjectsOfType<Battery>())
                {
                    if(range.Value < 0 || Vector3.Distance(__instance.transform.position, battery.transform.position) < range.Value)
                        AccessTools.Method(typeof(WindTurbine), "RechargeBattery").Invoke(__instance, new object[] { battery });
                }
                return false;
            }
        }
    }
}
