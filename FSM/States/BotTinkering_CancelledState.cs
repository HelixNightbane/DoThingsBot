using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using DoThingsBot.Lib;

namespace DoThingsBot.FSM.States {
    public class BotTinkering_CancelledState : IBotState {
        public string Name { get => "BotTinkering_CancelledState"; }
        public string PlayerName;

        public const int RETRY_DELAY = 4;
        public ItemBundle itemBundle;

        public BotTinkering_CancelledState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            // TODO: click no on the confirm craft success window
            try {
                try {
                    PostMessageTools.ClickNo();
                }
                catch (Exception e) { Util.LogException(e); }

                machine.ChangeState(new BotTinkering_FinishedState(itemBundle));
            }
            catch (Exception e) { Util.LogException(e); }
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
