using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Durability
{
    [BepInPlugin("aedenthorn.Durability", "Durability", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<float> toolDurabilityMultiplier;
        public static ConfigEntry<float> equipmentDurabilityMultiplier;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static double lastTime = 1;
        public static bool pausedMenu = false;
        public static bool wasActive = false; 

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
			toolDurabilityMultiplier = Config.Bind<float>("General", "ToolDurabilityMultiplier", 5, "Tool durability multiplier");
			equipmentDurabilityMultiplier = Config.Bind<float>("General", "EquipmentDurabilityMultiplier", 5, "Equipment durability multiplier");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private void Update()
        {
        }


		[HarmonyPatch(typeof(PlayerInventory), "RemoveStacksFromHotSlot")]
		static class PlayerInventory_RemoveStacksFromHotSlot_Patch
		{
			static void Prefix(ref int stacksToRemove)
			{
				if (!modEnabled.Value)
					return;

				if (stacksToRemove > 0)
				{
					stacksToRemove = Mathf.RoundToInt(stacksToRemove / toolDurabilityMultiplier.Value);
				}
			}
		}
		[HarmonyPatch(typeof(Slot), "IncrementUses")]
		static class Slot_IncrementUses_Patch
		{
			static void Prefix(Slot __instance, ref int amountOfUsesToAdd)
			{
				if (!modEnabled.Value)
					return;

				if (amountOfUsesToAdd < 0 && __instance.slotType == SlotType.Equipment)
				{
					amountOfUsesToAdd = Mathf.RoundToInt(amountOfUsesToAdd / equipmentDurabilityMultiplier.Value);
				}
			}
		}
	}
}
