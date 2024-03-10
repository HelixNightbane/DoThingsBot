using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;

namespace DoThingsBot.FSM.States {
    public class BotTrading_AwaitingItemsState : IBotState {
        public string Name { get => "BotTrading_AwaitingItemsState"; }

        private Machine _machine;

        public ItemBundle itemBundle;

        private List<WorldObject> tradeItems = new List<WorldObject>();
        private List<int> tradeItemsAwaitingIdentity = new List<int>();

        public BotTrading_AwaitingItemsState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            _machine = machine;

            ChatManager.Tell(itemBundle.GetOwner(), "Add your items to the trade window, and hit Accept Trade.");

            if (itemBundle.GetCraftMode() == CraftMode.WeaponTinkering)
            {
                bool charmed = Globals.Core.CharacterFilter.Augmentations.Contains((int)Augmentations.CharmedSmith);
                int maxsuccess = 33; 

                if (charmed) { maxsuccess = 38; };

                ChatManager.Tell(itemBundle.GetOwner(), "I can now apply Foolproof Salvage to your items! I will apply this last.");
                ChatManager.Tell(itemBundle.GetOwner(), "My chance of success is now correct for imbuing. My maximum chance of success is " + maxsuccess + " percent.");
                ChatManager.Tell(itemBundle.GetOwner(), "If my chance of success for an imbue is " + maxsuccess + " percent, I will automatically attempt to tinker the item.");
            }

           CoreManager.Current.WorldFilter.AddTradeItem += new EventHandler<AddTradeItemEventArgs>(WorldFilter_AddTradeItem);
            CoreManager.Current.WorldFilter.AcceptTrade += new EventHandler<AcceptTradeEventArgs>(WorldFilter_AcceptTrade);
            CoreManager.Current.WorldFilter.EndTrade += new EventHandler<EndTradeEventArgs>(WorldFilter_EndTrade);
            CoreManager.Current.WorldFilter.ResetTrade += new EventHandler<ResetTradeEventArgs>(WorldFilter_ResetTrade);
            Globals.Core.WorldFilter.ChangeObject += new EventHandler<ChangeObjectEventArgs>(WorldFilter_ChangeObject);
        }

        public void Exit(Machine machine) {
            CoreManager.Current.WorldFilter.AddTradeItem -= new EventHandler<AddTradeItemEventArgs>(WorldFilter_AddTradeItem);
            CoreManager.Current.WorldFilter.AcceptTrade -= new EventHandler<AcceptTradeEventArgs>(WorldFilter_AcceptTrade);
            CoreManager.Current.WorldFilter.EndTrade -= new EventHandler<EndTradeEventArgs>(WorldFilter_EndTrade);
            CoreManager.Current.WorldFilter.ResetTrade -= new EventHandler<ResetTradeEventArgs>(WorldFilter_ResetTrade);
            Globals.Core.WorldFilter.ChangeObject -= new EventHandler<ChangeObjectEventArgs>(WorldFilter_ChangeObject);
        }

        void WorldFilter_AddTradeItem(object sender, AddTradeItemEventArgs e) {
            try {
                // item added to trade window
                //Util.WriteToChat("Item added on side: " + e.SideId);

                WorldObject worldObject = CoreManager.Current.WorldFilter[e.ItemId];

                if (worldObject != null && worldObject.HasIdData) {
                    tradeItems.Add(worldObject);
                }
                else {
                    if (!tradeItemsAwaitingIdentity.Contains(e.ItemId)) {
                        tradeItemsAwaitingIdentity.Add(e.ItemId);
                        CoreManager.Current.Actions.RequestId(e.ItemId);
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private bool tradeAccepted = false;

        void WorldFilter_AcceptTrade(object sender, AcceptTradeEventArgs e) {
            try {
                //Util.WriteToChat("Got AcceptTrade: " + e.TargetId + " me: " + CoreManager.Current.CharacterFilter.Id);

                tradeAccepted = true;
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        void WorldFilter_EndTrade(object sender, EndTradeEventArgs e) {
            try {
                Util.WriteToDebugLog($"Got EndTrade: tradeAccepted? {tradeAccepted}");
                if (tradeAccepted) return;
                ChatManager.Tell(itemBundle.GetOwner(), "The trade was cancelled, you will need to send me a command to start again.");
                _machine.ChangeState(new BotTrading_TradeCancelledState(itemBundle));
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private bool selfResetTrade = false;

        void WorldFilter_ResetTrade(object sender, ResetTradeEventArgs e) {
            try {
                if (tradeAccepted) return;

                if (selfResetTrade == false) {
                    Util.WriteToChat("I am ending the trade.");
                    CoreManager.Current.Actions.TradeEnd();
                }
                
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        void WorldFilter_ChangeObject(object sender, ChangeObjectEventArgs e) {
            try {
                if (e.Change == WorldChangeType.IdentReceived && tradeItemsAwaitingIdentity.Contains(e.Changed.Id)) {
                    WorldObject worldObject = CoreManager.Current.WorldFilter[e.Changed.Id];

                    tradeItemsAwaitingIdentity.Remove(e.Changed.Id);

                   if (itemBundle.AddWorldObject(worldObject)) {
                        Util.WriteToDebugLog(itemBundle.GetOwner() + " showed me: " + "(" + worldObject.Id + ")" + Util.GetFullLootName(worldObject));
                        //RespondWithItemRemark(worldObject);

                        if (itemBundle.GetCraftMode() == CraftMode.InfiniteLeather || itemBundle.craftMode == CraftMode.InfiniteDye)
                            return;

                        if (e.Changed.ObjectClass == ObjectClass.Armor || e.Changed.ObjectClass == ObjectClass.Clothing || e.Changed.ObjectClass == ObjectClass.Jewelry || ItemBundle.IsWandOrWeapon(e.Changed)) {
                            itemBundle.tinkerCount = e.Changed.Values(LongValueKey.NumberTimesTinkered, 0);
                        }
                    }
                    else {
                        if (itemBundle.GetInvalidReason() != null) {
                            ChatManager.Tell(itemBundle.GetOwner(), itemBundle.GetInvalidReason());
                        }
                        else {
                            ChatManager.Tell(itemBundle.GetOwner(), "Something went wrong!  Please start over.");
                        }

                        selfResetTrade = true;
                        CoreManager.Current.Actions.TradeReset();
                        itemBundle.ClearItems();

                        System.Threading.Timer timer = null;
                        timer = new System.Threading.Timer((obj) => {
                            _machine.ChangeState(new BotTrading_AwaitingItemsState(itemBundle));
                            timer.Dispose();
                        },
                                    null, 1000, System.Threading.Timeout.Infinite);
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public void RespondWithItemRemark(WorldObject wo) {
            string message = "";

            if (ItemBundle.IsWandOrWeapon(wo)) {
                if (wo.ObjectClass == ObjectClass.WandStaffOrb) {
                    message = String.Format("That's a sweet looking {0}!", Util.GetItemName(wo));
                }
                else {
                    message = String.Format("That's a deadly looking {0}!", Util.GetItemName(wo));
                }
            }
            else if (Salvage.IsSalvage(wo)) {
                SalvageData d = Salvage.GetFromWorldObject(wo);
                WorldObject weapon = itemBundle.GetWeapon();

                if (weapon != null) {
                    message = String.Format("This {0} is gonna make your {1} even better!", d.Name, weapon.Name);
                }
                else {
                    message = String.Format("This {0} is gonna go great on something!", d.Name);
                }
            }

            if (message.Length > 0) {
                ChatManager.Tell(itemBundle.GetOwner(), message);
            }
        }

        private DateTime startedThinking = DateTime.UtcNow;
        private bool hasSentTimeoutWarning = false;

        public void Think(Machine machine) {
            if (hasSentTimeoutWarning == false && DateTime.UtcNow - startedThinking > TimeSpan.FromSeconds(90)) {
                hasSentTimeoutWarning = true;
                ChatManager.Tell(itemBundle.GetOwner(), "This bot session will timeout in 30 seconds unless you complete the trade.");
            }

            if (DateTime.UtcNow - startedThinking > TimeSpan.FromSeconds(120)) {
                ChatManager.Tell(itemBundle.GetOwner(), "This bot session has timed out.");
                CoreManager.Current.Actions.TradeEnd();
                _machine.ChangeState(new BotTrading_TradeCancelledState(itemBundle));
                return;
            }

            if (tradeItemsAwaitingIdentity.Count > 0) {
                //Util.WriteToChat("Waiting to ID " + tradeItemsAwaitingIdentity.Count + " items.");
                return;
            }
            else {
                if (tradeAccepted) {
                    if (itemBundle.CheckIsValidFinal()) {
                        tradeAccepted = true;
                        CoreManager.Current.Actions.TradeAccept();

                        Util.WriteToChat("ItemBundle is valid: " + itemBundle.GetItemNames());

                        //ChatManager.Tell(itemBundle.GetOwner(), "I got your: " + itemBundle.GetItemNames());
                        _machine.ChangeState(new BotTrading_FinishedState(itemBundle));
                    }
                    else {
                        Util.WriteToChat("ItemBundle is invalid: " + itemBundle.GetInvalidReason());

                        ChatManager.Tell(itemBundle.GetOwner(), itemBundle.GetInvalidReason());
                        _machine.ChangeState(new BotTrading_TradeCancelledState(itemBundle));
                    }
                }
            }
        }

        public ItemBundle GetItemBundle() {
            return null;
        }
    }
}
