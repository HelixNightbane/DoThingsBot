using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DoThingsBot.FSM.States {
    public class BotTrading_ReturnItemsState : IBotState {
        public string Name { get => "BotTrading_ReturnItemsState"; }

        public int ThinkCount { get => _thinkCounter; }
        private int _thinkCounter = 0;

        private ItemBundle itemBundle;
        private Machine _machine;
        private List<int> itemsToAddToTrade;
        private List<int> itemsBeingAddedToTrade = new List<int>();

        public BotTrading_ReturnItemsState(ItemBundle items) {
            itemBundle = items;

            itemsToAddToTrade = new List<int>(itemBundle.GetStolenItems().ToArray());

            Util.WriteToDebugLog($"itemsToAddToTrade size is {itemsToAddToTrade.Count}");
        }

        public void Enter(Machine machine) {
            _machine = machine;

            CoreManager.Current.WorldFilter.AddTradeItem += new EventHandler<AddTradeItemEventArgs>(WorldFilter_AddTradeItem);
            CoreManager.Current.WorldFilter.AcceptTrade += new EventHandler<AcceptTradeEventArgs>(WorldFilter_AcceptTrade);
            CoreManager.Current.WorldFilter.EndTrade += new EventHandler<EndTradeEventArgs>(WorldFilter_EndTrade);
            CoreManager.Current.WorldFilter.ResetTrade += new EventHandler<ResetTradeEventArgs>(WorldFilter_ResetTrade);

            if (itemsToAddToTrade.Count == 0) {
                _machine.ChangeState(new BotTrading_FinishedState(itemBundle));
            }
        }

        public void Exit(Machine machine) {
            CoreManager.Current.WorldFilter.AddTradeItem -= new EventHandler<AddTradeItemEventArgs>(WorldFilter_AddTradeItem);
            CoreManager.Current.WorldFilter.AcceptTrade -= new EventHandler<AcceptTradeEventArgs>(WorldFilter_AcceptTrade);
            CoreManager.Current.WorldFilter.EndTrade -= new EventHandler<EndTradeEventArgs>(WorldFilter_EndTrade);
            CoreManager.Current.WorldFilter.ResetTrade -= new EventHandler<ResetTradeEventArgs>(WorldFilter_ResetTrade);
        }

        private List<int> itemsIdsAdded = new List<int>();

        void WorldFilter_AddTradeItem(object sender, AddTradeItemEventArgs e) {
            try {
                var wo = CoreManager.Current.WorldFilter[e.ItemId];

                if (wo != null) {
                    Util.WriteToDebugLog("Added item " + wo.Name + " to the trade.");
                }

                if (itemsBeingAddedToTrade.Contains(e.ItemId)) {
                    itemsBeingAddedToTrade.Remove(e.ItemId);
                }

                if (!itemsIdsAdded.Contains(e.ItemId)) {
                    itemsIdsAdded.Add(e.ItemId);
                }

                if (itemsBeingAddedToTrade.Count == 0) {
                    CoreManager.Current.Actions.TradeAccept();
                }

                Util.WriteToDebugLog($"itemsIdsAdded size is now {itemsIdsAdded.Count}");
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        // this only fires for the first person to hit accept.
        void WorldFilter_AcceptTrade(object sender, AcceptTradeEventArgs e) {
            try {
                Util.WriteToDebugLog("Got AcceptTrade: " + e.TargetId + " me: " + CoreManager.Current.CharacterFilter.Id);
                if (e.TargetId != CoreManager.Current.CharacterFilter.Id)
                    CoreManager.Current.Actions.TradeAccept();
                return;
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        void WorldFilter_EndTrade(object sender, EndTradeEventArgs e) {
            try {
                Util.WriteToDebugLog($"Got WorldFilter_EndTrade: {e.ReasonId}");
                ChatManager.Tell(itemBundle.GetOwner(), "The trade was cancelled, tell me 'lostitems' and I will attempt to return your items again.");
                _machine.ChangeState(new BotTrading_TradeCancelledState(itemBundle));
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        //private bool tradeWasResetByMe = false;
        void WorldFilter_ResetTrade(object sender, ResetTradeEventArgs e) {
            try {
                Util.WriteToDebugLog($"Got WorldFilter_ResetTrade: {e.TraderId} me:{CoreManager.Current.CharacterFilter.Id}");
                if (CoreManager.Current.CharacterFilter.Id != e.TraderId) {
                    Util.WriteToDebugLog("Trade was reset. I am ending the trade.");
                    CoreManager.Current.Actions.TradeEnd();
                    ChatManager.Tell(itemBundle.GetOwner(), "It looks like you reset or ended the trade.  You will have to tell me 'lostitems' to get your items back.");
                    _machine.ChangeState(new BotTrading_FinishedState(itemBundle));
                }
                else {
                    Util.WriteToDebugLog("Trade was ended, marking all items as given.");
                    foreach (int id in itemsIdsAdded) {
                        itemBundle.RemoveItem(id);
                    }

                    _machine.ChangeState(new BotTrading_FinishedState(itemBundle));
                }

            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private DateTime lastThought = DateTime.UtcNow;
        private DateTime startTime = DateTime.UtcNow;
        private bool didFinish = false;

        public void Think(Machine machine) {
            try {
                if (DateTime.UtcNow - lastThought > TimeSpan.FromMilliseconds(500)) {
                    lastThought = DateTime.UtcNow;

                    if (DateTime.UtcNow - startTime > TimeSpan.FromSeconds(20)) {
                        Util.WriteToDebugLog("Trade timed out in ReturnItemsState::Think");
                        ChatManager.Tell(itemBundle.GetOwner(), "The trade has timed out.  Tell me 'lostitems' to check for any items of yours that I still have.");
                        machine.ChangeState(new BotTrading_TradeCancelledState(itemBundle));
                        return;
                    }

                    if (itemsBeingAddedToTrade.Count > 0) {
                        // still waiting on items to be confirmed added to trade window.
                        return;
                    }

                    if (itemsToAddToTrade.Count > 0) {
                        Util.WriteToDebugLog($"Attempting to add {itemsToAddToTrade.Count} items to the trade window");
                        foreach (var item in itemsToAddToTrade) {
                            var wo = CoreManager.Current.WorldFilter[item];
                            if (wo != null) {
                                Util.WriteToDebugLog(String.Format("Attempting to add {0} to the trade window", Util.GetItemShortName(wo)));
                                CoreManager.Current.Actions.TradeAdd(item);

                                itemsBeingAddedToTrade.Add(item);
                            }
                        }

                        itemsToAddToTrade.Clear();
                        return;
                    }

                    if (!didFinish) {
                        didFinish = true;
                        Util.WriteToDebugLog($"Finishing: itemsIdsAdded.Count:{itemsIdsAdded.Count} ");
                        CoreManager.Current.Actions.TradeAccept();

                        if (itemsIdsAdded.Count > 0) {
                            Util.WriteToDebugLog($"Everything went OK? Accepting trade.");
                            return;
                        }
                        else {
                            Util.WriteToDebugLog($"Unable to confirm... Accepting trade.");
                            ChatManager.Tell(itemBundle.GetOwner(), "I was unable to confirm I gave you your items back.  Tell me 'lostitems' to check for any items of yours that I still have.");
                            return;
                        }
                    }
                }
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public ItemBundle GetItemBundle() {
            return null;
        }
    }
}
