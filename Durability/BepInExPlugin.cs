using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace Durability
{
    [BepInPlugin("aedenthorn.Durability", "Durability", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<float> fluidDurabilityMultiplier;
        public static ConfigEntry<float> foodDurabilityMultiplier;
        public static ConfigEntry<float> usableDurabilityMultiplier;
        public static ConfigEntry<float> equipmentDurabilityMultiplier;
        public static ConfigEntry<string> specialDurabilityMultiplier;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static double lastTime = 1;
        public static bool pausedMenu = false;
        public static bool wasActive = false;
        public static Dictionary<string, float> specials = new Dictionary<string, float>();

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
            specialDurabilityMultiplier = Config.Bind<string>("Options", "SpecialDurabilityMultiplier", "", "Special item uses multiplier. Use format ItemName:Multiplier, e.g. HeadLight:2 (comma-separated).");
            fluidDurabilityMultiplier = Config.Bind<float>("Options", "FluidDurabilityMultiplier", 1, "Fluid item uses multiplier");
            foodDurabilityMultiplier = Config.Bind<float>("Options", "FoodDurabilityMultiplier", 1, "Food item uses multiplier");
            usableDurabilityMultiplier = Config.Bind<float>("Options", "UsableDurabilityMultiplier", 1, "Usable item uses multiplier");
            equipmentDurabilityMultiplier = Config.Bind<float>("Options", "EquipmentDurabilityMultiplier", 1, "Equipment item durability multiplier");

            var array = specialDurabilityMultiplier.Value.Split(',');
            foreach (var s in array)
            {
                if (!s.Contains(":"))
                    continue;
                var split = s.Split(':');
                if (float.TryParse(split[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float mult))
                {
                    specials.Add(split[0], mult);
                }
            }
            Dbgl($"Got {specials.Count} special mults");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }


		//[HarmonyPatch(typeof(ItemInstance), new Type[] {typeof(Item_Base), typeof(int), typeof(int), typeof(string) })]
		//[HarmonyPatch(MethodType.Constructor)]
		static class ItemInstance_Patch
		{
			static void Prefix(Item_Base itemBase, ref int uses)
			{
				if (!modEnabled.Value)
					return;
                Dbgl($"Creating new item {itemBase.UniqueName}; uses {uses}, max uses {itemBase.MaxUses}");
            }
        }
		[HarmonyPatch(typeof(Item_Base), nameof(Item_Base.MaxUses))]
		[HarmonyPatch(MethodType.Getter)]
		static class Item_Base_MaxUses_Getter_Patch
		{
			static void Postfix(Item_Base __instance, ref int __result)
			{
				if (!modEnabled.Value || __result <= 1)
					return;
                float mult = 1;
                if(specials.TryGetValue(__instance.UniqueName, out mult))
                {

                }
                else if(__instance.settings_consumeable.FoodType > FoodType.None)
                {
                    switch (__instance.settings_consumeable.FoodType)
                    {
                        case FoodType.Food:
                            mult = foodDurabilityMultiplier.Value;
                            break;
                        case FoodType.Water:
                        case FoodType.SaltWater:
                            mult = fluidDurabilityMultiplier.Value;
                            break;
                    }
                    //Dbgl($"consumable {__instance.UniqueName}; durability {__result}x{mult}");
                }
                else if (__instance.settings_usable.IsUsable())
                {
                    mult = usableDurabilityMultiplier.Value;
                    //Dbgl($"tool {__instance.UniqueName}; durability {__result}x{mult}");
                }
                else if (__instance.settings_equipment.EquipType > EquipSlotType.None)
                {
                    mult = equipmentDurabilityMultiplier.Value;
                    //Dbgl($"equipment {__instance.UniqueName}; durability {__result}x{mult}");
                }
                __result = Mathf.CeilToInt(__result * mult);
            }
        }
	}
}
