using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DoThingsBot.FSM.States {
    public class BotTinkering_AwaitCommandState : IBotState {
        public string Name { get => "BotTinkering_AwaitCommandState"; }

        public int ThinkCount { get => _thinkCounter; }
        private int _thinkCounter = 0;
        Machine _machine;

        private ItemBundle itemBundle;

        public BotTinkering_AwaitCommandState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            _machine = machine;

            ChatManager.RaiseChatCommandEvent += new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);

            ChatManager.Tell(itemBundle.GetOwner(), String.Format("I {0}'", itemBundle.successChanceFullString) + ". Respond with 'go' or 'cancel'.");
        }

        public void Exit(Machine machine) {
            ChatManager.RaiseChatCommandEvent -= new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);

        }

        void ChatManager_ChatCommand(object sender, ChatCommandEventArgs e) {
            try {
                //Util.WriteToChat(String.Format("Got command: '{0}' from '{1}' args: '{2}'", e.Command, e.PlayerName, e.Arguments));

                switch (e.Command) {
                    case "go":
                        _machine.ChangeState(new BotTinkering_ConfirmedState(itemBundle));
                        break;

                    case "cancel":
                        _machine.ChangeState(new BotTinkering_CancelledState(itemBundle));
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        DateTime startTime = DateTime.UtcNow;

        public void Think(Machine machine) {
            if (DateTime.UtcNow - startTime > TimeSpan.FromSeconds(15)) {
                ChatManager.Tell(itemBundle.GetOwner(), "Your request has timed out.");
                _machine.ChangeState(new BotTinkering_CancelledState(itemBundle));
            }
        }

        public ItemBundle GetItemBundle() {
            return itemBundle;
        }
    }
}
