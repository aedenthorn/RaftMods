using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace EngineTweaks
{
    [BepInPlugin("aedenthorn.EngineTweaks", "Engine Tweaks", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;
        public static bool skipOthers;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> foundationMult;
        public static ConfigEntry<string> toggleAllKey;
        public static ConfigEntry<string> toggleText;
        public static ConfigEntry<bool> useToggleOnSteeringWheel;

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
            foundationMult = Config.Bind<float>("Options", "FoundationMult", 1, "Multiply foundation pieces per engine by this amount");
            toggleAllKey = Config.Bind<string>("Options", "ToggleAllKey", "left shift", "Hold this key down when toggling power on one engine to toggle on all.");
            toggleText = Config.Bind<string>("Options", "ToggleText", "Toggle", "Text to show on steering wheel to toggle");
			useToggleOnSteeringWheel = Config.Bind<bool>("Options", "UseToggleOnSteeringWheel", true, "Allow using the toggle key on the steering wheel");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

		[HarmonyPatch(typeof(RaftWeightManager), nameof(RaftWeightManager.FoundationWeight))]
        [HarmonyPatch(MethodType.Getter)]
		static class RaftWeightManager_FoundationWeight_Patch
        {
			static void Postfix(ref int __result)
			{
				if (!modEnabled.Value)
					return;
                __result = Mathf.CeilToInt(__result / foundationMult.Value);
            }
        }
		[HarmonyPatch(typeof(MotorWheel), nameof(MotorWheel.ToggleEngine))]
		static class MotorWheel_ToggleEngine_Patch
        {
			static void Postfix(MotorWheel __instance)
			{
				if (!modEnabled.Value || !AedenthornUtils.CheckKeyHeld(toggleAllKey.Value) || skipOthers)
					return;
                skipOthers = true;
                var motors = FindObjectsOfType<MotorWheel>();
                Dbgl($"toggling {motors.Length} engines");
                foreach (var m in motors)
                {
                    if (m != __instance)
                        m.ToggleEngine();
                }
                skipOthers = false;
            }
        }
		[HarmonyPatch(typeof(SteeringWheel), nameof(SteeringWheel.OnIsRayed))]
		static class SteeringWheel_OnIsRayed_Patch
        {
			static void Postfix(MotorWheel __instance)
			{
                skipOthers = false;
                if (!modEnabled.Value || !useToggleOnSteeringWheel.Value || !AedenthornUtils.CheckKeyHeld(toggleAllKey.Value))
					return;

                ComponentManager<DisplayTextManager>.Value.ShowText(toggleText.Value, MyInput.Keybinds["Interact"].MainKey, 0, 0, true);
                if (MyInput.GetButtonDown("Interact"))
                {
                    var motors = FindObjectsOfType<MotorWheel>();
                    Dbgl($"toggling {motors.Length} engines");
                    skipOthers = true;
                    foreach (var m in motors)
                    {
                        m.ToggleEngine();
                    }
                    skipOthers = false;
                }
            }
        }
    }
}
