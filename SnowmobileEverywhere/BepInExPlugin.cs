using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UIElements;
using static SO_TradingPost_Buyable;

namespace SnowmobileEverywhere
{
    [BepInPlugin("aedenthorn.SnowmobileEverywhere", "Snowmobile Everywhere", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

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

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
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
            public static void Postfix(Snowmobile __instance, Rigidbody ___body, Transform ___groundCheckPoint, float ___groundRayLength)
            {
                Raft raft = ComponentManager<Raft>.Value;
                if (raft is null)
                {
                    return;
                }
                RaycastHit raycastHit;
                if (Physics.Raycast(___groundCheckPoint.position, -___groundCheckPoint.up, out raycastHit, 1, LayerMasks.MASK_GroundMask_Raft))
                {
                    if (__instance.transform.parent == null)
                    {
                        Dbgl("Locking snowmobile");
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


                    //__instance.transform.position = new Vector3(__instance.transform.position.x, Mathf.Max(-1, Mathf.Clamp(__instance.transform.position.y, raycastHit.collider.transform.position.y, raycastHit.collider.transform.position.y + 0.2f)), __instance.transform.position.z);
                    //___body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    //___body.isKinematic = true;
                    //__instance.OnSnowmobileReset = new Action<Snowmobile, bool>(BepInExPlugin.OnSnowmobileReset);
                }
                else
                {
                    posDict.Remove(__instance.GetInstanceID());
                    if (__instance.transform.parent != null)
                    {
                        __instance.transform.SetParent(null);
                    }
                }
            }
        }

        public static void OnSnowmobileReset(Snowmobile snowmobile, bool viaWater)
        {
            Dbgl("Resetting snowmobile");
            snowmobile.transform.position = Vector3.up;
            snowmobile.transform.rotation = Quaternion.identity;
            snowmobile.transform.SetParent(SingletonGeneric<GameManager>.Singleton.lockedPivot);
        }

        public static LayerMask GetLayerMask(LayerMask mask)
        {
            if (!modEnabled.Value)
                return mask;
            return mask | LayerMasks.MASK_GroundMask;
        }
    }
}
