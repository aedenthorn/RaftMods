using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace TreasureMod
{
    [BepInPlugin("aedenthorn.TreasureMod", "Treasure Mod", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

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
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }



        [HarmonyPatch(typeof(PickupObjectManager), nameof(PickupObjectManager.RemovePickupItem), new Type[] { typeof(PickupItem_Networked), typeof(CSteamID) })]
        public static class PickupObjectManager_RemovePickupItem_Patch
        {
            public static void Prefix(ref PickupItem_Networked pickupNetwork, CSteamID pickupPlayerID)
            {
                if (!modEnabled.Value || pickupNetwork == null || (Raft_Network.IsHost && !pickupNetwork.CanBePickedUp()))
                    return;
                var treasureName = pickupNetwork.name.Replace("(Clone)", "");

                var filePath = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "settings.json");
                if (!File.Exists(filePath))
                    return;
                JsonTreasureDocument document = null;
                try
                {
                    document = JsonSerializer.Deserialize<JsonTreasureDocument>(File.ReadAllText(filePath), new JsonSerializerOptions() { IncludeFields = true });
                }
                catch(Exception ex)
                {
                    Dbgl($"Error reading json file:\n{ex.Message}");
                    return;
                }
                if (document == null)
                    return;
                if (!document.treasures.TryGetValue(treasureName, out var treasure))
                {
                    foreach(var kvp in document.treasures)
                    {
                        if (kvp.Value.objects != null && kvp.Value.objects.TryGetValue(treasureName, out treasure))
                        {
                            Dbgl($"got {treasureName} in objects");
                            break;
                        }
                    }
                }
                if (treasure == null)
                    return;

                Dbgl($"got treasure {treasureName}");

                if(pickupNetwork.PickupItem.dropper != null)
                {
                    Dbgl($"Got dropper");

                    AccessTools.FieldRefAccess<RandomDropper, Interval_Int>(pickupNetwork.PickupItem.dropper, "amountOfItems") = new Interval_Int()
                    {
                        minValue = treasure.minItems,
                        maxValue = treasure.maxItems
                    };

                    var itemList = new List<RandomItem>();
                    foreach (var o in treasure.randomItems)
                    {
                        Dbgl($"\tsetting for item {o.itemName}");
                        itemList.Add(new RandomItem()
                        {
                            obj = o.itemName == null ? null : ItemManager.GetItemByName(o.itemName),
                            weight = o.weight
                        });
                    }
                    var asset = AccessTools.FieldRefAccess<RandomDropper, SO_RandomDropper>(pickupNetwork.PickupItem.dropper, "randomDropperAsset");
                    asset.randomizer.items = itemList.ToArray();
                    AccessTools.FieldRefAccess<RandomDropper, SO_RandomDropper>(pickupNetwork.PickupItem.dropper, "randomDropperAsset") = asset;
                }
                if(pickupNetwork.PickupItem.yieldHandler?.yieldAsset?.yieldAssets?.Count > 0)
                {
                    Dbgl($"Got yieldHandler");
                    pickupNetwork.PickupItem.yieldHandler.yieldAsset.yieldAssets.Clear();
                    foreach (var o in treasure.guaranteedItems)
                    {
                        Dbgl($"\tsetting for item {o.itemName}");

                        pickupNetwork.PickupItem.yieldHandler.yieldAsset.yieldAssets.Add(new Cost()
                        {
                            item = o.itemName == null ? null : ItemManager.GetItemByName(o.itemName),
                            amount = (int)o.amount
                        });
                    }
                }
            }
        }
        [HarmonyPatch(typeof(TreasurePointManager), "Start")]
        public static class TreasurePointManager_Start_Patch
        {
            public static void Postfix(ref SO_TreasureLootSettings[] ___treasureLootSettings)
            {
                if (!modEnabled.Value || ___treasureLootSettings?.Length == 0)
                {
                    return;
                }
                Dbgl($"tls: {___treasureLootSettings.Length}");
                var filePath = Path.Combine(AedenthornUtils.GetAssetPath(context, true), "settings.json");

                if (!File.Exists(filePath))
                {
                    JsonTreasureDocument document = new JsonTreasureDocument();
                    foreach (var ts in ___treasureLootSettings)
                    {
                        JsonLandmark landmark = new JsonLandmark()
                        {
                            minTreasures = ts.numberOfTreasures.minValue,
                            maxTreasures = ts.numberOfTreasures.maxValue
                        };
                        document.landmarks.Add(ts.name, landmark);
                    }
                    GetItemsRecursive(___treasureLootSettings[0].lootTable.items, document.treasures);
                    File.WriteAllText(filePath, JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
                }
                else
                {
                    JsonTreasureDocument document = null;
                    try
                    {
                        document = JsonSerializer.Deserialize<JsonTreasureDocument>(File.ReadAllText(filePath), new JsonSerializerOptions() { IncludeFields = true });
                    }
                    catch (Exception ex)
                    {
                        Dbgl($"Error reading json file:\n{ex.Message}");
                        return;
                    }
                    if (document == null)
                        return;

                    for(int i = 0; i < ___treasureLootSettings.Length; i++)
                    {
                        if (!document.landmarks.TryGetValue(___treasureLootSettings[i].name, out var setting))
                            continue;
                        Dbgl($"got loot setting {___treasureLootSettings[i].name}");

                        ___treasureLootSettings[i].numberOfTreasures = new Interval_Int()
                        {
                            minValue = setting.minTreasures, 
                            maxValue = setting.maxTreasures
                        };

                        for (int j = 0; j < ___treasureLootSettings[i].lootTable.items.Length; j++)
                        {
                            var item = ___treasureLootSettings[i].lootTable.items[j];
                            if (typeof(TreasurePoint).IsAssignableFrom(item.obj.GetType()))
                            {
                                Dbgl($"setting for tp {item.obj.name}");
                                if (!document.treasures.TryGetValue(item.obj.name, out var obj))
                                    continue;

                                Dbgl($"found");

                                ___treasureLootSettings[i].lootTable.items[j].weight = obj.weight;
                                Dbgl($"set weight to {___treasureLootSettings[i].lootTable.items[j].weight}");
                            }
                        }
                    }
                }
            }
            private static void GetItemsRecursive(RandomItem[] items, Dictionary<string, JsonTreasureObject> objects)
            {

                foreach (var i in items)
                {
                    var obj = new JsonTreasureObject()
                    {
                        weight = i.weight
                    };
                    if (typeof(TreasurePoint).IsAssignableFrom(i.obj.GetType()))
                    {
                        Dbgl($"Got tp {i.obj.name}");
                        
                        var dropper = (i.obj as TreasurePoint).pickupNetworked.PickupItem.dropper;
                        if(dropper != null)
                        {
                            Dbgl($"Got dropper");
                            var itemList = new List<JsonRandomItem>();

                            var interval = AccessTools.FieldRefAccess<RandomDropper, Interval_Int>(dropper, "amountOfItems");
                            obj.minItems = interval.minValue;
                            obj.maxItems = interval.maxValue;
                            var asset = AccessTools.FieldRefAccess<RandomDropper, SO_RandomDropper>(dropper, "randomDropperAsset");
                            foreach (var o in asset.randomizer.items)
                            {
                                Dbgl($"Got item {(o.obj as Item_Base)?.UniqueName}");
                                itemList.Add(new JsonRandomItem()
                                {
                                    itemName = (o.obj as Item_Base)?.UniqueName,
                                    weight = o.weight
                                });
                            }
                            obj.randomItems = itemList.ToArray();

                        }
                        if ((i.obj as TreasurePoint).pickupNetworked.PickupItem.yieldHandler?.yieldAsset?.yieldAssets?.Count > 0)
                        {
                            Dbgl($"Got yieldHandler");
                            var itemList = new List<JsonGuaranteedItem>();

                            foreach (var item in (i.obj as TreasurePoint).pickupNetworked.PickupItem.yieldHandler?.yieldAsset?.yieldAssets)
                            {
                                Dbgl($"Got item {item.item.UniqueName}");

                                itemList.Add(new JsonGuaranteedItem()
                                {
                                    itemName = item.item.UniqueName,
                                    amount = item.amount
                                });
                            }
                            obj.guaranteedItems = itemList.ToArray();
                        }
                    }
                    else if (i.obj is SO_RandomizerCategory)
                    {
                        Dbgl($"Got randomizer");
                        obj.objects = new Dictionary<string, JsonTreasureObject>();
                        GetItemsRecursive((i.obj as SO_RandomizerCategory).randomizer.items, obj.objects);
                    }
                    objects.Add(i.obj.name, obj);
                }
            }
        }
    }
}
