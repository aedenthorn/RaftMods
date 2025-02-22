using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SnowmobileEverywhere
{
    [BepInPlugin("aedenthorn.SnowmobileEverywhere", "Snowmobile Everywhere", "0.2.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<KeyCode> spawnKey;

        public static Dictionary<int, Vector3> posDict = new Dictionary<int, Vector3>();

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
            spawnKey = Config.Bind<KeyCode>("General", "SpawnKey", KeyCode.Keypad0, "Key to spawn a snowmobile");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public void Update()
        {
            if (!modEnabled.Value || ComponentManager<Raft_Network>.Value.GetLocalPlayer() == null || !Input.GetKeyDown(spawnKey.Value))
                return;
            Dbgl("Spawn key pressed");

            
            var sms = ComponentManager<ObjectSpawnerManager>.Value.floatingObjectParent.GetComponentInChildren<SnowmobileShed>(true);
            if (sms != null)
            {
                Dbgl("Spawning snowmobile");

                Snowmobile sm = AccessTools.FieldRefAccess<SnowmobileShed, Snowmobile>(sms, "snowmobilePrefab");
                var spawnedSnowmobile = UnityEngine.Object.Instantiate<Snowmobile>(sm, ComponentManager<Raft_Network>.Value.GetLocalPlayer().FeetPosition + ComponentManager<Raft_Network>.Value.GetLocalPlayer().transform.forward * 2f + Vector3.up, Quaternion.identity, null);
                spawnedSnowmobile.ObjectIndex = SaveAndLoad.GetUniqueObjectIndex();
                spawnedSnowmobile.BehaviourIndex = NetworkUpdateManager.GetUniqueBehaviourIndex();
                NetworkIDManager.AddNetworkID(spawnedSnowmobile, typeof(Snowmobile));
                NetworkIDManager.AddNetworkIDTick(spawnedSnowmobile);
                spawnedSnowmobile.OnSnowmobileReset = (Action<Snowmobile, bool>)Delegate.Combine(spawnedSnowmobile.OnSnowmobileReset, new Action<Snowmobile, bool>(BepInExPlugin.OnSnowmobileReset));
            }
        }

        public static void OnSnowmobileReset(Snowmobile snowmobile, bool arg2)
        {
            Dbgl("Destroying snowmobile");
            snowmobile.MakeAllPlayersLeave();
            Destroy(snowmobile.gameObject);
        }

        [HarmonyPatch(typeof(Snowmobile), "Update")]
        public static class Snowmobile_Update_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling Snowmobile_Update");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldsfld && codes[i].operand is FieldInfo && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(LayerMasks), nameof(LayerMasks.MASK_Obstruction)))
                    {
                        Dbgl("adding method to change mask");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetLayerMask))));
                        i++;
                    }
                }

                return codes.AsEnumerable();
            }
            public static void Postfix(Snowmobile __instance, Transform ___groundCheckPoint)
            {
                if (!modEnabled.Value)
                    return;
                Raft raft = ComponentManager<Raft>.Value;
                if (raft is null)
                {
                    return;
                }
                if (Physics.CheckSphere(___groundCheckPoint.position, 1, LayerMasks.MASK_GroundMask_Raft))
                {
                    if (__instance.transform.parent == null)
                    {
                        Dbgl("Locking snowmobile to raft");
                        __instance.transform.SetParent(SingletonGeneric<GameManager>.Singleton.lockedPivot);
                    }
                    else if(__instance.DrivingPlayer == null)
                    {
                        if(posDict.TryGetValue(__instance.GetInstanceID(), out var pos))
                        {
                            __instance.transform.localPosition = pos;                        }
                        else
                        {
                            Dbgl($"Setting fixed position to {__instance.transform.localPosition}");
                            posDict[__instance.GetInstanceID()] = __instance.transform.localPosition;
                        }
                        
                    }
                    else
                    {
                        posDict.Remove(__instance.GetInstanceID());
                    }
                }
                else
                {
                    posDict.Remove(__instance.GetInstanceID());
                    if (__instance.transform.parent != null)
                    {
                        Dbgl("Unlocking snowmobile from raft");

                        __instance.transform.SetParent(null);
                    }
                }
            }
        }
        


        public static LayerMask GetLayerMask(LayerMask mask)
        {
            if (!modEnabled.Value)
                return mask;
            return mask | LayerMasks.MASK_GroundMask;
        }
    }
}
