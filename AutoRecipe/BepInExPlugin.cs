using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace AutoRecipe
{
    [BepInPlugin("aedenthorn.AutoRecipe", "Auto Recipe", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<KeyCode> cookModKey;
        public static ConfigEntry<string> cookInteractText;
        public static ConfigEntry<float> storageRange;

        public static bool showingInteract = false;

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
            cookModKey = Config.Bind<KeyCode>("Options", "ModKey", KeyCode.LeftShift, "Mod key to hold to enable auto crafting");
            cookInteractText = Config.Bind<string>("Options", "InteractText", "Craft", "Interact text");
            storageRange = Config.Bind<float>("General", "StorageRange", -1, "Range in meters; set to negative for no range limit.");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }



        [HarmonyPatch(typeof(RemovePlaceables), "Update")]
        public static class RemovePlaceables_Update_Patch
        {
            public static void Postfix(RemovePlaceables __instance, Block ___currentBlock, CanvasHelper ___canvas)
            {
                if (!modEnabled.Value || (cookModKey.Value != KeyCode.None && !Input.GetKey(cookModKey.Value)))
                {
                    if (showingInteract)
                    {
                        ___canvas.displayTextManager.HideDisplayTexts(0);
                        showingInteract = false;
                    }
                    return;
                }

                RaycastHit raycastHit;
                if (!Helper.HitAtCursor(out raycastHit, 5f, LayerMasks.MASK_Block, QueryTriggerInteraction.UseGlobal))
                {
                    if (showingInteract)
                    {
                        ___canvas.displayTextManager.HideDisplayTexts(0);
                        showingInteract = false;
                    }
                    return;
                }
                var recipe = raycastHit.transform.GetComponentInParent<CookingTable_Recipe_UI>();
                if (recipe == null)
                {
                    if (showingInteract)
                    {
                        ___canvas.displayTextManager.HideDisplayTexts(0);
                        showingInteract = false;
                    }
                    return;
                }
                if (!showingInteract)
                {
                    ___canvas.displayTextManager.ShowText(cookInteractText.Value, MyInput.Keybinds["Interact"].MainKey, 0, 0, false);
                    showingInteract = true;
                }
                if (___currentBlock != null)
                    return;

                if (MyInput.GetButtonDown("Interact"))
                {
                    CookingTable station = null;
                    if (recipe.Recipe.RecipeType == CookingRecipeType.CookingPot)
                    {
                        station = FindObjectOfType<CookingTable_Pot>();
                    }
                    else if (recipe.Recipe.RecipeType == CookingRecipeType.Juicer)
                    {
                        station = FindObjectOfType<CookingTable_Juicer>();
                    }
                    if (station == null)
                    {
                        Dbgl($"No {recipe.Recipe.RecipeType} found");
                        return;
                    }
                    PlayerInventory pi = ComponentManager<Network_Player>.Value.Inventory;
                    var sprite = AccessTools.FieldRefAccess<CookingTable_Recipe_UI, Image>(recipe, "recipeImage").sprite;
                    foreach (var cm in recipe.Recipe.RecipeCost)
                    {
                        if (!cm.HasEnoughInInventory(pi))
                        {
                            (ComponentManager<NotificationManager>.Value.ShowNotification("QuestItem") as Notification_QuestItem).infoQue.Enqueue(new Notification_QuestItem_Info($"Not enough {string.Join("/", cm.items.Select(i => i.UniqueName))}", cm.amount, sprite));

                            Dbgl($"Not enough {string.Join("/", cm.items.Select(i => i.UniqueName))}");
                            return;
                        }
                    }
                    foreach (var slot in station.Slots)
                    {
                        if (slot.HasItem)
                        {
                            Dbgl($"Slot has {slot.CurrentItem.UniqueName}, aborting");
                            return;
                        }
                    }
                    var costs = new List<CostMultiple>();
                    foreach (var cm in recipe.Recipe.RecipeCost)
                    {
                        Dbgl($"cost multiple: {string.Join("/", cm.items.Select(i => i.UniqueName))} x{cm.amount}");

                        int amountAdded = 0;
                        while (amountAdded < cm.amount)
                        {
                            int most = 0;
                            Item_Base mostItem = null;
                            foreach (var item in cm.items)
                            {
                                var amount = pi.GetItemCount(item);
                                foreach (Storage_Small s in GetStorages())
                                {
                                    amount += s.GetInventoryReference().GetItemCount(item);
                                }
                                if (most < amount)
                                {
                                    most = amount;
                                    mostItem = item;
                                }
                            }
                            var amountToAdd = Mathf.Min(cm.amount - amountAdded, most);
                            costs.Add(new CostMultiple(new Item_Base[] { mostItem }, amountToAdd));
                            Dbgl($"Adding {mostItem.UniqueName} x{amountToAdd}");

                            for (int i = 0; i < amountToAdd; i++)
                            {
                                for (int j = 0; j < station.Slots.Length; j++)
                                {
                                    if (!station.Slots[j].HasItem)
                                    {
                                        Dbgl($"Placing {mostItem.UniqueName} in slot {j}");
                                        AccessTools.Method(typeof(CookingTable), "OnSlotInsertItem").Invoke(station, new object[] { null, station.Slots[j], new ItemInstance(mostItem, 1, mostItem.MaxUses) });
                                        break;
                                    }
                                }
                            }
                            amountAdded += amountToAdd;
                        }
                    }
                    Dbgl($"cooking {recipe.Recipe.Result.UniqueName}: {string.Join(", ", station.Slots.Select(s => s.CurrentItem?.UniqueName))}");

                    AccessTools.Method(typeof(CookingTable), "HandleStartCooking").Invoke(station, new object[] { });
                    pi.RemoveCostMultiple(costs.ToArray());
                }
            }
        }

        public static List<Storage_Small> GetStorages()
        {
            List<Storage_Small> list = new List<Storage_Small>();
            foreach (Storage_Small s in StorageManager.allStorages)
            {
                if (storageRange.Value >= 0 && Vector3.Distance(ComponentManager<Raft_Network>.Value.GetLocalPlayer().transform.position, s.transform.position) > storageRange.Value)
                    continue;
                list.Add(s);
            }
            return list;
        }
    }
}
