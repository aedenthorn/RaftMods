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
using static UnityEngine.GraphicsBuffer;

namespace HereFishy
{
    [BepInPlugin("aedenthorn.HereFishy", "Here Fishy", "0.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> playHereFishy;
        public static ConfigEntry<bool> useFemaleSound;
        public static ConfigEntry<bool> playWeee;
        public static ConfigEntry<bool> consumeBait;
        public static ConfigEntry<bool> loseDurability;
        public static ConfigEntry<KeyCode> hotkey;
        public static ConfigEntry<string> genericModel;

        public static FMOD.Sound fishySound;
        public static FMOD.Sound fishySoundFemale;
        public static FMOD.Sound weeSound;
        private static bool running;

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
            playHereFishy = Config.Bind<bool>("Options", "PlayHereFishy", true, "Play here fishy sound");
            useFemaleSound = Config.Bind<bool>("Options", "UseFemaleSound", false, "Use alternative female here fishy sound");
            playWeee = Config.Bind<bool>("Options", "PlayWeee", true, "Play weee sound");
            consumeBait = Config.Bind<bool>("Options", "ConsumeBait", true, "Consume bait on catch");
            loseDurability = Config.Bind<bool>("Options", "LoseDurability", true, "Lose durability on catch");
			hotkey = Config.Bind<KeyCode>("Options", "Hotkey", KeyCode.H, "Hotkey to call fish");
			genericModel = Config.Bind<string>("Options", "GenericModel", "Raw_Mackerel", "Generic model to show for higher tier fish");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            StartCoroutine(LoadSoundsCoroutine());
        }

        [HarmonyPatch(typeof(FishingRod), "Update")]
        static class FishingRod_Update_Patch
        {
            public static void Postfix(FishingRod __instance, Throwable ___throwable, FishingBaitHandler ___fishingBaitHandler, Network_Player ___playerNetwork, bool ___isMetalRod, Rope ___rope)
            {
                if (!modEnabled.Value || running || !___throwable.InHand || !___playerNetwork.IsLocalPlayer || !Input.GetKeyDown(hotkey.Value))
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
                var playerPoint = ___playerNetwork.Camera.transform.position;
                if (!Physics.Raycast(playerPoint, point - ___playerNetwork.Camera.transform.position, Vector3.Distance(point, ___playerNetwork.Camera.transform.position), LayerMasks.MASK_GroundMask))
                {
                    Dbgl($"hit water at {point}");
                    
                    context.StartCoroutine(HereFishyCoroutine(__instance, ___throwable, ___fishingBaitHandler, ___playerNetwork, ___isMetalRod, ___rope, point, playerPoint, ___playerNetwork.Camera.transform));
                }
            }
        }
        public static IEnumerator HereFishyCoroutine(FishingRod rod, Throwable throwable, FishingBaitHandler fishingBaitHandler, Network_Player player, bool isMetalRod, Rope rope, Vector3 oldPoint, Vector3 playerPoint, Transform cameraTransform)
        {
            running = true;
            Dbgl("Starting here fishy");

            RuntimeManager.LowlevelSystem.getMasterChannelGroup(out ChannelGroup channelgroup);
            channelgroup.getVolume(out float origVolume);
            channelgroup.setVolume(playHereFishy.Value ? 0.5f : 0f);
            RuntimeManager.LowlevelSystem.playSound(useFemaleSound.Value ? fishySoundFemale : fishySound, channelgroup, false, out var channel);
            var playing = true;
            while (playing)
            {
                yield return null;
                channel.isPlaying(out playing);
            }
            Item_Base rItem = fishingBaitHandler.GetRandomItemFromCurrentBaitPool(isMetalRod);
            if (rItem != null)
            {
                var newPoint = oldPoint + cameraTransform.position - playerPoint;
                ParticleManager.PlaySystem("WaterSplash_Hook", newPoint + Vector3.up * 0.1f, true);
                RuntimeManager.PlayOneShotSafe("event:/fishing/fish_hooked", newPoint);
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
                    nic.transform.position = newPoint;
                    nic.SetActive(true);
                }
                if (playWeee.Value)
                {
                    channelgroup.setVolume(0.5f);
                    RuntimeManager.LowlevelSystem.playSound(weeSound, channelgroup, false, out channel);
                }
                float fraction = 0;
                while (fraction < 1)
                {
                    fraction += Time.deltaTime;
                    if (nic != null)
                    {
                        Vector3 targetDirection = cameraTransform.position - newPoint;
                        nic.transform.rotation = Quaternion.LookRotation(targetDirection);
                        nic.transform.position = Vector3.Lerp(newPoint, cameraTransform.position    , fraction);
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
            try
            {
                if (RuntimeManager.LowlevelSystem.createSound(path, mode, out fishySound) == RESULT.OK)
                {
                    fishySound.getLength(out var fishyLength, TIMEUNIT.MS);
                    Dbgl($"Loaded herefishy sound {fishyLength / 1000f}");
                }
            }
            catch { }

            try
            {
                path = Path.Combine(AedenthornUtils.GetAssetPath(context), "herefishy_female.wav");
                if (RuntimeManager.LowlevelSystem.createSound(path, mode, out fishySoundFemale) == RESULT.OK)
                {
                    fishySoundFemale.getLength(out var fishyLengthFemale, TIMEUNIT.MS);
                    Dbgl($"Loaded wee sound {fishyLengthFemale / 1000f}");
                }
            }
            catch { }

            try
            {
                path = Path.Combine(AedenthornUtils.GetAssetPath(context), "wee.wav");
                if (RuntimeManager.LowlevelSystem.createSound(path, mode, out weeSound) == RESULT.OK)
                {
                    weeSound.getLength(out var weeLength, TIMEUNIT.MS);
                    Dbgl($"Loaded wee sound {weeLength / 1000f}");
                }
            }
            catch { }

            yield break;
        }
    }
}
