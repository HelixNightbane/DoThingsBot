using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using DoThingsBot.Lib;

namespace DoThingsBot.FSM.States {
    public class BotTinkering_ConfirmedState : IBotState {
        public string Name { get => "BotTinkering_ConfirmedState"; }
        public string PlayerName;

        public const int RETRY_DELAY = 4;
        public ItemBundle itemBundle;
        private Machine _machine;

        public BotTinkering_ConfirmedState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            _machine = machine;

            CoreManager.Current.ChatBoxMessage += new EventHandler<ChatTextInterceptEventArgs>(Current_ChatBoxMessage);


            // TODO: click yes on the confirm craft success window
            PostMessageTools.ClickYes();

            //You apply the aquamarine, but in the process you destroy the target.
            //Sunnuj Tinker fails to apply the White Sapphire Salvage (100) (workmanship 8.73) to the Ivory Blunt Sceptre. The target is destroyed.

            // You apply the aquamarine.
            // Sunnuj Tinker successfully applies the Imperial Topaz Salvage (100) (workmanship 7.20) to the Steel Slashing Crossbow.

            //machine.ChangeState(new BotTinkering_FinishedState(itemBundle));
        }

        public void Exit(Machine machine) {
            CoreManager.Current.ChatBoxMessage -= new EventHandler<ChatTextInterceptEventArgs>(Current_ChatBoxMessage);
        }

        void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e) {
            try {
                var characterName = Globals.Core.CharacterFilter.Name;
                var maxSuccess = Globals.Core.CharacterFilter.Augmentations.Contains((int)Augmentations.CharmedSmith) ? 38 : 38;
                string salvageType;
                string salvageWk;

                Regex CraftSuccess = new Regex("^" + characterName + @" successfully applies the ((?<salvage>[\w\s\-]+) Salvage([d]\s[\w]*|(\s?\(100\)))?\s\(workmanship (?<workmanship>\d+\.\d+)\)||(?<salvage>[\w\s\-]+) (Salvage|Salvaged||Foolproof)||Foolproof (?<salvage>[\w\s\-]+)) to the (?<item>[\w\s\-]+)\.$");
                Regex CraftFailure = new Regex("^" + characterName + @" fails to apply the ((?<salvage>[\w\s\-]+) Salvage([d]\s[\w]*|(\s?\(100\)))?\s\(workmanship (?<workmanship>\d+\.\d+)\)||(?<salvage>[\w\s\-]+) (Salvage|Salvaged||Foolproof)||Foolproof (?<salvage>[\w\s\-]+)) to the (?<item>[\w\s\-]+).\s?The target is destroyed\.$");

                Util.WriteToDebugLog(itemBundle.successChanceFullString);
                Util.WriteToDebugLog(e.Text);

                if (CraftSuccess.IsMatch(e.Text.Trim())) {
                    var match = CraftSuccess.Match(e.Text.Trim());
                    
                    salvageType = match.Groups["salvage"].Value;
                    salvageWk = string.IsNullOrEmpty(match.Groups["workmanship"].Value) ? "0" : match.Groups["workmanship"].Value;

                    itemBundle.tinkerCount++;

                    // successful imbue
                    if (itemBundle.successChance >= maxSuccess && itemBundle.IsImbue) {
                        Globals.Stats.AddPlayerImbuesLanded(itemBundle.GetOwner(), salvageType, 1);
                    }
                    else if (itemBundle.GetImbueSalvages().Count == 0) {
                        Globals.Stats.RecordTinkerSuccess(itemBundle.GetOwner(), salvageType + "(wk" + salvageWk + ")", itemBundle.successChance, match.Groups["item"].Value);
                    }

                    Globals.Stats.AddPlayerSalvageBagApplied(itemBundle.GetOwner(), salvageType, 1);

                    Util.WriteToDebugLog(e.Text);
                    
                    itemBundle.SetItemDestroyed(itemBundle.GetUseItemTarget());

                    if (itemBundle.HasItemsLeftToWorkOn()) {
                        System.Threading.Timer timer = null;
                        timer = new System.Threading.Timer((obj) => {
                            _machine.ChangeState(new BotTinkering_TrySuccessState(itemBundle));
                            timer.Dispose();
                        },
                                    null, 200, System.Threading.Timeout.Infinite);
                    }
                    else {
                        ChatManager.Tell(itemBundle.GetOwner(), "Wooo! We did it!");
                        _machine.ChangeState(new BotTinkering_FinishedState(itemBundle));
                    }

                    return;
                }

                if (CraftFailure.IsMatch(e.Text.Trim())) {
                    var match = CraftFailure.Match(e.Text.Trim());

                    salvageType = match.Groups["salvage"].Value;
                    salvageWk = string.IsNullOrEmpty(match.Groups["workmanship"].Value) ? "0" : match.Groups["workmanship"].Value;

                    Util.WriteToChat(string.Format("Failed. is: {0}, max: {1}, c: {2}", itemBundle.successChance, maxSuccess, itemBundle.GetImbueSalvages().Count));

                    // failed imbue
                    if (itemBundle.successChance >= maxSuccess && itemBundle.IsImbue) {
                        Globals.Stats.AddPlayerImbuesFailed(itemBundle.GetOwner(), salvageType, 1);
                    }
                    else if (itemBundle.GetImbueSalvages().Count == 0) {
                        Globals.Stats.RecordTinkerFailure(itemBundle.GetOwner(), salvageType + "(wk" + salvageWk + ")", itemBundle.successChance, match.Groups["item"].Value);
                    }

                    Globals.Stats.AddPlayerSalvageBagApplied(itemBundle.GetOwner(), salvageType, 1);

                    ChatManager.Tell(itemBundle.GetOwner(), "Ouch!  Maybe next time we'll have better luck.");

                    itemBundle.SetItemDestroyed(itemBundle.GetUseItemTarget());
                    itemBundle.SetItemDestroyed(itemBundle.GetUseItemOnTarget());
                    
                    _machine.ChangeState(new BotTinkering_FinishedState(itemBundle));

                    return;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        DateTime firstThought = DateTime.UtcNow;
        bool didFail = false;

        public void Think(Machine machine) {
            if (DateTime.UtcNow - firstThought > TimeSpan.FromSeconds(5)) {
                if (!didFail) {
                    didFail = true;
                    ChatManager.Tell(itemBundle.GetOwner(), "The tinkering request timed out, probably because something went wrong.");
                    _machine.ChangeState(new BotTinkering_CancelledState(itemBundle));
                }
                return;
            }
        }

        public ItemBundle GetItemBundle() {
            return itemBundle;
        }
    }
}
