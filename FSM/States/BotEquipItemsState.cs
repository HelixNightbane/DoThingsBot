using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;

namespace DoThingsBot.FSM.States {
    class BotEquipItemsState : IBotState {

        public string Name { get => "EquipItemsState"; }
        private ItemBundle itemBundle;
        private List<int> Equipment;
        private Machine _machine;


        private int currentEquipIndex = 0;
        private List<int> movedItems = new List<int>();

        private int equipTryCount = 0;
        private int lastEquippedItem = 0;
        private TimeSpan equipItemDelay = TimeSpan.FromMilliseconds(300);
        private TimeSpan dequipItemDelay = TimeSpan.FromMilliseconds(300);
        private bool hasDequippedAllItems = false;
        private DateTime lastThought = DateTime.MinValue;
        private DateTime lastEquipItemCommand = DateTime.MinValue;

        private List<int> requestedItemIds = new List<int>();
        

        public BotEquipItemsState(ItemBundle items) {
            try {
                itemBundle = items;
                Equipment = GetEquipment();
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public List<int> GetEquipment() {
            try {
                switch (itemBundle.GetEquipMode()) {
                    case EquipMode.Idle:
                        return Config.Equipment.IdleEquipmentIds.Value;
                    case EquipMode.Buff:
                        return Config.Equipment.BuffEquipmentIds.Value;
                    case EquipMode.SummonPortal:
                        return Config.Equipment.BuffEquipmentIds.Value;
                    case EquipMode.Tinker:
                        return Config.Equipment.TinkerEquipmentIds.Value;
                    case EquipMode.Craft:
                        return Config.Equipment.CraftEquipmentIds.Value;
                    default:
                        return Config.Equipment.IdleEquipmentIds.Value;
                }
            }
            catch (Exception e) { Util.LogException(e); }

            return null;
        }

        public void Enter(Machine machine) {
            try {
                _machine = machine;

                string itemNames = "";

                Util.WriteToDebugLog("Equipment mode is: " + itemBundle.GetEquipMode());

                foreach (var eq in Equipment) {
                    itemNames += eq + ", ";
                }

                Util.WriteToDebugLog("Items to Equip: " + itemNames);

                // done
                if (Equipment.Count == 0) {
                    GoToNextState();
                    return;
                }

                currentEquipIndex = 0;

                CoreManager.Current.WorldFilter.ChangeObject += WorldFilter_ChangeObject;
            }
            catch (Exception e) { Util.LogException(e); }
        }

        private void WorldFilter_ChangeObject(object sender, ChangeObjectEventArgs e) {
            try {
                if (e.Change == WorldChangeType.StorageChange) {
                    var equippedSlots = e.Changed.Values(LongValueKey.EquippedSlots, 0);
                    var slot = e.Changed.Values(LongValueKey.Slot, -1);
                    if (!hasDequippedAllItems) {
                        if (slot != -1) {
                            movedItems.Add(e.Changed.Id);
                            equipTryCount = 0;
                        }
                    }
                    else if (hasDequippedAllItems) {
                        if (!movedItems.Contains(e.Changed.Id)) {
                            movedItems.Add(e.Changed.Id);

                            lastEquipItemCommand = DateTime.UtcNow - TimeSpan.FromMilliseconds(100);
                        }
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public void Exit(Machine machine) {
            try {

            }
            catch (Exception e) { Util.LogException(e); }
        }

        public void GoToNextState() {
            if (itemBundle.GetCraftMode() == CraftMode.CheckSkills) {
                _machine.ChangeState(new BotCheckSkillsState(GetItemBundle()));
                return;
            }

            switch (itemBundle.GetEquipMode()) {
                case EquipMode.Idle:
                    _machine.ChangeState(new BotIdleState());
                    return;
                case EquipMode.Buff:
                    _machine.ChangeState(new BotBuffingState(GetItemBundle()));
                    return;
                case EquipMode.SummonPortal:
                    _machine.ChangeState(new BotSummonPortalState(GetItemBundle()));
                    return;
                case EquipMode.Tinker:
                    _machine.ChangeState(new BotTinkeringState(GetItemBundle()));
                    return;
                case EquipMode.Craft:
                    _machine.ChangeState(new BotCraftingState(GetItemBundle()));
                    return;
                default:
                    _machine.ChangeState(new BotIdleState());
                    return;
            }
        }
        
        public void Think(Machine machine) {
            try {
                if (DateTime.UtcNow - lastThought > TimeSpan.FromMilliseconds(50)) {
                    lastThought = DateTime.UtcNow;

                    // skip this item if its taking too long
                    if (lastEquipItemCommand > DateTime.MinValue && DateTime.UtcNow - lastEquipItemCommand > TimeSpan.FromSeconds(5)) {
                        var a = DateTime.UtcNow - lastEquipItemCommand;
                        currentEquipIndex++;
                    }

                    if (CoreManager.Current.Actions.BusyState != 0) return;

                    //if we are out of euip items, go to the next state
                    if (currentEquipIndex >= Equipment.Count) {
                        GoToNextState();
                        return;
                    }

                    if (!hasDequippedAllItems) {
                        // dequip any currently equipped items
                        foreach (WorldObject item in CoreManager.Current.WorldFilter.GetInventory()) {
                            // skip items we are just going to equip in a second anyways
                            if (GetEquipment().Contains(item.Id)) {
                                if (!movedItems.Contains(item.Id) && item.Values(LongValueKey.Slot, -1) == -1) {
                                    //Util.WriteToChat("Adding early: " + item.Name);
                                    movedItems.Add(item.Id);
                                }
                                continue;
                            }
                            if (movedItems.Contains(item.Id)) continue;

                            if (lastEquippedItem != item.Id || DateTime.UtcNow - lastEquipItemCommand > dequipItemDelay) {
                                if (item.Values(LongValueKey.Slot, -1) == -1) {
                                    //Util.WriteToDebugLog("Unequipping " + item.Name);

                                    if (equipTryCount > 15) {
                                        movedItems.Add(item.Id);
                                    }

                                    equipTryCount++;
                                    lastEquipItemCommand = DateTime.UtcNow;
                                    lastEquippedItem = item.Id;
                                    CoreManager.Current.Actions.MoveItem(item.Id, CoreManager.Current.CharacterFilter.Id);

                                    if (!requestedItemIds.Contains(item.Id)) {
                                        requestedItemIds.Add(item.Id);
                                        CoreManager.Current.Actions.RequestId(item.Id);
                                    }

                                    return;
                                }
                            }
                            else {
                                return;
                            }
                        }

                        hasDequippedAllItems = true;
                    }

                    int itemId = GetEquipment()[currentEquipIndex];

                    WorldObject wo = CoreManager.Current.WorldFilter[itemId];

                    if (wo == null) {
                        Util.WriteToDebugLog(String.Format("Could not find item with id ({0}), SKIPPING", itemId));
                        currentEquipIndex++;
                        return;
                    }

                    if (movedItems.Contains(wo.Id) || wo.Values(LongValueKey.Slot, -1) == -1) {
                        //Util.WriteToChat("Equipped " + Util.GetGameItemDisplayName(wo));
                        equipTryCount = 0;
                        currentEquipIndex++;
                    }
                    else {
                        if (DateTime.UtcNow - lastEquipItemCommand > equipItemDelay) {
                            lastEquipItemCommand = DateTime.UtcNow;
                            lastEquippedItem = wo.Id;

                            //if (equipTryCount == 0) {
                               // Util.WriteToChat("Equipping " + Util.GetGameItemDisplayName(wo));
                            //}

                            equipTryCount++;
                            CoreManager.Current.Actions.UseItem(wo.Id, 0);

                            if (!requestedItemIds.Contains(wo.Id)) {
                                requestedItemIds.Add(wo.Id);
                                CoreManager.Current.Actions.RequestId(wo.Id);
                            }
                        }
                    }
                }
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public ItemBundle GetItemBundle() {
            try {
                return itemBundle;
            }
            catch (Exception e) { Util.LogException(e); }

            return null;
        }
    }
}


