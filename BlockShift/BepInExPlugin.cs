using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace BlockShift
{
    [BepInPlugin("aedenthorn.BlockShift", "Block Shift", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<KeyCode> keyShiftForward;
        public static ConfigEntry<KeyCode> keyShiftBack;
        public static ConfigEntry<KeyCode> keyShiftLeft;
        public static ConfigEntry<KeyCode> keyShiftRight;
        public static ConfigEntry<KeyCode> keyShiftCenter;

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
            keyShiftForward = Config.Bind<KeyCode>("Options", "KeyShiftForward", KeyCode.Keypad8, "Key to shift blocks forward");
            keyShiftBack = Config.Bind<KeyCode>("Options", "KeyShiftBack", KeyCode.Keypad2, "Key to shift blocks back");
            keyShiftLeft = Config.Bind<KeyCode>("Options", "KeyShiftLeft", KeyCode.Keypad4, "Key to shift blocks right");
            keyShiftRight = Config.Bind<KeyCode>("Options", "KeyShiftRight", KeyCode.Keypad6, "Key to shift blocks left");
            keyShiftCenter = Config.Bind<KeyCode>("Options", "KeyShiftCenter", KeyCode.Keypad5, "Key to shift blocks to center");


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Hammer), "Update")]
        public static class Hammer_Update_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;
                if (Input.GetKeyDown(keyShiftForward.Value))
                {
                    ShiftBlocks(0, 1);
                }
                else if (Input.GetKeyDown(keyShiftBack.Value))
                {
                    ShiftBlocks(0, -1);
                }
                else if (Input.GetKeyDown(keyShiftLeft.Value))
                {
                    ShiftBlocks(-1, 0);
                }
                else if (Input.GetKeyDown(keyShiftRight.Value))
                {
                    ShiftBlocks(1, 0);
                }
                else if (Input.GetKeyDown(keyShiftCenter.Value))
                {
                    ShiftBlocks(0, 0);
                }
            }

            private static void ShiftBlocks(int x, int z)
            {
                List<Block> placedBlocks = BlockCreator.GetPlacedBlocks();
                Vector3 shift = new Vector3(1.5f * x, 0, 1.5f * z);
                if(x == 0 && z == 0)
                {
                    float minX = 0;
                    float maxX = 0;
                    float minZ = 0;
                    float maxZ = 0;
                    foreach (var block in placedBlocks.Where(b => b.buildableItem.UniqueName.Contains("Foundation")))
                    {
                        if(minX > block.transform.localPosition.x)
                        {
                            minX = block.transform.localPosition.x;
                        }
                        if(minZ > block.transform.localPosition.z)
                        {
                            minZ = block.transform.localPosition.z;
                        }
                        if(maxX < block.transform.localPosition.x)
                        {
                            maxX = block.transform.localPosition.x;
                        }
                        if(maxZ < block.transform.localPosition.z)
                        {
                            maxZ = block.transform.localPosition.z;
                        }
                    }
                    int width = Mathf.RoundToInt((maxX - minX) / 1.5f) + 1;
                    int height = Mathf.RoundToInt((maxZ - minZ) / 1.5f) + 1;
                    float targetX = width / 2 * 1.5f - 0.75f;
                    float targetZ = height / 2 * 1.5f - 0.75f;
                    shift = new Vector3(targetX - maxX, 0, targetZ - maxZ);
                    Dbgl($"min {minX},{minZ}; max {maxX},{maxZ}; dim {width},{height}; target {targetX},{targetZ}; shift {shift}");

                }
                Dbgl($"shifting by {shift}");

                foreach (Transform e in SingletonGeneric<GameManager>.Singleton.lockedPivot)
                {
                    if (e.GetComponent<Block>() != null)
                        continue;
                    if(e.GetComponent<Light>() != null || e.GetComponent<Network_Entity>() != null)
                    {
                        e.localPosition += shift;
                    }
                }
                ComponentManager<RaftBounds>.Value.ClearWalkableBlocks();
                AccessTools.FieldRefAccess<RaftCollisionManager, List<Vector3>>(ComponentManager<RaftCollisionManager>.Value, "blocks").Clear();
                for (int i = 0; i < placedBlocks.Count; i++)
                {
                    placedBlocks[i].transform.localPosition += shift;
                    if (placedBlocks[i].blockColliders.Length != 0)
                    {
                        ComponentManager<BlockCollisionConsolidator>.Value.RemoveBlock(placedBlocks[i]);
                        ComponentManager<BlockCollisionConsolidator>.Value.AddBlock(placedBlocks[i]);
                    }
                    BlockCreator.PlaceBlockCallStack(placedBlocks[i], null, true, -1);
                }
                ComponentManager<RaftBounds>.Value.Initialize();
                ComponentManager<RaftCollisionManager>.Value.Initialize();
            }
        }
    }
}
