using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using DoThingsBot.Lib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DoThingsBot.FSM.States {
    public class BotTinkeringState : IBotState {
        public string Name { get => "BotTinkeringState"; }

        private Machine _machine;
        private Machine parentMachine;
        private bool IsRunning = false;
        private ItemBundle itemBundle;

        public BotTinkeringState(ItemBundle items) {
            itemBundle = items;
            itemBundle.playerData.jobType = "tinker";

            _machine = new Machine();
        }

        public void Enter(Machine machine) {
            parentMachine = machine;
            IsRunning = true;
            try {
                PostMessageTools.ClickNo();
            }
            catch (Exception e) { Util.LogException(e); }

            _machine.SetParentState(this.Name);
            _machine.ChangeState(new BotTinkering_UseBuffItemsState(itemBundle));
            _machine.Start();

            ChatManager.RaiseChatCommandEvent += new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);
        }

        public void Exit(Machine machine) {
            IsRunning = false;
            _machine.Stop();

            ChatManager.RaiseChatCommandEvent -= new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);
        }

        DateTime firstThought = DateTime.UtcNow;
        DateTime lastThought = DateTime.UtcNow;
        bool didFail = false;
        bool didCancel = false;

        void ChatManager_ChatCommand(object sender, ChatCommandEventArgs e) {
            try {
                if (_machine.IsOrWillBeInState("BotTinkering_AwaitCommandState")) return;

                switch (e.Command) {
                    case "cancel":
                        didCancel = true;
                        ChatManager.Tell(itemBundle.GetOwner(), "Cancelling this tinkering session.");
                        itemBundle.SavePlayerData();
                        itemBundle.SetCraftMode(CraftMode.GiveBackItems);
                        parentMachine.ChangeState(new BotTradingState(itemBundle));
                        break;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public void Think(Machine machine) {
            if (didCancel) return;

            if (DateTime.UtcNow - firstThought > TimeSpan.FromSeconds(180)) {
                if (!didFail) {
                    didFail = true;
                    ChatManager.Tell(itemBundle.GetOwner(), "The tinkering request timed out, probably because something went wrong.");
                    _machine.ChangeState(new BotTinkering_CancelledState(itemBundle));
                }
                return;
            }

            if (DateTime.UtcNow - lastThought > TimeSpan.FromMilliseconds(200)) {
                lastThought = DateTime.UtcNow;

                if (_machine.IsInState("BotTinkering_FinishedState")) {
                    if (itemBundle.GetItems().Count > 0) {
                        itemBundle.SetCraftMode(CraftMode.GiveBackItems);
                        machine.ChangeState(new BotTradingState(itemBundle));
                    }
                    else {
                        machine.ChangeState(new BotFinishState(itemBundle));
                    }
                    return;
                }
            }

            if (IsRunning) _machine.Think();
        }

        public ItemBundle GetItemBundle() {
            return itemBundle;
        }
    }
}
