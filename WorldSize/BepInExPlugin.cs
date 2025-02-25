using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace WorldSize
{
    [BepInPlugin("aedenthorn.WorldSize", "World Size", "0.1.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> worldSizeMult;

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
            worldSizeMult = Config.Bind<float>("Options", "WorldSizeMult", 10, "World size multiplier");

            ChunkManager.ChunkSize = (uint)Math.Round(ChunkManager.ChunkSize  * (double)worldSizeMult.Value);

            worldSizeMult.SettingChanged += WorldSizeMult_SettingChanged;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void WorldSizeMult_SettingChanged(object sender, EventArgs e)
        {
            ChunkManager.ChunkSize = (uint)Math.Round(ChunkManager.ChunkSize * (double)worldSizeMult.Value);
        }

        [HarmonyPatch(typeof(ChunkManager), nameof(ChunkManager.AddChunkPointForcibly))]
        public static class ChunkManager_AddChunkPointForcibly_Patch
        {
            public static void Postfix(ChunkManager __instance, ref ChunkPoint __result)
            {
                if (!modEnabled.Value)
                    return;
                __result.worldPosition *= worldSizeMult.Value;
                Dbgl($"spawning landmark at {__result.worldPosition}, {Vector3.Distance(__instance.RaftTransform.position, __result.worldPosition)}m from raft");
            }
        }
        [HarmonyPatch(typeof(ChunkManager), "Update")]
        public static class ChunkManager_Update_Patch
        {

            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling ChunkManager_Update");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Callvirt && codes[i].operand is MethodInfo && (MethodInfo)codes[i].operand == AccessTools.PropertyGetter(typeof(ChunkPoint), nameof(ChunkPoint.RemoveDistanceFromRaft)))
                    {
                        Dbgl("adding method to modify ChunkPoint.RemoveDistanceFromRaft");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.RemoveDistanceFromRaft))));
                        i++;
                    }
                }
                return codes.AsEnumerable();
            }
        }

        private static float RemoveDistanceFromRaft(float value)
        {
            if (modEnabled.Value)
            {
                value *= worldSizeMult.Value;
            }
            return value;
        }
    }
}
