using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;

namespace DoThingsBot.FSM.States {
    public class BotTrading_OpenTradeState : IBotState {
        public string Name { get => "BotTrading_OpenTradeState"; }

        private DateTime lastOpenTradeTry = DateTime.MinValue;
        private int openTradeCount = 0;
        private Machine _machine;

        public const int RETRY_DELAY = 5;
        private ItemBundle itemBundle;

        public BotTrading_OpenTradeState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            _machine = machine;

            if (!Globals.DoThingsBot.HasTradeOpen()) {
                ChatManager.Tell(itemBundle.GetOwner(), "I'm attempting to open a trade window with you, please stand close to me.");
            }

            CoreManager.Current.WorldFilter.EnterTrade += new EventHandler<EnterTradeEventArgs>(WorldFilter_EnterTrade);
        }

        public void Exit(Machine machine) {
            CoreManager.Current.WorldFilter.EnterTrade -= new EventHandler<EnterTradeEventArgs>(WorldFilter_EnterTrade);
        }

        public void TryToOpenTrade() {
            WorldObject player = Util.FindPlayerWorldObjectByName(itemBundle.GetOwner());

            if (player == null || Util.GetDistanceFromPlayer(player) > 2) {
                openTradeCount++;

                if (openTradeCount > 1) {
                    ChatManager.Tell(itemBundle.GetOwner(), "I was unable to open a trade with you. You'll have to start over.");

                    _machine.ChangeState(new BotTrading_TradeCancelledState(itemBundle));
                }
                else {
                    ChatManager.Tell(itemBundle.GetOwner(), "Please get closer to me, I'm unable to open a trade window.  I will retry in " + RETRY_DELAY + " seconds.");
                }
                return;
            }
            else if (Globals.DoThingsBot.HasTradeOpen() && Globals.DoThingsBot.GetTradePartner() == player.Id) {
                // trade has been opened
                if (itemBundle.GetCraftMode() == CraftMode.GiveBackItems) {
                    _machine.ChangeState(new BotTrading_ReturnItemsState(itemBundle));
                }
                else {
                    _machine.ChangeState(new BotTrading_AwaitingItemsState(itemBundle));
                }
                return;
            }
            else if (Globals.DoThingsBot.HasTradeOpen() && Globals.DoThingsBot.GetTradePartner() != player.Id) {
                Globals.Core.Actions.TradeEnd();
                return;
            }

            if (openTradeCount > 2) {
                ChatManager.Tell(itemBundle.GetOwner(), "I was unable to open a trade with you. You'll have to start over.");

                _machine.ChangeState(new BotTrading_TradeCancelledState(itemBundle));
            }
            openTradeCount++;

            CoreManager.Current.Actions.UseItem(player.Id, 0);
        }

        void WorldFilter_EnterTrade(object sender, EnterTradeEventArgs e) {
            try {
                // trade has been opened
                if (itemBundle.GetCraftMode() == CraftMode.GiveBackItems) {
                    _machine.ChangeState(new BotTrading_ReturnItemsState(itemBundle));
                }
                else if (itemBundle.GetCraftMode() == CraftMode.InfiniteRations) {
                    itemBundle.SetCraftMode(CraftMode.GiveBackItems);
                    _machine.ChangeState(new BotTrading_ReturnItemsState(itemBundle));
                }
                else {
                    _machine.ChangeState(new BotTrading_AwaitingItemsState(itemBundle));
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public void Think(Machine machine) {
            if (DateTime.UtcNow - lastOpenTradeTry > TimeSpan.FromMilliseconds(RETRY_DELAY * 1000)) {
                TryToOpenTrade();

                lastOpenTradeTry = DateTime.UtcNow;
            }
        }

        public ItemBundle GetItemBundle() {
            return itemBundle;
        }
    }
}
