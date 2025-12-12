using BepInEx;
using BepInEx.Configuration;
using FMODUnity;
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
    [BepInPlugin("aedenthorn.QuickStore", "Quick Store", "0.3.2")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> storeFromHotbar;
        public static ConfigEntry<bool> storeFromNets;
        public static ConfigEntry<bool> storeEggs;
        public static ConfigEntry<bool> storeDropped;
        public static ConfigEntry<bool> storeHoneyComb;
        public static ConfigEntry<bool> storeGrown;
        public static ConfigEntry<bool> harvestTrees;
        public static ConfigEntry<bool> replantGrown;
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
            storeHoneyComb = Config.Bind<bool>("Options", "StoreHoneyComb", true, "Store honeycomb");
            storeGrown = Config.Bind<bool>("Options", "StoreGrown", true, "Store products grown in crop plots (except flowers near beehives)");
            harvestTrees = Config.Bind<bool>("Options", "HarvestTrees", true, "Auto cut down trees on the raft");
            replantGrown = Config.Bind<bool>("Options", "ReplantGrown", true, "Replant grown products being stored if possible");
            storeDropped = Config.Bind<bool>("Options", "StoreDropped", true, "Store items instead of dropping when inventory full");
			hotkey = Config.Bind<string>("Options", "Hotkey", "k", "Hotkey to trigger quick store");
			range = Config.Bind<float>("Options", "Range", 10, "Range in metres from storage to allow quick store (-1 is infinite range)");
			disallowedItems = Config.Bind<string>("Options", "DisallowedItems", "", "List of items that will not be moved (comma-separated)");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        public void Update()
        {
            if (!modEnabled.Value || CanvasHelper.ActiveMenu != MenuType.None || ComponentManager<PlayerInventory>.Value == null)
                return;
            if (AedenthornUtils.CheckKeyDown(hotkey.Value))
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
                        if (s.IsOpen || range.Value >= 0 && Vector3.Distance(player.transform.position, s.transform.position) > range.Value)
                            continue;
                        var found = s.GetInventoryReference().GetItemCount(slotName);
                        if (found > 0)
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
                            if (Raft_Network.IsHost)
                            {
                                player.Network.RPC(new Message_Storage_Close(Messages.StorageManager_Close, player.StorageManager, s), Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                            }
                            else
                            {
                                player.SendP2P(new Message_Storage_Close(Messages.StorageManager_Close, player.StorageManager, s), EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                            }
                        }
                    }
                    if (slot.itemInstance is null || slot.itemInstance.Amount < originalAmount)
                    {
                        int remaining = (slot.itemInstance is null) ? 0 : slot.itemInstance.Amount;
                        Dbgl($"Stored {originalAmount - remaining} {slotName}");
                        ip.ShowItem(slotName, remaining - originalAmount);
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
                        if (ic.collectedItems == null)
                            continue;
                        for (int k = ic.collectedItems.Count - 1; k >= 0; k--)
                        {
                            PickupItem_Networked pin = ic.collectedItems[k];
                            if (pin?.gameObject?.activeSelf != true || pin?.PickupItem?.yieldHandler?.Yield?.Count <= 0)
                                continue;
                            for (int i = pin.PickupItem.yieldHandler.Yield.Count - 1; i >= 0; i--)
                            {
                                if (pin.PickupItem.yieldHandler.Yield[i]?.item == null)
                                {
                                    continue;
                                }
                                int remain = pin.PickupItem.yieldHandler.Yield[i].amount;
                                int amount = pin.PickupItem.yieldHandler.Yield[i].amount;
                                Dbgl($"Trying to store from net: {pin.PickupItem.yieldHandler.Yield[i].item.UniqueName} x{remain}");
                                TryStore(player.transform.position, pin.PickupItem.yieldHandler.Yield[i].item, amount, ref remain);
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
                                    ic.collectedItems?.RemoveAt(k);
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
                    var pns = SingletonGeneric<GameManager>.Singleton?.lockedPivot?.gameObject?.GetComponentsInChildren<PickupItem_Networked>();
                    if (pns != null)
                    {

                        for (int i = 0; i < pns.Length; i++)
                        {
                            if (pns[i]?.PickupItem?.yieldHandler != null && pns[i].PickupItem.yieldHandler.Yield.Any())
                            {
                                for (int j = 0; j < pns[i].PickupItem.yieldHandler.Yield.Count; j++)
                                {
                                    if (pns[i].PickupItem.yieldHandler.Yield[j].item.UniqueName == "Egg")
                                    {
                                        int remain = pns[i].PickupItem.yieldHandler.Yield[j].amount;
                                        int amount = remain;
                                        var item = pns[i].PickupItem.yieldHandler.Yield[j].item;
                                        Dbgl($"Trying to store from net: {item.UniqueName} x{remain}");
                                        TryStore(player.transform.position, item, amount, ref remain);
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
                if (storeGrown.Value)
                {
                    var plots = FindObjectsOfType<Cropplot>().Where(c => !(c is Cropplot_Grass) && (range.Value < 0 || Vector3.Distance(c.transform.position, player.transform.position) <= range.Value));
                    var beehives = FindObjectsOfType<BeeHive>().ToList();
                    var uic = AccessTools.FieldRefAccess<Network_Player, PlayerItemManager>(player, "playerItemManager").useItemController;
                    var dict = AccessTools.FieldRefAccess<UseItemController, Dictionary<string, ItemConnection>>(uic, "connectionDictionary");
                    foreach (var plot in plots)
                    {
                        foreach (var slot in plot.GetSlots())
                        {
                            var plant = slot.plant;
                            if (plant == null)
                                continue;
                            if (!plant.FullyGrown() || !plant.harvestable || (!plant.playerCanHarvest && !harvestTrees.Value))
                                continue;
                            if (beehives.Any() && beehives[0].IsItemBaseFlower(plant.item) && beehives.Exists(b => Vector3.Distance(plot.transform.position, b.transform.position) < b.FlowerFindDistance))
                                continue;
                            bool replant = false;
                            if (plant?.pickupComponent.yieldHandler?.Yield?.Any() == true)
                            {
                                var yield = plant.pickupComponent.yieldHandler.Yield;
                                for (int j = 0; j < yield.Count; j++)
                                {
                                    var item = yield[j].item;
                                    int amount = yield[j].amount;
                                    int remain = amount;
                                    if (replantGrown.Value && plot.AcceptsPlantType(item) && dict.TryGetValue(item.UniqueName, out var ic))
                                    {
                                        PlantComponent pc = ic.obj.GetComponent<PlantComponent>();
                                        if (pc != null)
                                        {
                                            Dbgl($"Reserving {item.UniqueName} for replant");
                                            remain--;
                                            replant = true;
                                        }
                                    }
                                    if (remain > 0)
                                    {
                                        TryStore(plant.transform.position, item, amount, ref remain);
                                    }

                                    bool replantRandom = false;

                                    if (plant.pickupComponent.dropper != null)
                                    {
                                        var asset = AccessTools.FieldRefAccess<RandomDropper, SO_RandomDropper>(plant.pickupComponent.dropper, "randomDropperAsset");
                                        if(replantGrown.Value && !replant)
                                        {
                                            foreach (var i in asset.randomizer.items)
                                            {
                                                if (!(i.obj is Item_Base itemBase))
                                                    continue;
                                                if (plot.AcceptsPlantType(itemBase) && dict.TryGetValue(itemBase.UniqueName, out ic))
                                                {
                                                    PlantComponent pc = ic.obj.GetComponent<PlantComponent>();
                                                    if (pc != null)
                                                    {
                                                        Dbgl($"Using random seed drop {itemBase.UniqueName} for replant");
                                                        replantRandom = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        if (!replantRandom && (remain == 0 || remain + (replant ? 1 : 0) < amount))
                                        {
                                            Item_Base[] randomItems = plant.pickupComponent.dropper.GetRandomItems();
                                            foreach(var item2 in randomItems)
                                            {
                                                if (item2 is null)
                                                    continue;
                                                int remain2 = 1;
                                                Dbgl($"Trying to store random plant drop: {item2.UniqueName} x{1}");
                                                TryStore(player.transform.position, item2, 1, ref remain2);
                                                if(remain2 > 0)
                                                {
                                                    Dbgl($"\tSending {item2.UniqueName} x{remain2} left over random drop to player inventory");

                                                    pi.AddItem(item2.UniqueName, remain2);
                                                }
                                            }
                                        }
                                    }

                                    if (remain == 0 || remain + (replant ? 1 : 0) != amount)
                                    {
                                        RuntimeManager.PlayOneShot("event:/crafting/plant_harvest", plant.transform.position);
                                        plant.PullRoots();
                                        player.PlantManager.SendOnCropplotModified(plot, PlantManager.ObjectModification.PLANTREMOVED);
                                        if (remain != 0)
                                        {
                                            Dbgl($"\tSending {item.UniqueName} x{remain} left over to player inventory");

                                            pi.AddItem(item.UniqueName, remain);
                                        }
                                        PickupObjectManager.RemovePickupItem(plant.pickupComponent.networkID);
                                        if (replant || replantRandom)
                                        {
                                            Dbgl($"replanting {item.UniqueName}");
                                            Message_PlantSeed message_PlantSeed = new Message_PlantSeed(Messages.PlantManager_PlantSeed, player.PlantManager, plot, plant, false);
                                            if (Raft_Network.IsHost)
                                            {
                                                message_PlantSeed.plantObjectIndex = SaveAndLoad.GetUniqueObjectIndex();
                                                message_PlantSeed.waterPlantedSeed = ComponentManager<WeatherManager>.Value.GetCurrentWeatherType() == UniqueWeatherType.Rain;
                                                player.PlantManager.PlantSeed(plot, plant, message_PlantSeed.plantObjectIndex, message_PlantSeed.waterPlantedSeed, true);
                                                player.Network.RPC(message_PlantSeed, Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                                            }
                                            else
                                            {
                                                message_PlantSeed.plantObjectIndex = 0U;
                                                player.SendP2P(message_PlantSeed, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                                            }
                                        }
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
                if (storeHoneyComb.Value)
                {
                    var beehives = FindObjectsOfType<BeeHive>().Where(h => range.Value < 0 || Vector3.Distance(h.transform.position, player.transform.position) <= range.Value).ToList();
                    foreach (var hive in beehives)
                    {
                        if (hive.CurrentHoneyLevel == null)
                            continue;
                        if (hive.productionTimer != null)
                        {
                            hive.productionTimer.ResetTimer();
                        }
                        Beehive_HoneyLevel currentHoneyLevel = hive.CurrentHoneyLevel;
                        if (currentHoneyLevel != null)
                        {
                            for (int i = 0; i < currentHoneyLevel.yield.yieldAssets.Count; i++)
                            {

                                var item = currentHoneyLevel.yield.yieldAssets[i].item;
                                int amount = currentHoneyLevel.yield.yieldAssets[i].amount;
                                int remain = amount;

                                TryStore(player.transform.position, item, amount, ref remain);
                                if (remain != currentHoneyLevel.yield.yieldAssets[i].amount)
                                {
                                    if (remain != 0)
                                    {
                                        Dbgl($"\tSending {item.UniqueName} x{remain} left over to player inventory");

                                        pi.AddItem(item.UniqueName, remain);
                                    }
                                    hive.UpdateHoneyLevel(-1);
                                    AccessTools.Method(typeof(BeeHive), "RecheckBeeParticles").Invoke(hive, null);
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

        private void TryStore(Vector3 position, Item_Base item, int amount, ref int remain)
        {
            var player = ComponentManager<Raft_Network>.Value.GetLocalPlayer();
            var pi = ComponentManager<PlayerInventory>.Value;
            InventoryPickup ip = AccessTools.FieldRefAccess<PlayerInventory, InventoryPickup>(pi, "inventoryPickup");
            foreach (Storage_Small s in StorageManager.allStorages)
            {
                if (s.IsOpen || range.Value >= 0 && Vector3.Distance(position, s.transform.position) > range.Value)
                    continue;
                if (s.GetInventoryReference().GetItemCount(item.UniqueName) > 0)
                {
                    var newRemain = s.GetInventoryReference().AddItem(item.UniqueName, remain);
                    if (newRemain != remain)
                    {
                        ip.ShowItem(item.UniqueName, remain - newRemain);
                        Dbgl($"Stored {item.UniqueName}, remain: {newRemain}/{amount}");
                        remain = newRemain;
                        try
                        {
                            if (Raft_Network.IsHost)
                            {
                                player.Network.RPC(new Message_Storage_Close(Messages.StorageManager_Close, player.StorageManager, s), Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                            }
                            else
                            {
                                player.SendP2P(new Message_Storage_Close(Messages.StorageManager_Close, player.StorageManager, s), EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                            }

                        }
                        catch { }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InventoryPickupMenuItem), "SetItem")]
        public static class InventoryPickupMenuItem_SetItem_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling InventoryPickupMenuItem.SetItem");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand is string str && str == "+")
                    {
                        Dbgl("adding method to check for negative amount");
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.CheckNegative))));
                        codes.Insert(i + 1, new CodeInstruction(OpCodes.Ldarg_2));
                        break;
                    }
                }


                return codes.AsEnumerable();
            }
        }


        private static string CheckNegative(string str, int amount)
        {
            return amount < 0 ? "" : str;
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
        [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.DropItem), new Type[] { typeof(ItemInstance) })]
        public static class PlayerInventory_DropItem_Patch1
        {
            public static bool Prefix(PlayerInventory __instance, ref ItemInstance instance)
            {

                if (!modEnabled.Value || !storeDropped.Value)
                    return true;
                int original = instance.Amount;
                int remain = instance.Amount;
                InventoryPickup ip = AccessTools.FieldRefAccess<PlayerInventory, InventoryPickup>(__instance, "inventoryPickup");
                foreach (Storage_Small s in StorageManager.allStorages)
                {
                    if (s.IsOpen || range.Value >= 0 && Vector3.Distance(__instance.transform.position, s.transform.position) > range.Value)
                        continue;
                    if (s.GetInventoryReference().GetItemCount(instance.UniqueName) > 0)
                    {
                        s.GetInventoryReference().AddItem(instance, false);
                        var newRemain = instance.Amount;
                        if (newRemain != remain)
                        {
                            ip.ShowItem(instance.UniqueName, remain - newRemain);
                            Dbgl($"\tStored {instance.UniqueName} x{remain - newRemain} from dropped items, remain: {newRemain}/{original}");
                            remain = newRemain;
                            if (Raft_Network.IsHost)
                            {
                                __instance.hotbar.playerNetwork.Network.RPC(new Message_Storage_Close(Messages.StorageManager_Close, __instance.hotbar.playerNetwork.StorageManager, s), Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                            }
                            else
                            {
                                __instance.hotbar.playerNetwork.SendP2P(new Message_Storage_Close(Messages.StorageManager_Close, __instance.hotbar.playerNetwork.StorageManager, s), EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                            }
                            if (remain <= 0)
                                return false;
                        }
                        else
                        {
                            Dbgl($"\tall remain, skipping");
                        }
                    }
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(PlayerInventory), nameof(PlayerInventory.DropItem), new Type[] { typeof(Item_Base), typeof(int)})]
        public static class PlayerInventory_DropItem_Patch2
        {
            public static bool Prefix(PlayerInventory __instance, Item_Base item, ref int amount)
            {

                if (!modEnabled.Value || !storeDropped.Value)
                    return true;
                int original = amount;
                int remain = amount;
                InventoryPickup ip = AccessTools.FieldRefAccess<PlayerInventory, InventoryPickup>(__instance, "inventoryPickup");
                foreach (Storage_Small s in StorageManager.allStorages)
                {
                    if (s.IsOpen || range.Value >= 0 && Vector3.Distance(__instance.transform.position, s.transform.position) > range.Value)
                        continue;
                    if (s.GetInventoryReference().GetItemCount(item.UniqueName) > 0)
                    {
                        int newRemain = s.GetInventoryReference().AddItem(item.UniqueName, remain);
                        if (newRemain != remain)
                        {
                            ip.ShowItem(item.UniqueName, remain - newRemain);
                            Dbgl($"\tStored {item.UniqueName} x{remain - newRemain} from dropped items, remain: {newRemain}/{original}");
                            remain = newRemain;
                            if (Raft_Network.IsHost)
                            {
                                __instance.hotbar.playerNetwork.Network.RPC(new Message_Storage_Close(Messages.StorageManager_Close, __instance.hotbar.playerNetwork.StorageManager, s), Target.Other, EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                            }
                            else
                            {
                                __instance.hotbar.playerNetwork.SendP2P(new Message_Storage_Close(Messages.StorageManager_Close, __instance.hotbar.playerNetwork.StorageManager, s), EP2PSend.k_EP2PSendReliable, NetworkChannel.Channel_Game);
                            }
                            if (remain <= 0)
                                return false;
                        }
                        else
                        {
                            Dbgl($"\tall remain, skipping");
                        }
                    }
                }
                return true;
            }
        }

    }
}
