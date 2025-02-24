using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace GlobalBatteryCharge
{
    [BepInPlugin("aedenthorn.GlobalBatteryCharge", "Global Battery Charge", "0.2.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> proportionateCharge;
        public static ConfigEntry<int> batteryChargesPerTick;
        public static ConfigEntry<float> batteryPerWindturbine;
        public static ConfigEntry<string> interactText;

        public static TimerEventer timerEventer = new TimerEventer(15);
        public static FieldInfo efficiencyFi = AccessTools.Field(typeof(WindTurbine), "efficiancy");
        public static FieldInfo displayTextsFi = AccessTools.Field(typeof(DisplayTextManager), "displayTexts");
        public static FieldInfo textComponentFi = AccessTools.Field(typeof(DisplayText), "textComponent");
        public static float currentEfficiency;
        public static int batteryCount;

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
            proportionateCharge = Config.Bind<bool>("Options", "ProportionateCharge", true, "Charge supplied is based on how many batteries are being charged.");
            batteryChargesPerTick = Config.Bind<int>("Options", "BatteryChargesPerTick", 1, "Battery charges per charge");
            batteryPerWindturbine = Config.Bind<float>("Options", "BatteryPerWindturbine", 1f, "Batteries per turbine for max efficiency");
            interactText = Config.Bind<string>("Options", "InteractText", "\nEfficiency: {0}%\nBatteries Charging: {1}", "Interact text");

            if (!modEnabled.Value)
                return;

            timerEventer.OnTimerReached += new Action(ChargeBatteries);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            InvokeRepeating("UpdateEfficiency", 1, 1);
        }

        public static void ChargeBatteries()
        {
            foreach (var battery in FindObjectsOfType<Battery>())
            {
                if (battery != null && !battery.BatterySlotIsEmpty && battery.NormalizedBatteryLeft != 1f)
                {
                    battery.AddBatteryUsesNetworked(batteryChargesPerTick.Value);
                }
            }
        }
        public void Update()
        {
            if (!modEnabled.Value || !proportionateCharge.Value || !Raft_Network.IsHost || ComponentManager<Raft_Network>.Value?.GetLocalPlayer() == null)
                return;
            timerEventer.DoUpdate(Time.deltaTime * currentEfficiency, true);
        }

        public void UpdateEfficiency()
        {
            if (!modEnabled.Value || !proportionateCharge.Value || !Raft_Network.IsHost || ComponentManager<Raft_Network>.Value?.GetLocalPlayer() == null)
                return;
            GetTotalEfficiency();

        }

        public static void GetTotalEfficiency()
        {
            currentEfficiency = 0;
            foreach (var w in FindObjectsOfType<WindTurbine>())
            {
                currentEfficiency += (float)efficiencyFi.GetValue(w);
            }
            batteryCount = 0;
            foreach (var battery in FindObjectsOfType<Battery>())
            {
                if (battery != null && !battery.BatterySlotIsEmpty && battery.NormalizedBatteryLeft != 1f)
                    batteryCount++;
            }
            float load = batteryCount / batteryPerWindturbine.Value;
            currentEfficiency = load == 0 ? 0 : currentEfficiency / load;
        }


        [HarmonyPatch(typeof(WindTurbine), "ChargeBatteries")]
		public static class WindTurbine_ChargeBatteries_Patch
        {
            public static bool Prefix(WindTurbine __instance)
			{
                if (!modEnabled.Value)
					return true;
                if(!proportionateCharge.Value)
                    ChargeBatteries();
                return false;
            }
        }
        [HarmonyPatch(typeof(Battery), "OnIsRayed")]
		public static class Battery_OnIsRayed_Patch
        {
            public static void Postfix(Battery __instance, CanvasHelper ___canvas)
			{
                if (!modEnabled.Value || currentEfficiency == 0)
					return;

                var dts = (DisplayText[])displayTextsFi.GetValue(___canvas.displayTextManager);
                var tc = (Text)textComponentFi.GetValue(dts[0]);
                if (!string.IsNullOrEmpty(tc.text))
                {
                    tc.text += string.Format(interactText.Value, Mathf.RoundToInt(currentEfficiency * 100), batteryCount);
                }
            }
        }
    }
}
