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

namespace EnableCheats
{
    [BepInPlugin("aedenthorn.EnableCheats", "Enable Cheats", "0.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> cheatCommandsEnabled;
        public static ConfigEntry<bool> cheatKeysEnabled;
        public static ConfigEntry<bool> devEnabled;
        public static ConfigEntry<KeyCode> cheatSprintKey;

        public static Dictionary<string, ChatWord> chatWords = new Dictionary<string, ChatWord>();

        public static void Dbgl(string str = "", BepInEx.Logging.LogLevel level = BepInEx.Logging.LogLevel.Debug, bool pref = false)
        {
            if (isDebug.Value)
                context.Logger.Log(level, (pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");
			cheatCommandsEnabled = Config.Bind<bool>("General", "CheatCommandsEnabled", true, "Enable cheat commands");
			cheatKeysEnabled = Config.Bind<bool>("General", "CheatKeysEnabled", true, "Enable cheat keys");
			devEnabled = Config.Bind<bool>("General", "DevEnabled", false, "Enable dev mode");
			cheatSprintKey = Config.Bind<KeyCode>("General", "CheatSprintKey", KeyCode.Mouse4, "Set a different sprint key");

            cheatKeysEnabled.SettingChanged += CheatKeysEnabled_SettingChanged;

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            foreach(var i in Enum.GetNames(typeof(QuestType)))
            {
                ChatWordLibrary.chatWords["quest"].chatWords[i] = new ChatWord();
            }
        }

        private void CheatKeysEnabled_SettingChanged(object sender, EventArgs e)
        {
            Cheat.UseCheats = cheatKeysEnabled.Value;
        }

        [HarmonyPatch(typeof(ItemManager), "LoadAllItems")]
        public static class ItemManager_LoadAllItems_Patch
        {
            public static void Postfix(List<Item_Base> ___allAvailableItems)
            {
                ChatWordLibrary.chatWords["give"].chatWords = new Dictionary<string, ChatWord>();
                var keys = ___allAvailableItems.Select(i => i.UniqueName).ToList();
                keys.Sort();
                foreach (var i in keys)
                {
                    ChatWordLibrary.chatWords["give"].chatWords[i] = new ChatWord();
                }
            }
        }
        [HarmonyPatch(typeof(RareMaterial), "Awake")]
        public static class RareMaterial_Awake_Patch
        {
            public static void Postfix(Randomizer ___randomizer)
            {
                ChatWordLibrary.chatWords["animals"].chatWords["changematerial"].chatWords = new Dictionary<string, ChatWord>();
                var mats = ___randomizer.GetAllItems<Material>();
                
                foreach (var m in mats)
                {
                    var i = ___randomizer.GetIndexFromItem(m);
                    ChatWordLibrary.chatWords["animals"].chatWords["changematerial"].chatWords[i.ToString()] = new ChatWord();
                }
            }
        }
        [HarmonyPatch(typeof(CookingTable), "RetrieveAllRecipes")]
        public static class CookingTable_RetrieveAllRecipes_Patch
        {
            public static void Postfix(SO_CookingTable_Recipe[] ___allRecipes)
            {
                ChatWordLibrary.chatWords["cook"].chatWords = new Dictionary<string, ChatWord>();
                var keys = ___allRecipes.Select(i => i.Result.UniqueName).ToList();
                keys.Sort();
                foreach (var i in keys)
                {
                    ChatWordLibrary.chatWords["cook"].chatWords[i] = new ChatWord();
                }
            }
        }
        [HarmonyPatch(typeof(WeatherManager), "Start")]
        public static class WeatherManager_Start_Patch
        {
            public static void Postfix(List<WeatherPool> ___weatherPools)
            {
                ChatWordLibrary.chatWords["weather"].chatWords = new Dictionary<string, ChatWord>();
                List<string> keys = new List<string>();
                foreach (WeatherPool weatherPool in ___weatherPools)
                {
                    Weather[] allItems = weatherPool.randomizer.GetAllItems<Weather>();
                    if (allItems != null)
                    {
                        keys.AddRange(allItems.Select(w => w.name));
                    }
                }
                keys.Sort();
                foreach (var i in keys)
                {
                    ChatWordLibrary.chatWords["weather"].chatWords[i] = new ChatWord()
                    {
                        chatWords = new Dictionary<string, ChatWord>()
                        {
                            { "lerp", new ChatWord() }
                        }
                    };
                }
            }
        }
        public static InputField hintField;

        [HarmonyPatch(typeof(ChatTextFieldController), "Awake")]
        public static class ChatTextFieldController_Awake_Patch
        {
            public static void Postfix(ChatTextFieldController __instance)
            {
                if (!modEnabled.Value)
                    return;
                hintField = Instantiate(__instance.chatInputField, __instance.chatInputField.transform.parent);
                hintField.textComponent.color = Color.grey;
                __instance.chatInputField.transform.SetAsLastSibling();
                Destroy(__instance.chatInputField.GetComponent<Image>());
            }
        }

        public static string lastText = "";

        [HarmonyPatch(typeof(ChatTextFieldController), "HandleInput")]
        public static class ChatTextFieldController_HandleInput_Patch
        {
            public static void Postfix(ChatTextFieldController __instance)
            {
                if (!modEnabled.Value )
                    return;
                hintField.gameObject.SetActive(__instance.chatInputField.isActiveAndEnabled);

                var text = __instance.chatInputField.text;
                if (!text.StartsWith("/") || text == lastText)
                {
                    if (hintField.text.Length >= text.Length && Input.GetKeyDown(KeyCode.Tab))
                    {
                        __instance.chatInputField.text = hintField.text;
                        __instance.chatInputField.caretPosition = __instance.chatInputField.text.Length;
                        lastText = __instance.chatInputField.text;
                    }
                    return;
                }
                hintField.text = "";
                lastText = text;
                var words = text.Substring(1).Split(' ');
                var hints = new List<string>();
                var dict = ChatWordLibrary.chatWords;
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].Length == 0)
                    {
                        break;
                    }
                    foreach (var hint in dict.Keys)
                    {
                        if (hint.ToLower().Equals(words[i].ToLower()))
                        {
                            hints.Add(hint);
                            dict = dict[hint].chatWords;
                            goto next;
                        }
                    }
                    foreach (var hint in dict.Keys)
                    {
                        if (hint.ToLower().StartsWith(words[i].ToLower()))
                        {
                            hints.Add(hint);
                            goto breakout;
                        }
                    }
                    break;
                next:
                    continue;
                }
            breakout:
                if(hints.Count > 0)
                {
                    hintField.text = "/"+string.Join(" ", hints);
                }
            }
        }
        
        [HarmonyPatch(typeof(Cheat), nameof(Cheat.AllowCheatsForLocalPlayer))]
        public static class Cheat_AllowCheatsForLocalPlayer_Patch
        {
            public static bool Prefix(ref bool __result)
            {
                if (!modEnabled.Value)
                    return true; 
                __result = cheatKeysEnabled.Value;
                return false;
            }
        }


        [HarmonyPatch(typeof(Cheat), nameof(Cheat.AllowCheatsForUser))]
        public static class Cheat_AllowCheatsForUser_Patch
        {
            public static bool Prefix(ref bool __result, CSteamID cSteamID)
            {
                if (!modEnabled.Value || ComponentManager<Network_Player>.Value == null || ComponentManager<Network_Player>.Value.steamID != cSteamID)
                    return true;
                __result = cheatCommandsEnabled.Value;
                return false;
            }
        }

        [HarmonyPatch(typeof(RemoteConfigManager), nameof(RemoteConfigManager.CheckIfUserIsDev))]
		public static class RemoteConfigManager_CheckIfUserIsDev_Patch
        {
            public static void Postfix(CSteamID id, ref bool __result)
            {
                if (!modEnabled.Value || __result || ComponentManager<Network_Player>.Value == null || ComponentManager<Network_Player>.Value.steamID != id)
                    return;
                __result = devEnabled.Value;
            }
        }
        [HarmonyPatch(typeof(Cheat), "Give")]
        static class Cheat_Give_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling Cheat_Give");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(ItemManager), nameof(ItemManager.GetItemByNameContains)))
                    {
                        Dbgl("Changing method to get exact match item");
                        codes[i].operand = AccessTools.Method(typeof(ItemManager), nameof(ItemManager.GetItemByName));
                        codes.RemoveAt(i - 1);
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }
        [HarmonyPatch(typeof(PersonController), "Update")]
        static class PersonController_Update_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling PersonController_Update");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Call && codes[i].operand is MethodInfo && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(Input), nameof(Input.GetMouseButtonDown)))
                    {
                        Dbgl("adding method to override cheat sprint");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetSprintKeyDown))));
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }

        private static bool GetSprintKeyDown(bool down)
        {
            if (!modEnabled.Value || cheatSprintKey.Value == KeyCode.None)
            {
                if (down)
                {
                    Dbgl("default down");
                }
                return down;
            }
            down = Input.GetKeyDown(cheatSprintKey.Value);
            if (down)
            {
                Dbgl("key down");
            }
            return down;

        }
    }
}
