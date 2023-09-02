using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using DoThingsBot.Lib;
using DoThingsBot.Lib.Recipes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DoThingsBot.FSM.States {
    class BotInfinites_LeatherState : IBotState {
        public string Name { get => "BotInfinites_LeatherState"; }

        public int ThinkCount { get => _thinkCounter; }
        private int _thinkCounter = 0;
        private int toolId = 0;

        private ItemBundle itemBundle;
        private Machine _machine;

        private bool ignoreYes = (Globals.Core.CharacterFilter.CharacterOptions & 0x80000000) == 0;


        public BotInfinites_LeatherState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            _machine = machine;

            var tool = Util.GetInventoryItemByName("Infinite Leather");
            if (tool == null) {
                ChatManager.Tell(itemBundle.GetOwner(), "Error: Unable to find Infinite Leather.");
                machine.ChangeState(new BotFinishState(itemBundle));
                return;
            }
            toolId = tool.Id;

            ChatManager.Tell(itemBundle.GetOwner(), "Ok, applying some leather.");

            CoreManager.Current.ChatBoxMessage += Current_ChatBoxMessage;
        }

        private void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e) {
            try {
                if (e.Text.StartsWith("You apply")) {
                    isTinkering = false;
                    lastAction = DateTime.UtcNow;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public void Exit(Machine machine) {
            CoreManager.Current.ChatBoxMessage -= Current_ChatBoxMessage;
        }

        private DateTime lastThought = DateTime.UtcNow;
        private DateTime startTime = DateTime.UtcNow;
        private DateTime lastAction = DateTime.UtcNow;
        private List<int> finishedItems = new List<int>();
        private bool isTinkering;

        public void Think(Machine machine) {
            try {
                if (DateTime.UtcNow - lastThought > TimeSpan.FromMilliseconds(500)) {
                    lastThought = DateTime.UtcNow;

                    if (DateTime.UtcNow - lastAction > TimeSpan.FromSeconds(5) || DateTime.UtcNow - startTime > TimeSpan.FromSeconds(60)) {
                        ChatManager.Tell(itemBundle.GetOwner(), "Timed out. I was unable to apply the leather.");
                        itemBundle.SetCraftMode(CraftMode.GiveBackItems);
                        machine.ChangeState(new BotTradingState(itemBundle));
                        return;
                    }

                    if (CoreManager.Current.Actions.BusyState != 0 || isTinkering)
                        return;

                    var items = itemBundle.GetItems();
                    foreach (var item in items) {
                        if (!finishedItems.Contains(item)) {
                            finishedItems.Add(item);
                            CoreManager.Current.Actions.ApplyItem(toolId, item);
                            isTinkering = true;
                            if (!ignoreYes)
                            {
                                PostMessageTools.ClickYes();
                            }
                            return;
                        }
                    }

                    itemBundle.SetCraftMode(CraftMode.GiveBackItems);
                    machine.ChangeState(new BotTradingState(itemBundle));
                }
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public ItemBundle GetItemBundle() {
            return null;
        }
    }
}
