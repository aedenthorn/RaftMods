using FMODUnity;
using Harmony;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace SpawnSettings

{
	public class Main
	{
		private static readonly bool isDebug = false;

		public static void Dbgl(string str = "", bool pref = true)
		{
			if (isDebug)
				Debug.Log((pref ? "SpawnSettings " : "") + str);
		}
		public static Settings settings { get; private set; }
		public static bool enabled;
		private static void Load(UnityModManager.ModEntry modEntry)
		{
			settings = Settings.Load<Settings>(modEntry);

			modEntry.OnGUI = OnGUI;
			modEntry.OnSaveGUI = OnSaveGUI;
			modEntry.OnToggle = OnToggle;

			var harmony = HarmonyInstance.Create(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());

			return;
		}

		// Called when the mod is turned to on/off.
		static bool OnToggle(UnityModManager.ModEntry modEntry, bool value /* active or inactive */)
		{
			enabled = value;
			return true; // Permit or not.
		}
		private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
		{
			settings.Save(modEntry);
		}

		private static void OnGUI(UnityModManager.ModEntry modEntry)
		{
			GUILayout.Label(string.Format("Spawn amount multiplier: <b>{0:F1}x</b>", settings.SpawnAmountMultiplier), new GUILayoutOption[0]);
			settings.SpawnAmountMultiplier = GUILayout.HorizontalSlider(settings.SpawnAmountMultiplier * 10, 1f, 100f, new GUILayoutOption[0]) / 10f;
			GUILayout.Label(string.Format("Spawn interval multiplier: <b>{0:F1}x</b>", settings.SpawnIntervalMultiplier), new GUILayoutOption[0]);
			settings.SpawnIntervalMultiplier = GUILayout.HorizontalSlider(settings.SpawnIntervalMultiplier * 10, 1f, 100f, new GUILayoutOption[0]) / 10f;
		}

		[HarmonyPatch(typeof(SO_ObjectSpawner), "GetSettings")]
		static class SO_ObjectSpawner_GetSettings_Patch
		{
			static void Postfix(ref ObjectSpawnerAssetSettings __result)
			{
				if (!enabled)
					return;
				__result.spawnAmount.minValue = Mathf.RoundToInt(__result.spawnAmount.minValue * settings.SpawnAmountMultiplier);
				__result.spawnAmount.maxValue = Mathf.RoundToInt(__result.spawnAmount.maxValue * settings.SpawnAmountMultiplier);
				__result.spawnInterval = Mathf.RoundToInt(__result.spawnInterval * settings.SpawnIntervalMultiplier);
			}
		}
	}
}
