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

namespace AutoFuel
{
    [BepInPlugin("aedenthorn.AutoFuel", "Auto Fuel", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> hotkey;
        public static ConfigEntry<string> disallowedItems;
        public static ConfigEntry<float> range;

        public static Dictionary<Fuel, int> ticks = new Dictionary<Fuel, int>();

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
            hotkey = Config.Bind<string>("Options", "Hotkey", "k", "Hotkey to trigger quick store");
            range = Config.Bind<float>("Options", "Range", 10, "Range in metres from storage to allow quick store (-1 is infinite range)");
            disallowedItems = Config.Bind<string>("Options", "DisallowedItems", "", "List of items that will not be moved (comma-separated)");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        [HarmonyPatch(typeof(Fuel), nameof(Fuel.UpdateFuel))]
        static class Fuel_UpdateFuel_Patch
        {
            public static void Prefix(Fuel __instance, ref int __state)
            {
                if (!modEnabled.Value)
                    return;

                __state = __instance.IsBurning() ? __instance.GetFuelCount() : -1;
            }
            public static void Postfix(Fuel __instance, int __state)
            {
                if (!modEnabled.Value || __state < 0 || __state == __instance.GetFuelCount())
                    return;
                foreach (Storage_Small s in StorageManager.allStorages)
                {
                    foreach(var slot in s.)
                    s.GetInventoryReference().RemoveCostMultiple(array, true);
                }
            }
        }
    }
}
