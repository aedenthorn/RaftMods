using BepInEx;
using BepInEx.Configuration;
using FMOD;
using FMODUnity;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UltimateWater;
using UnityEngine;

namespace HereFishy
{
    [BepInPlugin("aedenthorn.HereFishy", "Here Fishy", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> playHereFishy;
        public static ConfigEntry<bool> playWeee;
        public static ConfigEntry<bool> consumeBait;
        public static ConfigEntry<bool> loseDurability;
        public static ConfigEntry<string> hotkey;
        public static ConfigEntry<string> genericModel;

        public static AudioClip fishyClip;
        public static AudioClip weeClip;
        public static FMOD.Sound fishySound;
        public static FMOD.Sound weeSound;
        public static uint weeLength;
        public static uint fishyLength;
        private static bool running;

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
            playHereFishy = Config.Bind<bool>("Options", "PlayHereFishy", true, "Play here fishy sound");
            playWeee = Config.Bind<bool>("Options", "PlayWeee", true, "Play weee sound");
            consumeBait = Config.Bind<bool>("Options", "ConsumeBait", true, "Consume bait on catch");
            loseDurability = Config.Bind<bool>("Options", "LoseDurability", true, "Lose durability on catch");
			hotkey = Config.Bind<string>("Options", "Hotkey", "h", "Hotkey to call fish");
			genericModel = Config.Bind<string>("Options", "GenericModel", "Raw_Mackerel", "Generic model to show for higher tier fish");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            StartCoroutine(LoadSoundsCoroutine());
        }

        [HarmonyPatch(typeof(FishingRod), "Update")]
        static class FishingRod_Update_Patch
        {
            public static void Postfix(FishingRod __instance, Throwable ___throwable, FishingBaitHandler ___fishingBaitHandler, Network_Player ___playerNetwork, bool ___isMetalRod, Rope ___rope)
            {
                if (!modEnabled.Value || running || !___throwable.InHand || !___playerNetwork.IsLocalPlayer || !AedenthornUtils.CheckKeyDown(hotkey.Value))
                    return;
                Dbgl("pressed here fishy button");

                var forward = ___playerNetwork.Camera.transform.forward;
                forward.y = 0;
                forward.Normalize();
                forward *= 5;
                var point = ___playerNetwork.Camera.transform.position + forward;
                point.y = SingletonGeneric<GameManager>.Singleton.water.GetHeightAt(point.x, point.z);
                point.x += Random.Range(-1, 1);
                point.z += Random.Range(-1, 1);

                //Dbgl($"player pos {___playerNetwork.transform.position}, camera forward {___playerNetwork.Camera.transform.forward}, forward {forward}");
                if (!Physics.Raycast(___playerNetwork.Camera.transform.position, point - ___playerNetwork.Camera.transform.position, Vector3.Distance(point, ___playerNetwork.Camera.transform.position), LayerMasks.MASK_GroundMask))
                {
                    Dbgl($"hit water at {point}");
                    context.StartCoroutine(HereFishyCoroutine(__instance, ___throwable, ___fishingBaitHandler, ___playerNetwork, ___isMetalRod, ___rope, point));
                }
            }
        }
        public static IEnumerator HereFishyCoroutine(FishingRod rod, Throwable throwable, FishingBaitHandler fishingBaitHandler, Network_Player player, bool isMetalRod, Rope rope, Vector3 point)
        {
            running = true;
            Dbgl("Starting here fishy");

            RuntimeManager.LowlevelSystem.getMasterChannelGroup(out ChannelGroup channelgroup);
            channelgroup.getVolume(out float origVolume);
            channelgroup.setVolume(0.5f);
            RuntimeManager.LowlevelSystem.playSound(fishySound, channelgroup, false, out var channel);
            var playing = true;
            while (playing)
            {
                yield return null;
                channel.isPlaying(out playing);
            }
            Item_Base rItem = fishingBaitHandler.GetRandomItemFromCurrentBaitPool(isMetalRod);
            if (rItem != null)
            {
                ParticleManager.PlaySystem("WaterSplash_Hook", point + Vector3.up * 0.1f, true);
                RuntimeManager.PlayOneShotSafe("event:/fishing/fish_hooked", point);
                var uic = AccessTools.FieldRefAccess<Network_Player, PlayerItemManager>(player, "playerItemManager").useItemController;
                GameObject nic = null;

                Dbgl($"caught {rItem.UniqueName}");

                var dict = AccessTools.FieldRefAccess<UseItemController, Dictionary<string, ItemConnection>>(uic, "connectionDictionary");

                if (!dict.TryGetValue(rItem.UniqueName, out var ic))
                {
                    dict.TryGetValue(genericModel.Value, out ic);
                }
                if (ic != null)
                {
                    Dbgl("Creating model");

                    nic = Instantiate(ic.obj, null, true);
                    Destroy(nic.GetComponent<ConsumeComponent>());
                    nic.transform.position = point;
                    nic.SetActive(true);
                }
                RuntimeManager.LowlevelSystem.playSound(weeSound, channelgroup, false, out channel);
                float fraction = 0;
                while (fraction < 1)
                {
                    fraction += Time.deltaTime;
                    if (nic != null)
                    {
                        nic.transform.position = Vector3.Lerp(point, player.transform.position, fraction);
                        nic.transform.position += new Vector3(0, Mathf.Sin(fraction * Mathf.PI), 0);
                    }
                    yield return null;
                }
                channelgroup.setVolume(origVolume);
                if (nic != null)
                {
                    nic.Destroy();
                }
                player.Inventory.AddItem(rItem.UniqueName, 1);
                if (consumeBait.Value)
                {
                    fishingBaitHandler.ConsumeBait();
                }
                if (loseDurability.Value && player.Inventory.RemoveDurabillityFromHotSlot(1))
                {
                    rope.gameObject.SetActive(false);
                }
            }
            running = false;
            yield break;
        }
        public static IEnumerator LoadSoundsCoroutine()
        {

            MODE mode = MODE.LOOP_OFF | MODE.CREATECOMPRESSEDSAMPLE;
            
            var path = Path.Combine(AedenthornUtils.GetAssetPath(context), "herefishy.wav");
            if (RuntimeManager.LowlevelSystem.createSound(path, mode, out fishySound) == RESULT.OK)
            {
                fishySound.getLength(out fishyLength, TIMEUNIT.MS);
                Dbgl($"Loaded herefishy sound {fishyLength / 1000f}");
            }

            path = Path.Combine(AedenthornUtils.GetAssetPath(context), "wee.wav");
            if (RuntimeManager.LowlevelSystem.createSound(path, mode, out weeSound) == RESULT.OK)
            {
                weeSound.getLength(out weeLength, TIMEUNIT.MS);
                Dbgl($"Loaded wee sound {weeLength / 1000f}");
            }

            yield break;
        }
    }
}
