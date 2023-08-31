using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using DoThingsBot.Lib;

namespace DoThingsBot.FSM.States {
    public class BotTinkering_FinishedState : IBotState {
        public string Name { get => "BotTinkering_FinishedState"; }
        public string PlayerName;

        public const int RETRY_DELAY = 4;
        public ItemBundle itemBundle;
        public List<WorldObject> items;

        public BotTinkering_FinishedState(ItemBundle items) {
            itemBundle = items;

            try {
                PostMessageTools.ClickNo();
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public void Enter(Machine machine) {
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
