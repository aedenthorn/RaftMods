using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Steamworks;
using System;
using System.Reflection;
using UnityEngine;

namespace AnchorFromWheel
{
    [BepInPlugin("aedenthorn.AnchorFromWheel", "Anchor From Wheel", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;
        public static bool skipOthers;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> toggleKey;
        public static ConfigEntry<string> toggleText;
        public static FieldInfo fiBottom;
        public static FieldInfo fiUse;

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
            toggleKey = Config.Bind<string>("Options", "ToggleKey", "left alt", "Hold this key down to show toggle.");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            fiBottom = AccessTools.Field(typeof(Anchor_Stationary), "atBottom");
            fiUse = AccessTools.Field(typeof(Anchor_Stationary), "canUse");
        }

		[HarmonyPatch(typeof(SteeringWheel), nameof(SteeringWheel.OnIsRayed))]
		static class SteeringWheel_OnIsRayed_Patch
        {
			static void Postfix(MotorWheel __instance)
			{
                skipOthers = false;
                if (!modEnabled.Value || !AedenthornUtils.CheckKeyHeld(toggleKey.Value))
					return;

                var anchor = FindObjectOfType<Anchor_Stationary>();
                if (anchor == null || !(bool)fiUse.GetValue(anchor))
                    return;
                string text = (bool)fiBottom.GetValue(anchor) ? "Game/WeighAnchor" : "Game/DropAnchor";

                ComponentManager<DisplayTextManager>.Value.ShowText(Helper.GetTerm(text, true), MyInput.Keybinds["Interact"].MainKey, 0, 0, true);
                if (MyInput.GetButtonDown("Interact"))
                {
                    if (Raft_Network.IsHost)
                    {
                        ComponentManager<Raft_Network>.Value.RPC(new Message_NetworkBehaviour(Messages.StationaryAnchorUse, anchor), Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                        AccessTools.Method(typeof(Anchor_Stationary), "Use").Invoke(anchor, new object[] { });
                    }
                    else
                    {
                        ComponentManager<Raft_Network>.Value.SendP2P(ComponentManager<Raft_Network>.Value.HostID, new Message_NetworkBehaviour(Messages.StationaryAnchorUse, anchor), EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                    }
                }
            }
        }
    }
}
