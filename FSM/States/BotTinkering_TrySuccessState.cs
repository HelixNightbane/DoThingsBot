using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using DoThingsBot.Lib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DoThingsBot.FSM.States {
    public class BotTinkering_TrySuccessState : IBotState {
        public string Name { get => "BotTinkering_TrySuccessState"; }

        public int ThinkCount { get => _thinkCounter; }
        private int _thinkCounter = 0;

        private ItemBundle itemBundle;
        int useItem;
        int targetItem;
        Machine _machine;

        public BotTinkering_TrySuccessState(ItemBundle items) {
            try {
                itemBundle = items;
             }
            catch (Exception e) { Util.LogException(e); }
        }

        public void Enter(Machine machine) {
            try {
                CoreManager.Current.EchoFilter.ServerDispatch += new EventHandler<NetworkMessageEventArgs>(EchoFilter_ServerDispatch);

                _machine = machine;
                try {
                    PostMessageTools.ClickNo();
                }
                catch (Exception e) { Util.LogException(e); }

                itemBundle.SetItemTargets();

                useItem = itemBundle.GetUseItemTarget();
                targetItem = itemBundle.GetUseItemOnTarget();
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public void Exit(Machine machine) {
            try {
                CoreManager.Current.EchoFilter.ServerDispatch -= new EventHandler<NetworkMessageEventArgs>(EchoFilter_ServerDispatch);
            }
            catch (Exception e) { Util.LogException(e); }
        }

        private bool hasDoneCrafting = false;
        private List<int> requestedIds = new List<int>();

        private DateTime lastThought = DateTime.MinValue;
        private DateTime firstThought = DateTime.UtcNow;
        private bool didFail = false;

        public void Think(Machine machine) {
            try {
                if (DateTime.UtcNow - firstThought > TimeSpan.FromSeconds(10)) {
                    if (!didFail) {
                        didFail = true;
                        ChatManager.Tell(itemBundle.GetOwner(), "The tinkering request timed out, probably because something went wrong.");
                        _machine.ChangeState(new BotTinkering_CancelledState(itemBundle));
                    }
                    return;
                }

                if (DateTime.UtcNow - lastThought > TimeSpan.FromSeconds(2)) {
                    lastThought = DateTime.UtcNow;
                    bool itemsNeedIds = false;

                    foreach (int id in itemBundle.GetItems()) {
                        WorldObject wo = CoreManager.Current.WorldFilter[id];

                        if (!wo.HasIdData && !requestedIds.Contains(wo.Id)) {
                            itemsNeedIds = true;
                            requestedIds.Add(wo.Id);
                            CoreManager.Current.Actions.RequestId(wo.Id);
                        }
                    }

                    if (itemsNeedIds) {
                        return;
                    }

                    if (CoreManager.Current.Actions.BusyState != 0) {
                        return;
                    }

                    if (!hasDoneCrafting) {
                        CoreManager.Current.Actions.ApplyItem(useItem, targetItem);
                    }

                    //Util.WriteToChat(String.Format("{0}: Thinking", Name));
                }
            }
            catch (Exception e) { Util.LogException(e); }
        }
        private bool didFinish = false;

        void EchoFilter_ServerDispatch(object sender, NetworkMessageEventArgs e) {
            try {
                if (didFinish) return;

                if (e.Message.Type == 0xF7B0 && (int)e.Message["event"] == 0x0274 && e.Message.Value<int>("type") == 5) {
                    Match match = Globals.PercentConfirmation.Match(e.Message.Value<string>("text"));

                    // Util.WriteToChat("I got: " + e.Message.Value<string>("text"));
                    // You have a 33.3% chance of using Black Garnet Salvage (100) on Green Jade Heavy Crossbow.

                    if (match.Success) {
                        double percent;

                        double.TryParse(match.Groups["percent"].Value, out percent);

                        itemBundle.successChanceFullString = match.Groups["msg"].Value;
                        itemBundle.successChance = percent;

                        var toolWo = CoreManager.Current.WorldFilter[useItem];
                        var targetWo = CoreManager.Current.WorldFilter[targetItem];

                        if (toolWo != null && targetWo != null) {
                            var toolName = Util.GetItemName(toolWo);
                            var targetName = Util.GetItemName(targetWo);

                            itemBundle.successChanceFullString = $"I have a {percent}% chance of using {toolName} on {targetName} (tink {itemBundle.tinkerCount + 1})";
                        }

                        var maxSuccess = Globals.Core.CharacterFilter.Augmentations.Contains((int)Augmentations.CharmedSmith) ? 38 : 38;

                        if (percent >= 100) {
                            didFinish = true;
                            _machine.ChangeState(new BotTinkering_ConfirmedState(itemBundle));
                        }
                        else if (percent >= maxSuccess && itemBundle.GetImbueSalvages().Count == 1 && itemBundle.GetSalvages().Count == itemBundle.GetImbueSalvages().Count) {
                            didFinish = true;
                            _machine.ChangeState(new BotTinkering_ConfirmedState(itemBundle));
                        }
                        else {
                            didFinish = true;
                            _machine.ChangeState(new BotTinkering_AwaitCommandState(itemBundle));
                        }
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public ItemBundle GetItemBundle() {
            try {
                return itemBundle;
            }
            catch (Exception ex) { Util.LogException(ex); }

            return null;
        }
    }
}
