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
    [BepInPlugin("aedenthorn.WorldSize", "World Size", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> worldSizeMult;

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
            worldSizeMult = Config.Bind<float>("Options", "WorldSizeMult", 10, "World size multiplier");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(SO_ChunkSpawnRuleAsset), nameof(SO_ChunkSpawnRuleAsset.SpawnDistanceFromRaft))]
        [HarmonyPatch(MethodType.Getter)]
        public static class SO_ChunkSpawnRuleAsset_SpawnDistanceFromRaft_Patch
        {
            public static void Postfix(ref Interval_Float __result)
            {
                if (!modEnabled.Value)
                    return;
                __result.minValue *= worldSizeMult.Value;
                __result.maxValue *= worldSizeMult.Value;
                Dbgl($"spawning landmark between {__result.minValue} and {__result.maxValue}");
            }
        }
    }
}
