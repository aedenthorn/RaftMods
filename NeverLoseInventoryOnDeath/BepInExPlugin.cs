using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace NeverLoseInventoryOnDeath
{
    [BepInPlugin("aedenthorn.NeverLoseInventoryOnDeath", "NeverLoseInventoryOnDeath", "0.2.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> gatherTime;

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

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

		[HarmonyPatch(typeof(Player), nameof(Player.StartRespawn))]
		static class Player_StartRespawn_Patch
        {
			static void Prefix(ref bool clearInventory)
			{
                if (modEnabled.Value)
                    clearInventory = false;
            }
        }

		[HarmonyPatch(typeof(Player), nameof(Player.RespawnWithoutBed))]
		static class Player_RespawnWithoutBed_Patch
        {
			static void Prefix(ref bool clearInventory)
			{
                if (modEnabled.Value)
                    clearInventory = false;
            }
        }
		[HarmonyPatch(typeof(DeathMenu), nameof(DeathMenu.MenuOpen))]
		static class DeathMenu_MenuOpen_Patch
        {
			static bool Prefix(DeathMenu __instance, FadePanel ___fadePanel, GameObject ___respawnButton, GameObject ___loseItemsText)
			{
                if (!modEnabled.Value)
                    return true;
                ___fadePanel.SetAlpha(0f);
                __instance.StartCoroutine(___fadePanel.FadeToAlpha(1f, 0.5f));
                ___respawnButton.gameObject.SetActive(true);
                ___loseItemsText.SetActiveSafe(false);
                return false;

            }
        }
    }
}
