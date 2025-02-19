using BepInEx;
using BepInEx.Configuration;
using FMODUnity;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UltimateWater;
using UnityEngine;
using System.Linq;
using System.Reflection.Emit;
using System.Collections;

namespace QuickStore
{
    [BepInPlugin("aedenthorn.QuickStore", "Quick Store", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> storeFromHotbar;
        public static ConfigEntry<bool> storeFromNets;
        public static ConfigEntry<string> hotkey;
        public static ConfigEntry<string> disallowedItems;
        public static ConfigEntry<float> range;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        } 
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");
            storeFromHotbar = Config.Bind<bool>("Options", "StoreFromHotbar", false, "Store items from the hotbar");
            storeFromNets = Config.Bind<bool>("Options", "StoreFromNets", true, "Store items caught in net blocks");
			hotkey = Config.Bind<string>("Options", "Hotkey", "k", "Hotkey to trigger quick store");
			range = Config.Bind<float>("Options", "Range", 10, "Range in metres from storage to allow quick store (-1 is infinite range)");
			disallowedItems = Config.Bind<string>("Options", "DisallowedItems", "", "List of items that will not be moved (comma-separated)");

            //Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public static bool running = false;
        public void Update()
        {
            if (!modEnabled.Value || ComponentManager<PlayerInventory>.Value == null)
                return;
            if (!running && AedenthornUtils.CheckKeyDown(hotkey.Value))
            {
                Dbgl("Quick store");
                var disallowedList = disallowedItems.Value.Split(',').ToList();
                var pi = ComponentManager<PlayerInventory>.Value;
                foreach (var slot in pi.allSlots)
                {
                    if (slot.itemInstance is null || disallowedList.Contains(slot.itemInstance.UniqueName) || (!storeFromHotbar.Value && pi.hotbar.ContainsSlot(slot)))
                        continue;
                    string slotName = slot.itemInstance.UniqueName;
                    int originalAmount = slot.itemInstance.Amount;
                    foreach (Storage_Small s in StorageManager.allStorages)
                    {
                        if (range.Value >= 0 && Vector3.Distance(ComponentManager<Raft_Network>.Value.GetLocalPlayer().transform.position, s.transform.position) > range.Value)
                            continue;
                        if (s.GetInventoryReference().GetItemCount(slotName) > 0)
                        {
                            s.GetInventoryReference().AddItem(slot.itemInstance, false);
                            if (slot.itemInstance.Amount == 0)
                            {
                                slot.SetItem(null);
                                if (pi.hotbar.IsSelectedHotSlot(slot))
                                {
                                    pi.hotbar.ReselectCurrentSlot();
                                }
                                break;
                            }
                            else
                            {
                                slot.RefreshComponents();
                            }
                        }
                    }
                    if (slot.itemInstance is null || slot.itemInstance.Amount < originalAmount)
                    {
                        int remaining = (slot.itemInstance is null) ? 0 : slot.itemInstance.Amount;
                        Dbgl($"Stored {originalAmount - remaining} {slotName}");
                        var ip = FindObjectOfType<InventoryPickup>();
                        //ip.ShowItem(slotName, originalAmount - remaining);
                        //continue;
                        InventoryPickupMenuItem firstItem = (InventoryPickupMenuItem)AccessTools.Method(typeof(InventoryPickup), "GetFirstItem").Invoke(ip, new object[] { });
                        Item_Base itemByName = ItemManager.GetItemByName(slotName);
                        if (firstItem == null || itemByName == null)
                        {
                            Dbgl($"firstItem {firstItem != null}, itemByName {itemByName != null}");
                            break;
                        }
                        foreach (InventoryPickupMenuItem inventoryPickupMenuItem in AccessTools.FieldRefAccess<InventoryPickup, List<InventoryPickupMenuItem>>(ip, "items"))
                        {
                            if (inventoryPickupMenuItem.gameObject.activeInHierarchy)
                            {
                                inventoryPickupMenuItem.index++;
                            }
                        }
                        firstItem.rect.localPosition = new Vector3(0f, -2f * firstItem.rect.sizeDelta.y, 0f);
                        firstItem.gameObject.SetActive(true);
                        firstItem.amountTextComponent.text = "-" + (originalAmount - remaining).ToString();
                        firstItem.nameTextComponent.text = itemByName.settings_Inventory.DisplayName;
                        firstItem.imageComponent.sprite = itemByName.settings_Inventory.Sprite;
                        AccessTools.FieldRefAccess<InventoryPickupMenuItem, CanvasGroup>(firstItem, "canvasGroup").alpha = 1f;
                        AccessTools.FieldRefAccess<InventoryPickupMenuItem, bool>(firstItem, "fade") = false;
                        firstItem.Invoke("StartFade", 3.5f);
                        firstItem.index = 0;
                    }

                }
                if (storeFromNets.Value)
                {
                    var ics = FindObjectsOfType<ItemCollector>();
                    foreach (var ic in ics)
                    {
                        for (int k = ic.collectedItems.Count - 1; k >= 0; k--)
                        {
                            PickupItem_Networked pin = ic.collectedItems[k];
                            if (pin?.gameObject.activeSelf != true || pin?.PickupItem?.yieldHandler == null || pin.PickupItem.yieldHandler.Yield.Count == 0)
                                continue;
                            for (int i = pin.PickupItem.yieldHandler.Yield.Count - 1; i >= 0; i--)
                            {
                                if (pin.PickupItem.yieldHandler.Yield[i].item == null)
                                    continue;
                                int remain  = pin.PickupItem.yieldHandler.Yield[i].amount;
                                foreach (Storage_Small s in StorageManager.allStorages)
                                {
                                    if (range.Value >= 0 && Vector3.Distance(ic.transform.position, s.transform.position) > range.Value)
                                        continue;
                                    if (s.GetInventoryReference().GetItemCount(pin.PickupItem.yieldHandler.Yield[i].item.UniqueName) > 0)
                                    {
                                        var newRemain = s.GetInventoryReference().AddItem(pin.PickupItem.yieldHandler.Yield[i].item.UniqueName, remain);
                                        if(newRemain != remain)
                                        {
                                            Dbgl($"Put {pin.PickupItem.yieldHandler.Yield[i].item.UniqueName} x{remain - newRemain} in storage, {newRemain} remaining");
                                            remain = newRemain;
                                        }
                                    }
                                }
                                if (remain != pin.PickupItem.yieldHandler.Yield[i].amount)
                                {
                                    Collider[] componentsInChildren = pin.GetComponentsInChildren<Collider>();
                                    if (componentsInChildren != null)
                                    {
                                        foreach (Collider collider in componentsInChildren)
                                        {
                                            if (!(collider == null))
                                            {
                                                collider.enabled = true;
                                            }
                                        }
                                    }
                                    WaterFloatSemih2 component = pin.GetComponent<WaterFloatSemih2>();
                                    if (component != null)
                                    {
                                        component.enabled = true;
                                    }
                                    if (remain != 0)
                                    {
                                        Dbgl($"Sending {pin.PickupItem.yieldHandler.Yield[i].item.UniqueName} x{remain} to player inventory");

                                        pi.AddItem(pin.PickupItem.yieldHandler.Yield[i].item.UniqueName, remain);
                                    }
                                    PickupObjectManager.RemovePickupItem(pin);
                                    ic.collectedItems.RemoveAt(k);
                                    break;
                                }

                            }
                        }
                    }
                }
            }
        }
    }
}
