using System;
using System.Collections.Generic;
using System.Text;

namespace DoThingsBot.FSM.States {
    class Finished : IBotState {
        public string Name { get => "BotBuffing_FinishedState"; }
        private ItemBundle itemBundle;

        public Finished(ItemBundle items) {
            itemBundle = items;
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
