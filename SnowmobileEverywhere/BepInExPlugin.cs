using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SnowmobileEverywhere
{
    [BepInPlugin("aedenthorn.SnowmobileEverywhere", "Snowmobile Everywhere", "0.3.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<KeyCode> spawnKey;
        public static ConfigEntry<float> jumpVelocity;
        public static ConfigEntry<float> destroyDepth;

        public static Dictionary<int, Vector3> posDict = new Dictionary<int, Vector3>();

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
            spawnKey = Config.Bind<KeyCode>("Options", "SpawnKey", KeyCode.Keypad0, "Key to spawn a snowmobile");
            jumpVelocity = Config.Bind<float>("Options", "JumpVelocity", 5f, "Jump velocity");
            destroyDepth = Config.Bind<float>("Options", "DestroyDepth", -30f, "Jump velocity");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public void Update()
        {
            if (!modEnabled.Value || ComponentManager<Raft_Network>.Value.GetLocalPlayer() == null || !Input.GetKeyDown(spawnKey.Value))
                return;
            Dbgl("Spawn key pressed");


            var scene = ComponentManager<SceneLoader>.Value.loadedLandmarks.FirstOrDefault(s => s.go.name.Contains("#Landmark_Temperance#"))?.go;
            if(scene == null)
            {
                Dbgl("Temperance not found!");
                return;
            }
            var sms = scene.GetComponentInChildren<SnowmobileShed>();
            if(sms == null)
            {
                Dbgl("SnowmobileShed not found!");
                return;
            }
            var sm = AccessTools.FieldRefAccess<SnowmobileShed, Snowmobile>(sms, "snowmobilePrefab");

            Dbgl("Spawning snowmobile");

            var ssm = Instantiate<Snowmobile>(sm, ComponentManager<Raft_Network>.Value.GetLocalPlayer().FeetPosition + ComponentManager<Raft_Network>.Value.GetLocalPlayer().transform.forward * 2f + Vector3.up, Quaternion.identity, null);
            //var sfs = ssm.gameObject.AddComponent<WaterFloatSemih_Rigidbody>();
            //sfs.SetBody(AccessTools.FieldRefAccess<Snowmobile, Rigidbody>(ssm, "body"));
            ssm.ObjectIndex = SaveAndLoad.GetUniqueObjectIndex();
            ssm.BehaviourIndex = NetworkUpdateManager.GetUniqueBehaviourIndex();
            NetworkIDManager.AddNetworkID(ssm, typeof(Snowmobile));
            NetworkIDManager.AddNetworkIDTick(ssm);
            ssm.OnSnowmobileReset = (Action<Snowmobile, bool>)Delegate.Combine(ssm.OnSnowmobileReset, new Action<Snowmobile, bool>(BepInExPlugin.OnSnowmobileReset));
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
                    else if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand is float && (float)codes[i].operand == -1.5f)
                    {
                        Dbgl("adding method to change destroy depth");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetDestroyDepth))));
                        i++;
                    }
                }

                return codes.AsEnumerable();
            }
            public static void Postfix(Snowmobile __instance, Rigidbody ___body, bool ___grounded, Transform ___groundCheckPoint)
            {
                if (!modEnabled.Value)
                    return;
                if(__instance.DrivingPlayer == ComponentManager<Raft_Network>.Value.GetLocalPlayer())
                {
                    if (___grounded && jumpVelocity.Value > 0 && MyInput.GetButtonDown("Jump"))
                    {
                        Dbgl("Jumping");
                        ___body.velocity += Vector3.up * jumpVelocity.Value;
                    }
                    PlayerItemManager.IsBusy = false;
                }

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

        public static float GetDestroyDepth(float depth)
        {
            if (!modEnabled.Value)
                return depth;
            return destroyDepth.Value;
        }
    }
}
