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
    class BotInfinites_RationsState : IBotState {
        public string Name { get => "BotTrading_ReturnItemsState"; }

        public int ThinkCount { get => _thinkCounter; }
        private int _thinkCounter = 0;
        private int toolId = 0;

        private ItemBundle itemBundle;
        private Machine _machine;

        public BotInfinites_RationsState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            _machine = machine;

            WorldObject player = Util.FindPlayerWorldObjectByName(itemBundle.GetOwner());

            if (player == null || Util.GetDistanceFromPlayer(player) > 2) {
                ChatManager.Tell(itemBundle.GetOwner(), "Please stand closer to me and try again.");

                _machine.ChangeState(new BotFinishState(itemBundle));
                return;
            }

            var tool = Util.GetInventoryItemByName("Cooking Pot");
            if (tool == null) {
                ChatManager.Tell(itemBundle.GetOwner(), $"I'm missing a Cooking Pot.  It {Recipes.GetToolLocation("Cooking Pot")}");
                _machine.ChangeState(new BotFinishState(itemBundle));
                return;
            }
            toolId = tool.Id;

            ChatManager.Tell(itemBundle.GetOwner(), "Ok, Making you some rations.");
        }

        public void Exit(Machine machine) {

        }
        
        private DateTime lastThought = DateTime.UtcNow;
        private DateTime startTime = DateTime.UtcNow;
        private bool shouldClickYes;

        void EchoFilter_ServerDispatch(object sender, NetworkMessageEventArgs e) {
            try {

                if (e.Message.Type == 0xF7B0 && (int)e.Message["event"] == 0x0274 && e.Message.Value<int>("type") == 5) {
                    Match match = Globals.PercentConfirmation.Match(e.Message.Value<string>("text"));

                    Util.WriteToChat("I got: " + e.Message.Value<string>("text"));
                    if (match.Success) {
                        shouldClickYes = true;
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public void Think(Machine machine) {
            try {
                if (DateTime.UtcNow - lastThought > TimeSpan.FromMilliseconds(500)) {
                    lastThought = DateTime.UtcNow;

                    if (DateTime.UtcNow - startTime > TimeSpan.FromSeconds(20)) {
                        ChatManager.Tell(itemBundle.GetOwner(), "Timed out. I was unable to craft rations.");
                        machine.ChangeState(new BotTrading_TradeCancelledState(itemBundle));
                        return;
                    }

                    var elaborateRations = Util.GetInventoryItemByName("Elaborate Field Rations");
                    var simpleRations = Util.GetInventoryItemByName("Simple Field Rations");
                    if (elaborateRations != null) {
                        Util.WriteToDebugLog($"Found elaborate rations: {elaborateRations.Name}({elaborateRations.Id:X8})");
                        itemBundle.AddWorldObject(elaborateRations, true);
                        _machine.ChangeState(new BotTradingState(itemBundle));
                    }
                    else if (simpleRations != null) {
                        Util.WriteToDebugLog($"Found simple rations: {simpleRations.Name}({simpleRations.Id:X8})");
                        itemBundle.AddWorldObject(simpleRations, true);
                        _machine.ChangeState(new BotTradingState(itemBundle));
                    }
                    else {
                        if (shouldClickYes) {
                            shouldClickYes = false;
                            PostMessageTools.ClickYes();
                            return;
                        }

                        var rations = Util.GetInventoryItemByName("Infinite Elaborate Dried Rations") ?? Util.GetInventoryItemByName("Infinite Simple Dried Rations");

                        if (rations == null) {
                            Chat.ChatManager.Tell(itemBundle.GetOwner(), "Something went wrong, unable to find infinite rations.");
                            _machine.ChangeState(new BotFinishState(itemBundle));
                            return;
                        }
                        var tool = CoreManager.Current.WorldFilter[toolId];
                        Util.WriteToDebugLog($"Making rations. Tool: {tool.Name}({toolId:X8}) Rations: {rations.Name}({rations.Id})");
                        CoreManager.Current.Actions.ApplyItem(toolId, rations.Id);
                        shouldClickYes = true;
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
