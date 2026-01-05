using FMODUnity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomItems
{
    [Serializable]
    public class ItemData
    {
        public string uniqueName;
        public int uniqueIndex = -1;
        public int maxUses = 1;
        public ItemInstance_Usable_Data settings_usable;
        public ItemInstance_Equipment_Data settings_equipment;

        public ItemInstance_Inventory_Data settings_Inventory;
        public ItemInstance_Consumeable_Data settings_consumeable;
        public ItemInstance_Cookable_Data settings_cookable;

        internal Item_Base itemBase;
    }

    [Serializable]
    public class ItemInstance_Usable_Data
    {
        public bool isUsable;
        public bool allowHoldButton;
        public string useButtonName = "LeftClick";
        public float useButtonCooldown = 0.2f;
        public PlayerAnimation animationOnSelect = PlayerAnimation.None;
        public PlayerAnimation animationOnUse = PlayerAnimation.None;
        public bool forceAnimationIndex;
        public bool setTriggering;
        public bool lockItemDuringCooldown;
        public string resetTriggerOnDeselect = string.Empty;
        public int consumeUseAmount;
        public string eventRef_break;
        public string useableItemTemplate;
        public Dictionary<string, string> useableItemTextures = new Dictionary<string, string>();
        internal Dictionary<string, Texture2D> useableItemTexturesReal = new Dictionary<string, Texture2D>();
    }
    [Serializable]
    public class ItemInstance_Equipment_Data
    {
        public EquipSlotType slotType;
    }
    [Serializable]
    public class ItemInstance_Inventory_Data
    {
        public string texturePath;
        internal Rect textureRect = new Rect(0,0,64,64);
        public string localizationTerm = "Item/...";
        public string displayName = "An item";
        public string description = "A description of an item";
        public int stackSize = 1;
    }
    [Serializable]
    public class ItemInstance_Consumeable_Data
    {
        public FoodType foodType;
        public FoodForm foodForm;
        public float oxygenYield;
        public float hungerYield;
        public float bonusHungerYield;
        public float thirstYield;
        public float bonusThirstYield;
        public bool isRaw = true;
        public string eventRef_consumeSound = string.Empty;
        public string itemAfterUse = null;
        public int itemAfterUseAmount = 0;
    }
    [Serializable]
    public class ItemInstance_Cookable_Data
    {
        public float cookTime = 1f;
        public int cookingSlotsRequired = 1;
        public string cookingResult;
        public int cookingResultAmount;
    }
}