using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace QuickStore
{
    [BepInPlugin("aedenthorn.QuickStore", "Quick Store", "0.3.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> storeFromHotbar;
        public static ConfigEntry<bool> storeFromNets;
        public static ConfigEntry<bool> storeEggs;
        public static ConfigEntry<string> hotkey;
        public static ConfigEntry<string> disallowedItems;
        public static ConfigEntry<float> range;
        public static bool suppress;

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
            storeFromHotbar = Config.Bind<bool>("Options", "StoreFromHotbar", false, "Store items from the hotbar");
            storeFromNets = Config.Bind<bool>("Options", "StoreFromNets", true, "Store items caught in net blocks");
            storeEggs = Config.Bind<bool>("Options", "StoreEggs", true, "Store eggs laid by cluckers");
			hotkey = Config.Bind<string>("Options", "Hotkey", "k", "Hotkey to trigger quick store");
			range = Config.Bind<float>("Options", "Range", 10, "Range in metres from storage to allow quick store (-1 is infinite range)");
			disallowedItems = Config.Bind<string>("Options", "DisallowedItems", "", "List of items that will not be moved (comma-separated)");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
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
                var player = ComponentManager<Raft_Network>.Value.GetLocalPlayer();
                var pi = ComponentManager<PlayerInventory>.Value;
                InventoryPickup ip = AccessTools.FieldRefAccess<PlayerInventory, InventoryPickup>(pi, "inventoryPickup");
                foreach (var slot in pi.allSlots)
                {
                    if (slot.itemInstance is null || disallowedList.Contains(slot.itemInstance.UniqueName) || (!storeFromHotbar.Value && pi.hotbar.ContainsSlot(slot)))
                        continue;
                    string slotName = slot.itemInstance.UniqueName;
                    int originalAmount = slot.itemInstance.Amount;
                    foreach (Storage_Small s in StorageManager.allStorages)
                    {
                        if (s.IsOpen || range.Value >= 0 && Vector3.Distance(ComponentManager<Raft_Network>.Value.GetLocalPlayer().transform.position, s.transform.position) > range.Value)
                            continue;
                        var found = s.GetInventoryReference().GetItemCount(slotName);
                        if (found > 0)
                        {
                            Dbgl($"Found {slotName} x{found} in storage");

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
                            player.Network.RPC(new Message_Storage_Close(Messages.StorageManager_Close, player.StorageManager, s), Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                        }
                    }
                    if (slot.itemInstance is null || slot.itemInstance.Amount < originalAmount)
                    {
                        int remaining = (slot.itemInstance is null) ? 0 : slot.itemInstance.Amount;
                        Dbgl($"Stored {originalAmount - remaining} {slotName}");
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
                    if (ics == null)
                    {
                        Dbgl("no ItemCollector found");
                        return;
                    }
                    Dbgl($"Found {ics.Length} ItemCollectors");

                    foreach (var ic in ics)
                    {
                        for (int k = ic.collectedItems.Count - 1; k >= 0; k--)
                        {
                            PickupItem_Networked pin = ic.collectedItems[k];
                            if (pin?.gameObject.activeSelf != true || pin?.PickupItem?.yieldHandler?.Yield == null || pin.PickupItem.yieldHandler.Yield.Count == 0)
                                continue;
                            for (int i = pin.PickupItem.yieldHandler.Yield.Count - 1; i >= 0; i--)
                            {
                                if (pin.PickupItem.yieldHandler.Yield[i].item == null)
                                {
                                    continue;
                                }
                                int remain  = pin.PickupItem.yieldHandler.Yield[i].amount;
                                Dbgl($"Trying to store from net: {pin.PickupItem.yieldHandler.Yield[i].item.UniqueName} x{remain}");
                                foreach (Storage_Small s in StorageManager.allStorages)
                                {
                                    if (s.IsOpen || range.Value >= 0 && Vector3.Distance(ic.transform.position, s.transform.position) > range.Value)
                                        continue;
                                    if (s.GetInventoryReference().GetItemCount(pin.PickupItem.yieldHandler.Yield[i].item.UniqueName) > 0)
                                    {
                                        var newRemain = s.GetInventoryReference().AddItem(pin.PickupItem.yieldHandler.Yield[i].item.UniqueName, remain);
                                        if(newRemain != remain)
                                        {
                                            ip.ShowItem(pin.PickupItem.yieldHandler.Yield[i].item.UniqueName, remain - newRemain);
                                            Dbgl($"\tStored {pin.PickupItem.yieldHandler.Yield[i].item.UniqueName} x{remain - newRemain} from collector net, remain: {newRemain}/{pin.PickupItem.yieldHandler.Yield[i].amount}");
                                            remain = newRemain;
                                            player.Network.RPC(new Message_Storage_Close(Messages.StorageManager_Close, player.StorageManager, s), Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
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
                                        Dbgl($"\tSending {pin.PickupItem.yieldHandler.Yield[i].item.UniqueName} x{remain} left over to player inventory");

                                        pi.AddItem(pin.PickupItem.yieldHandler.Yield[i].item.UniqueName, remain);
                                    }
                                    PickupObjectManager.RemovePickupItem(pin);
                                    ic.collectedItems.RemoveAt(k);
                                    break;
                                }
                                else
                                {
                                    Dbgl($"\tall remain, skipping");
                                }

                            }
                        }
                    }
                }
                if (storeEggs.Value)
                {
                    var pns = SingletonGeneric<GameManager>.Singleton.lockedPivot.gameObject.GetComponentsInChildren<PickupItem_Networked>();
                    for(int i = 0; i < pns.Length; i++)
                    {
                        if (pns[i].PickupItem.yieldHandler != null && pns[i].PickupItem.yieldHandler.Yield.Any())
                        {
                            for (int j = 0; j < pns[i].PickupItem.yieldHandler.Yield.Count; j++)
                            {
                                if (pns[i].PickupItem.yieldHandler.Yield[j].item.UniqueName == "Egg")
                                {
                                    int remain = pns[i].PickupItem.yieldHandler.Yield[j].amount;
                                    foreach (Storage_Small s in StorageManager.allStorages)
                                    {
                                        if (s.IsOpen || range.Value >= 0 && Vector3.Distance(pns[i].transform.position, s.transform.position) > range.Value)
                                            continue;
                                        if (s.GetInventoryReference().GetItemCount(pns[i].PickupItem.yieldHandler.Yield[j].item.UniqueName) > 0)
                                        {
                                            var newRemain = s.GetInventoryReference().AddItem(pns[i].PickupItem.yieldHandler.Yield[j].item.UniqueName, remain);
                                            if (newRemain != remain)
                                            {
                                                ip.ShowItem(pns[i].PickupItem.yieldHandler.Yield[j].item.UniqueName, remain - newRemain);
                                                Dbgl($"Stored Egg, remain: {newRemain}/{pns[i].PickupItem.yieldHandler.Yield[j].amount}");
                                                remain = newRemain;
                                            }
                                            player.Network.RPC(new Message_Storage_Close(Messages.StorageManager_Close, player.StorageManager, s), Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                                        }
                                    }
                                    if (remain != pns[i].PickupItem.yieldHandler.Yield[j].amount)
                                    {
                                        Collider[] componentsInChildren = pns[i].GetComponentsInChildren<Collider>();
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
                                        WaterFloatSemih2 component = pns[i].GetComponent<WaterFloatSemih2>();
                                        if (component != null)
                                        {
                                            component.enabled = true;
                                        }
                                        if (remain != 0)
                                        {
                                            Dbgl($"\tSending {pns[i].PickupItem.yieldHandler.Yield[j].item.UniqueName} x{remain} left over to player inventory");

                                            pi.AddItem(pns[i].PickupItem.yieldHandler.Yield[j].item.UniqueName, remain);
                                        }
                                        PickupObjectManager.RemovePickupItem(pns[i]);
                                        break;
                                    }
                                    else
                                    {
                                        Dbgl($"\tall remain, skipping");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), new Type[] { typeof(ItemInstance), typeof(bool) })]
        public static class Inventory_AddItem_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling Inventory.AddItem");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (i < codes.Count - 1 && codes[i].opcode == OpCodes.Ldfld && codes[i].operand is FieldInfo && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(Inventory), "soundManager") && codes[i + 1].opcode == OpCodes.Ldnull)
                    {
                        Dbgl("adding check for slot is null");
                        codes.Insert(i + 4, codes[i+3].Clone());
                        codes.Insert(i + 4, codes[i+2].Clone());
                        codes.Insert(i + 4, codes[i+1].Clone());
                        codes.Insert(i + 4, new CodeInstruction(OpCodes.Ldloc_0));
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }
    }
}
