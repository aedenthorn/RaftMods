using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UltimateWater.Utils;
using UnityEngine;

namespace ShowRange
{
    [BepInPlugin("aedenthorn.ShowRange", "Show Range", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public enum ShowType 
        { 
            Both,
            Scarecrow,
            BeeHive
        }

        public static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<float> scarecrowRange;
        public static ConfigEntry<float> beehiveRange;
        public static ConfigEntry<float> plotMarkerRadius;
        public static ConfigEntry<KeyCode> showKey;
        public static ConfigEntry<KeyCode> cycleKey;
        public static ConfigEntry<ShowType> showType;
        public static ConfigEntry<bool> holdToShow;
        public static ConfigEntry<Color> scarecrowColor;
        public static ConfigEntry<Color> beeHiveColor;
        public static ConfigEntry<float> colorChangeRate;
        public static bool on = false;

        public static string markerName = "RangeMarker";
        public static string numberName = "FlowerCount";

        public static List<TextMeshPro> numbers = new List<TextMeshPro>();
        public static List<GameObject> markers = new List<GameObject>();

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
            showKey = Config.Bind<KeyCode>("Options", "ShowKey", KeyCode.RightShift, "Key to show ranges");
            cycleKey = Config.Bind<KeyCode>("Options", "CycleKey", KeyCode.Insert, "Key to cycle through show types");
            showType = Config.Bind<ShowType>("Options", "ShowType", ShowType.Both, "Which type of range to show");
            holdToShow = Config.Bind<bool>("Options", "HoldToShow", true, "Hold to show?");
            scarecrowRange = Config.Bind<float>("Options", "ScarecrowRange", 6, "Scarecrow Range");
            beehiveRange = Config.Bind<float>("Options", "BeehiveRange", 6, "Beehive Range");
            plotMarkerRadius = Config.Bind<float>("Options", "PlotMarkerRadius", 0.1f, "Plot marker radius");
            scarecrowColor = Config.Bind<Color>("Options", "ScarecrowColor", new Color(0, 0, 1f, 0.2f), "Scarecrow range color");
            beeHiveColor = Config.Bind<Color>("Options", "BeeHiveColor", new Color(1f, 1f, 0, 0.2f), "Bee hive range color");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
        }
        public void Update()
        {
            if (CanvasHelper.ActiveMenu != MenuType.None)
                return;

            if (numbers.Count > 0)
            {
                for (int i = numbers.Count - 1; i >= 0; i--)
                {
                    if (numbers[i]?.IsDestroyed() != false)
                    {
                        numbers.RemoveAt(i);
                        continue;
                    }
                    numbers[i].transform.rotation = Quaternion.LookRotation(numbers[i].transform.position - ComponentManager<Raft_Network>.Value.GetLocalPlayer().transform.position);
                }
            }

            if (Input.GetKeyDown(cycleKey.Value))
            {
                showType.Value = (ShowType)((int)(showType.Value + 1) % Enum.GetValues(typeof(ShowType)).Length);
            }
            else if (!holdToShow.Value && Input.GetKeyDown(showKey.Value))
            {
                Dbgl("Pressed key");
                on = !on;
            }
            else if (holdToShow.Value)
            {
                if (on && !Input.GetKey(showKey.Value))
                {
                    Dbgl("released key");
                    on = false;
                }
                else if (!on && Input.GetKey(showKey.Value))
                {
                    Dbgl("Pressed key");
                    on = true;
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
            ToggleAllMarkers();
        }

        public static void ToggleAllMarkers()
        {
            if (!modEnabled.Value || !Raft_Network.IsHost)
                return;
            Dbgl("Toggling all");
            if (markers.Any() && (!on || showType.Value != ShowType.Both))
            {
                for (int i = markers.Count - 1; i >= 0; i--)
                {
                    if (!on || DoNotShow(markers[i]))
                    {
                        Destroy(markers[i]);
                        markers.RemoveAt(i);
                    }
                }
            }
            if(numbers.Any() && (!on || showType.Value == ShowType.Scarecrow))
            { 
                foreach(var n in numbers)
                {
                    Destroy(n);
                }
                numbers.Clear();
            }
            if (on)
            {
                if(showType.Value != ShowType.BeeHive)
                {
                    foreach (var behaviour in FindObjectsOfType<Scarecrow>())
                    {
                        CreateMarkers(behaviour, scarecrowRange.Value, scarecrowColor.Value);
                    }
                }
                if (showType.Value != ShowType.Scarecrow)
                {
                    foreach (var n in numbers)
                    {
                        Destroy(n);
                    }
                    numbers.Clear();
                    foreach (var behaviour in FindObjectsOfType<BeeHive>())
                    {
                        CreateMarkers(behaviour, beehiveRange.Value, beeHiveColor.Value);
                    }
                }
            }
        }

        public static void CreateMarkers(MonoBehaviour behaviour, float range, Color color)
        {
            if (behaviour is Scarecrow)
            {
                foreach (var plot in FindObjectsOfType<Cropplot>())
                {
                    if (plot is Cropplot_Grass)
                        continue;

                    if (IsScarecrowNearPlot(behaviour, plot))
                    {
                        var pm = CreateMarker(plot, plotMarkerRadius.Value, color);
                        pm.transform.localPosition = new Vector3(0, behaviour is Scarecrow ? 1 : 1 + plotMarkerRadius.Value * 2, 0);
                    }
                }
            }

            CreateMarker(behaviour, range, color);
        }

        private static bool DoNotShow(GameObject obj)
        {
            return (obj.GetComponentInParent<BeeHive>() != null && showType.Value == ShowType.Scarecrow) || (obj.GetComponentInParent<Scarecrow>() != null && showType.Value == ShowType.BeeHive);
        }

        private static bool IsScarecrowNearPlot(MonoBehaviour s, Cropplot cropplot)
        {
            Vector3 vector = cropplot.mainCollider.transform.TransformPoint(cropplot.mainCollider.center);
            return Physics.OverlapSphere(vector, scarecrowRange.Value, 1838080).Contains((s as Scarecrow).mainCollider);
        }

        private static GameObject CreateMarker(MonoBehaviour behaviour, float range, Color color)
        {
            var t = behaviour.transform.Find(markerName);
            if (t != null)
            {
                DestroyImmediate(t.gameObject);
            }
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.GetComponent<Collider>().enabled = false;
            var mat = marker.GetComponent<MeshRenderer>().material;
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            marker.GetComponent<MeshRenderer>().material.color = color;
            marker.transform.SetParent(behaviour.transform, false);
            marker.transform.localScale = Vector3.one * range * 2;
            marker.name = markerName;
            markers.Add(marker);
            t = behaviour.transform.Find(numberName);
            if (t != null)
            {
                DestroyImmediate(t.gameObject);
            }
            if (behaviour is BeeHive hive)
            {
                int number = (int)AccessTools.Method(typeof(BeeHive), "GetWateredFlowerPlantationSlotCount").Invoke(hive, null);
                if(number > 0)
                {
                    Dbgl($"Number of flowers: {number}");
                    TextMeshPro newText = new GameObject().AddComponent<TextMeshPro>();
                    newText.text = number.ToString();
                    newText.fontSize = 4;
                    newText.alignment = TextAlignmentOptions.Center;
                    newText.color = Color.white;
                    newText.fontStyle = FontStyles.Bold;
                    newText.transform.SetParent(behaviour.transform, false);
                    newText.name = numberName;
                    newText.rectTransform.anchoredPosition3D = new Vector3(0, 1.5f, 0);
                    newText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    newText.transform.rotation = Quaternion.LookRotation(newText.transform.position - ComponentManager<Raft_Network>.Value.GetLocalPlayer().transform.position);
                    numbers.Add(newText);
                }
            }
            return marker;
        }

        [HarmonyPatch(typeof(Seagull), "GetScarecrowInVicinity")]
        public static class Seagull_GetScarecrowInVicinity_Patch
        {
            public static void Prefix(ref float radius)
            {
                if (!modEnabled.Value)
                    return;
                radius = scarecrowRange.Value;
            }
        }

        [HarmonyPatch(typeof(BeeHive), nameof(BeeHive.FlowerFindDistance))]
        [HarmonyPatch(MethodType.Getter)]
        public static class BeeHive_FlowerFindDistance_Patch
        {
            public static void Postfix(ref float __result)
            {
                if (!modEnabled.Value)
                    return;
                __result = beehiveRange.Value;
            }
        }

        [HarmonyPatch(typeof(BlockCreator), "RemoveBlockCoroutine")]
        public static class BlockCreator_RemoveBlockCoroutine_Patch
        {
            public static void Postfix(Block block)
            {
                if (!modEnabled.Value || !on || block?.buildableItem?.settings_buildable?.Placeable != true)
                    return;
                ToggleAllMarkers();
            }
        }
        [HarmonyPatch(typeof(BlockCreator), nameof(BlockCreator.CreateBlock))]
        public static class BlockCreator_CreateBlock_Patch
        {
            public static void Postfix(Item_Base blockItem)
            {
                if (!modEnabled.Value || !on || blockItem?.settings_buildable?.Placeable != true)
                    return;
                ToggleAllMarkers();
            }
        }
    }
}
