using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace FullReturn
{
    [BepInPlugin("aedenthorn.FullReturn", "Full Return", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> returnFortification;
        public static ConfigEntry<float> returnPercent;

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
			returnFortification = Config.Bind<bool>("Options", "ReturnFortification", true, "Return fortificiation costs");
			returnPercent = Config.Bind<float>("Options", "ReturnPercent", 1f, "Decimal portion to return");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
		[HarmonyPatch(typeof(RemovePlaceables), nameof(RemovePlaceables.ReturnItemsFromBlock))]
        public static class RemovePlaceables_ReturnItemsFromBlock_Patch
        {
            public static void Prefix(Block block, Network_Player player, bool giveItems)
            {
                if (!modEnabled.Value)
                    return;
                Dbgl($"returning items for {block.buildableItem?.UniqueName}");
            }
            public static void Postfix(Block block, Network_Player player, bool giveItems)
            {
                if (!modEnabled.Value || !giveItems || !block.Reinforced || !returnFortification.Value || GameModeValueManager.GetCurrentGameModeValue().playerSpecificVariables.unlimitedResources)
                    return;
                var item = ItemManager.GetAllItems().FirstOrDefault(i => i.UniqueName.Equals("Block_FoundationArmor"));
                if (item is null)
                    return;
                foreach (CostMultiple costMultiple in item.settings_recipe.NewCost)
                {
                    player.Inventory.AddItem(costMultiple.items[0].UniqueName, Mathf.CeilToInt(costMultiple.amount * returnPercent.Value));
                }
            }
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling RemovePlaceables.ReturnItemsFromBlock");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldc_R4 && (float)codes[i].operand == 0.5f)
                    {
                        Dbgl("replacing 0.5 with method");
                        codes.Insert(i+1, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetPortion))));
                        break;
                    }
                }

                return codes.AsEnumerable();
            }
        }
		[HarmonyPatch(typeof(BlockCreator), "RemoveBlock")]
        public static class BlockCreator_RemoveBlock_Patch
        {
            public static bool Prefix(Block block, Network_Player playerRemovingBlock, bool updateRaftBounds)
            {
                if (!modEnabled.Value )
                    return true;

                if (SingletonGeneric<GameManager>.Singleton != null && SingletonGeneric<GameManager>.Singleton.gameObject.activeInHierarchy)
                {
                    SingletonGeneric<GameManager>.Singleton.StartCoroutine(RemoveBlockCoroutine(block, playerRemovingBlock, updateRaftBounds));
                }
                return false;
            }
        }

        public static IEnumerator RemoveBlockCoroutine(Block block, Network_Player playerRemovingBlock, bool updateRaftBounds)
        {
            RaftBounds ___raftBounds = AccessTools.StaticFieldRefAccess<BlockCreator, RaftBounds>("raftBounds");
            List<Block> ___placedBlocks = AccessTools.StaticFieldRefAccess<BlockCreator, List<Block>>("placedBlocks");
            if (block != null)
            {
                ComponentManager<SoundManager>.Value.PlayBlockBreak(block.transform.position, Block.IsBlockIndexFoundation(block.buildableItem.UniqueIndex) || Block.IsBlockIndexCollectionNet(block.buildableItem.UniqueIndex));
            }
            HashSet<Block> destroyedBlocksFilter = new HashSet<Block>();
            yield return SingletonGeneric<GameManager>.Singleton.StartCoroutine(BlockCreator.DestroyBlock(block, destroyedBlocksFilter));

            List<Block> list = destroyedBlocksFilter.ToList<Block>();
            Dbgl($"got {list.Count} destroyed blocks");
            if (updateRaftBounds)
            {
                ___raftBounds.RemovedWalkableBlocks(list);
            }
            if (BlockCreator.RemoveBlockCallStack != null)
            {
                BlockCreator.RemoveBlockCallStack(list, playerRemovingBlock);
            }
            if (BlockCreator.RemoveBlockCallStackAlways != null)
            {
                BlockCreator.RemoveBlockCallStackAlways(list, playerRemovingBlock);
            }
            for (int i = 0; i < list.Count; i++)
            {
                Block block2 = list[i];
                if (block2 != null)
                {
                    Dbgl($"trying to return items for {block2.buildableItem?.UniqueName}");

                    if (playerRemovingBlock != null && playerRemovingBlock.IsLocalPlayer)
                    {
                        if (block2.itemToReturnOnDestroy != null && block2.itemToReturnOnDestroy.UniqueIndex != 257)
                        {
                            playerRemovingBlock.Inventory.AddItem(block2.itemToReturnOnDestroy.UniqueName, 1);
                        }
                        if(block != block2)
                            RemovePlaceables.ReturnItemsFromBlock(block2, playerRemovingBlock, true);
                    }
                    if (block2.networkedBehaviour != null)
                    {
                        NetworkUpdateManager.RemoveBehaviour(block2.networkedBehaviour);
                    }
                    DestroyImmediate(block2.gameObject);
                }
            }
            for (int j = 0; j < ___placedBlocks.Count; j++)
            {
                if (___placedBlocks[j] == null)
                {
                    ___placedBlocks.RemoveAt(j);
                    j--;
                }
            }
            yield break;
        }

        public static float GetPortion(float value)
        {
            if (!modEnabled.Value)
                return value;
            return returnPercent.Value;
        }
    }
}
