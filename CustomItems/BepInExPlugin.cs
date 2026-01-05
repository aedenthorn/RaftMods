using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using UnityEngine;

namespace CustomItems
{
    [BepInPlugin("aedenthorn.CustomItems", "CustomItems", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static Dictionary<string, ItemData> itemDataDict = new Dictionary<string, ItemData>();
        public static Dictionary<string, string> termDict = new Dictionary<string, string>();
        public static Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();

        public static void Dbgl(object obj, BepInEx.Logging.LogLevel level = BepInEx.Logging.LogLevel.Debug)
        {
            if (isDebug.Value)
                context.Logger.Log(level, obj);
        }
        public void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "ModEnabled", true, "Enable mod");
			isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
        }

        public void Start()
        {
            ReloadItems();
            //var item = new ItemData();
            //File.WriteAllText(Path.Combine(AedenthornUtils.GetAssetPath(context, true), "test.json"), JsonUtility.ToJson(item));
        }

        public static void ReloadItems()
        {
            itemDataDict.Clear();
            termDict.Clear();
            textureDict.Clear();
            var path = AedenthornUtils.GetAssetPath(context, true);
            var items = Resources.LoadAll<Item_Base>("IItems").ToList<Item_Base>();

            foreach (var file in Directory.GetFiles(path, "*.json", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file).StartsWith("_"))
                    continue;
                try
                {
                    Dbgl($"Loading item file {file}");
                    ItemData itemData = JsonSerializer.Deserialize<ItemData>(File.ReadAllText(file), new JsonSerializerOptions() { IncludeFields = true });
                    if (itemData == null)
                    {
                        Dbgl($"\tError loading json file {file}");
                        continue;
                    }
                    Texture2D tex;
                    var texPath = Path.Combine(Path.GetDirectoryName(file), itemData.settings_Inventory.texturePath);
                    if(!textureDict.TryGetValue(texPath, out tex))
                    {
                        tex = GetTexture(texPath);
                    }

                    Dbgl($"Creating item");
                    Item_Base itemBase = (Item_Base)ScriptableObject.CreateInstance(typeof(Item_Base));
                    itemBase.settings_buildable = new ItemInstance_Buildable(new Block(), false, false, false);
                    itemBase.settings_cookable = new ItemInstance_Cookable(itemData.settings_cookable.cookingSlotsRequired, itemData.settings_cookable.cookTime, GetCost(itemData.settings_cookable.cookingResult, itemData.settings_cookable.cookingResultAmount, items));
                    itemBase.settings_consumeable = new ItemInstance_Consumeable(itemData.settings_consumeable.hungerYield, itemData.settings_consumeable.bonusHungerYield, itemData.settings_consumeable.thirstYield, itemData.settings_consumeable.oxygenYield, itemData.settings_consumeable.isRaw, GetCost(itemData.settings_consumeable.itemAfterUse, itemData.settings_consumeable.itemAfterUseAmount, items), (FoodType)itemData.settings_consumeable.foodType, (FoodForm)itemData.settings_consumeable.foodForm);
                    itemBase.settings_equipment = new ItemInstance_Equipment(itemData.settings_equipment.slotType);
                    itemBase.settings_Inventory = new ItemInstance_Inventory(Sprite.Create(tex, itemData.settings_Inventory.textureRect, Vector2.zero), itemData.settings_Inventory.localizationTerm, itemData.settings_Inventory.stackSize);
                    itemBase.settings_recipe = new ItemInstance_Recipe(CraftingCategory.Nothing, false, false, "", -1);
                    itemBase.settings_usable = new ItemInstance_Usable(itemData.settings_usable.useButtonName, itemData.settings_usable.useButtonCooldown, itemData.settings_usable.consumeUseAmount, itemData.settings_usable.isUsable, itemData.settings_usable.allowHoldButton, itemData.settings_usable.animationOnSelect, itemData.settings_usable.animationOnUse, itemData.settings_usable.forceAnimationIndex, itemData.settings_usable.setTriggering, itemData.settings_usable.lockItemDuringCooldown, itemData.settings_usable.resetTriggerOnDeselect);
                    if(itemData.settings_usable.isUsable)
                    {
                        foreach(var kvp in itemData.settings_usable.useableItemTextures)
                        {
                            string p = Path.Combine(AedenthornUtils.GetAssetPath(context, true), kvp.Value);
                            itemData.settings_usable.useableItemTexturesReal.Add(kvp.Key, GetTexture(p));
                        }
                    }
                    if(true || itemData.uniqueIndex < 0)
                    {
                        int newIndex = 0;
                        while (true)
                        {
                            if(!items.Exists(i => i.UniqueIndex == newIndex))
                            {
                                Dbgl($"setting index to {newIndex}");
                                itemData.uniqueIndex = newIndex;
                                break;
                            }
                            newIndex++;
                        }
                    }
                    itemBase.Initialize(itemData.uniqueIndex, itemData.uniqueName, itemData.maxUses);
                    termDict.Add(itemData.settings_Inventory.localizationTerm, $"{itemData.settings_Inventory.displayName}@{itemData.settings_Inventory.description}");
                    itemData.itemBase = itemBase;
                    itemDataDict.Add(itemData.uniqueName, itemData);
                    items.Add(itemBase);
                }
                catch(Exception ex)
                {
                    Dbgl($"error loading json file {file}\n\n{ex}");
                }
            }
            Dbgl($"Added {itemDataDict.Count}");
            
            AccessTools.StaticFieldRefAccess<ItemManager, List<Item_Base>>("allAvailableItems")?.Clear();
            ItemManager.GetAllItems();

        }

        public static Cost GetCost(string uniqueName, int amount, List<Item_Base> items)
        {
            return new Cost(items.FirstOrDefault(i => i.UniqueName == uniqueName), amount);
        }

        public static Texture2D GetTexture(string texPath)
        {
            var tex = new Texture2D(2, 2);
            Dbgl($"Loading texture file {texPath}");
            var bytes = File.ReadAllBytes(texPath);
            tex.LoadImage(bytes);
            textureDict.Add(texPath, tex);
            return tex;
        }

        [HarmonyPatch(typeof(UseItemController), "Awake")]
        public static class UseItemController_Awake_Patch
        {
            public static void Postfix(UseItemController __instance, Dictionary<string, ItemConnection> ___connectionDictionary)
            {
                if(!modEnabled.Value) 
                    return;
                foreach(var itemData in itemDataDict.Values)
                {
                    if (itemData.settings_usable.isUsable && !string.IsNullOrEmpty(itemData.settings_usable.useableItemTemplate))
                    {

                        Dbgl($"\tAdding useable item to UseItemController");

                        if (__instance.allConnections.Exists(i => i.inventoryItem?.UniqueName == itemData.uniqueName))
                            continue;
                        if (!___connectionDictionary.TryGetValue(itemData.settings_usable.useableItemTemplate, out var temp))
                            continue;
                        var con = new ItemConnection()
                        {
                            inventoryItem = itemData.itemBase,
                        };
                        if (itemData.settings_usable.useableItemTexturesReal.Any())
                        {
                            foreach (var kvp in itemData.settings_usable.useableItemTexturesReal)
                            {
                                if (temp.obj.GetComponent<MeshRenderer>()?.material.mainTexture?.name == kvp.Key)
                                {
                                    var tag = $"{itemData.uniqueName}_{kvp.Key}";
                                    if (!temp.obj.transform.parent.Find(tag))
                                    {
                                        var obj = Instantiate(temp.obj, temp.obj.transform.parent);
                                        obj.name = tag;
                                        obj.GetComponent<MeshRenderer>().material.mainTexture = kvp.Value;
                                        con.obj = obj;
                                        Dbgl($"\tswapped texture {kvp.Key}");
                                        goto next;
                                    }
                                }
                            }
                            con.obj = temp.obj;
                        next:
                            var objs = new List<GameObject>();
                            if (temp.objs?.Any() == true)
                            {
                                foreach (var o in temp.objs)
                                {
                                    if(o.GetComponent<MeshRenderer>()?.material.mainTexture == null)
                                    {
                                        objs.Add(o);
                                        continue;
                                    }
                                    var which = itemData.settings_usable.useableItemTexturesReal.Keys.FirstOrDefault(k => k == o.GetComponent<MeshRenderer>().material.mainTexture.name);
                                    if (which == null)
                                    {
                                        objs.Add(o);
                                        continue;
                                    }
                                    var tag = $"{itemData.uniqueName}_{which}";
                                    var obj = temp.obj.transform.parent.Find(tag)?.gameObject;
                                    if (obj == null)
                                    {
                                        obj = Instantiate(temp.obj, temp.obj.transform.parent);
                                        obj.name = tag;
                                        obj.GetComponent<MeshRenderer>().material.mainTexture = itemData.settings_usable.useableItemTexturesReal[which];
                                        objs.Add(obj);
                                        Dbgl($"\tswapped texture {which}");
                                    }
                                    else
                                    {
                                        objs.Add(obj);
                                    }
                                }
                            }
                            else
                            {
                                objs = temp.objs;
                            }
                            con.objs = objs;
                        }
                        else
                        {
                            con.obj = temp.obj;
                            con.objs = temp.objs;
                        }
                        __instance.allConnections.Add(con);
                        ___connectionDictionary.Add(itemData.uniqueName, con);
                        Dbgl($"\tAdded useable item to UseItemController");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ItemManager), "LoadAllItems")]
        public static class ItemManager_LoadAllItems_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling ItemManager_LoadAllItems");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Stsfld)
                    {
                        Dbgl("Inserting method to add new items");
                        codes.Insert(i, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(AddCustomItems))));
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }
        public static List<Item_Base> AddCustomItems(List<Item_Base> items)
        {
            if(modEnabled.Value)
            {
                Dbgl("Adding custom items");
                items.AddRange(itemDataDict.Select(p => p.Value.itemBase));
            }
            return items;
        }


        [HarmonyPatch(typeof(Helper), nameof(Helper.GetTerm))]
        public static class Helper_GetTerm_Patch
        {
            public static bool Prefix(string term, ref string __result)
            {
                if(!modEnabled.Value || !termDict.TryGetValue(term, out var str)) 
                    return true;
                __result = str;
                return false;
            }

        }

        [HarmonyPatch(typeof(Cheat), nameof(Cheat.HandleCheatCode))]
        public static class Cheat_HandleCheatCode_Patch
        {
            public static bool Prefix(string p_message, string p_commandDelimeter)
            {
                if (!modEnabled.Value)
                    return true;
                string text = p_message.Substring(p_commandDelimeter.Length, p_message.Length - 1);
                string[] array = text.Split(new char[] { ' ' });
                if (array.Length == 0)
                {
                    return false;
                }
                if ("dump".Contains(array[0]) && array.Length >= 2)
                {
                    var item = ItemManager.GetItemByNameContains(array[1]);
                    if (item != null)
                    {
                        ItemData data = new ItemData();
                        if (item.settings_consumeable != null)
                        {
                            data.settings_consumeable = new ItemInstance_Consumeable_Data()
                            {
                                foodType = item.settings_consumeable.FoodType,
                                foodForm = item.settings_consumeable.FoodForm,
                                oxygenYield = item.settings_consumeable.OxygenYield,
                                hungerYield = item.settings_consumeable.HungerYield,
                                bonusHungerYield = item.settings_consumeable.BonusHungerYield,
                                thirstYield = item.settings_consumeable.ThirstYield,
                                bonusThirstYield = item.settings_consumeable.BonusThirstYield,
                                isRaw = item.settings_consumeable.IsRaw,
                                eventRef_consumeSound = item.settings_consumeable.EventRef_ConsumeSound
                            };

                        }
                        if (item.settings_cookable?.CookingResult?.item != null)
                        {
                            data.settings_cookable = new ItemInstance_Cookable_Data()
                            {
                                cookTime = item.settings_cookable.CookingTime,
                                cookingSlotsRequired = item.settings_cookable.CookingSlotsRequired,
                                cookingResult = item.settings_cookable.CookingResult.item?.UniqueName,
                                cookingResultAmount = item.settings_cookable.CookingResult.amount
                            };
                        }
                        if (item.settings_equipment != null)
                        {
                            data.settings_equipment = new ItemInstance_Equipment_Data()
                            {
                                slotType = item.settings_equipment.EquipType
                            };
                        }
                        if (item.settings_Inventory != null)
                        {
                            data.settings_Inventory = new ItemInstance_Inventory_Data()
                            {

                                localizationTerm = item.settings_Inventory.LocalizationTerm,
                                displayName = item.settings_Inventory.DisplayName,
                                description = item.settings_Inventory.Description,
                                stackSize = item.settings_Inventory.StackSize
                            };
                        }
                        if (item.settings_usable != null)
                        {
                            data.settings_usable = new ItemInstance_Usable_Data()
                            {
                                isUsable = item.settings_usable.IsUsable(),
                                allowHoldButton = item.settings_usable.AllowHoldButton(),
                                useButtonName = item.settings_usable.GetUseButtonName(),
                                useButtonCooldown = item.settings_usable.UseButtonCooldown,
                                animationOnSelect = item.settings_usable.AnimationOnSelect,
                                animationOnUse = item.settings_usable.AnimationOnUse,
                                forceAnimationIndex = item.settings_usable.ForceAnimationIndex,
                                setTriggering = item.settings_usable.SetTriggering,
                                lockItemDuringCooldown = item.settings_usable.LockItemDuringCooldown,
                                resetTriggerOnDeselect = item.settings_usable.ResetTriggerOnDeselect,
                                consumeUseAmount = item.settings_usable.ConsumeUseAmount,
                                eventRef_break = item.settings_usable.EventRef_Break
                            };

                        }

                        File.WriteAllText(Path.Combine(AedenthornUtils.GetAssetPath(context, true), $"_{item.UniqueName}.json"), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }));
                    }
                    return false;
                }
                return true;
            }
        }

    }
}
