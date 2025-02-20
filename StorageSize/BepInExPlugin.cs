using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace StorageSize
{
    [BepInPlugin("aedenthorn.StorageSize", "Storage Size", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static readonly bool isDebug = true;
        public static BepInExPlugin context;

        public static ConfigEntry<float> storageMult;
        public static ConfigEntry<bool> modEnabled;

        public static double lastTime = 1;
        public static bool pausedMenu = false; 
        public static bool wasActive = false; 

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        } 
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
            storageMult = Config.Bind<float>("General", "StorageMult", 2f, "Storage size multiplier");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public void Update()
        {
        }
        [HarmonyPatch(typeof(Inventory), "Awake")]
        static class Inventory_Patch
        {
            static void Prefix(Inventory __instance)
            {
                if (!modEnabled.Value)
                    return;
                Slot[] slots = __instance.GetComponentsInChildren<Slot>();
                List<Slot> slotsList = new List<Slot>();
                Dbgl($"inventory {__instance.name} slots: {slots.Length}");
                int total = Mathf.RoundToInt(slots.Length * storageMult.Value);
                for (int i = 0; i < total; i++)
                {
                    Slot slot;
                    if (i >= slots.Length)
                    {
                        slot = Instantiate(slots[0].gameObject, slots[0].transform.parent).GetComponent<Slot>();
                        Dbgl($"added slot {i}");
                    }
                    else
                        slot = slots[i];

                    slot.name = "Slot_Inventory" + (i == 0 ? "" : $" ({i})");
                }
            }
        }
    }
}
