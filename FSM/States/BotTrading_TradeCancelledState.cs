using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;

namespace DoThingsBot.FSM.States {
    public class BotTrading_TradeCancelledState : IBotState {
        public string Name { get => "BotTrading_TradeCancelledState"; }
        public string PlayerName;
        
        public ItemBundle itemBundle;

        public BotTrading_TradeCancelledState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            try {
                CoreManager.Current.Actions.TradeEnd();
            }
            catch (Exception e) {
                Util.LogException(e);
            }
        }

        public void Exit(Machine machine) {

        }

        public void Think(Machine machine) {
        }

        public ItemBundle GetItemBundle() {
            return itemBundle;
        }
    }
}
