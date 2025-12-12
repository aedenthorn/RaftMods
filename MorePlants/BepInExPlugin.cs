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

namespace MorePlants
{
    [BepInPlugin("aedenthorn.MorePlants", "More Plants", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> alwaysReturnSeeds;

        public static string[] newSeeds = new string[]
        {
            "Chili",
            "Juniper",
            "Berries_Red",
            "Turmeric"

        };

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
            alwaysReturnSeeds = Config.Bind<bool>("Options", "AlwaysReturnSeeds", true, "Always return seeds on harvest");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
        }

        //[HarmonyPatch(typeof(UseItemController), "Awake")]
        public static class UseItemController_Awake_Patch
        {
            public static void Postfix(UseItemController __instance, Dictionary<string, ItemConnection> ___connectionDictionary)
            {
                if(!modEnabled.Value) 
                    return;
                var templateUse = ___connectionDictionary["Seed_Strawberry"];
                var templateItem = ItemManager.GetItemByName("Seed_Strawberry");
                foreach (var seed in newSeeds)
                {
                    var item = ItemManager.GetItemByName(seed);
                    item.settings_usable = templateItem.settings_usable.Clone();
                    __instance.allConnections.Add(new ItemConnection()
                    {
                        obj = templateUse.obj,
                        inventoryItem = item
                    });
                }
            }
        }

        [HarmonyPatch(typeof(RandomDropper), nameof(RandomDropper.GetRandomItems))]
        public static class RandomDropper_GetRandomItems_Patch
        {
            public static bool Prefix(RandomDropper __instance, ref Item_Base[] __result)
            {
                if(!modEnabled.Value || !alwaysReturnSeeds.Value) 
                    return true;
                Plant plant = __instance.GetComponent<Plant>();
                if (plant == null)
                    return true;
                Dbgl($"Creating guaranteed drops for {plant.item.UniqueName}");

                var asset = AccessTools.FieldRefAccess<RandomDropper, SO_RandomDropper>(__instance, "randomDropperAsset");
                List<Item_Base> items = new List<Item_Base>();
                foreach (var item in asset.randomizer.items)
                {
                    if (!(item.obj is Item_Base itemBase))
                        continue;
                    items.Add(itemBase);
                }
                __result = items.ToArray();
                return false;
            }
        }

        //[HarmonyPatch(typeof(Cropplot), nameof(Cropplot.AcceptsPlantType))]
        public static class Cropplot_AcceptsPlantType_Patch
        {
            public static bool Prefix(Cropplot __instance, Item_Base item, ref bool __result)
            {
                if(!modEnabled.Value) 
                    return true;
                if(__instance.acceptableItemTypes.Exists(t => t.UniqueName == "Seed_Strawberry"))
                {
                    __result = true;
                    return false;
                }
                return true;
            }
        }
    }
}
