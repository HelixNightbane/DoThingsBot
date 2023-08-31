using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using DoThingsBot.Lib;

namespace DoThingsBot.FSM.States {
    public class BotTinkering_UseBuffItemsState : IBotState {
        public string Name { get => "BotTinkering_UseBuffItemsState"; }
        public ItemBundle itemBundle;
        public DateTime lastThought = DateTime.UtcNow;
        public DateTime firstThought = DateTime.UtcNow;
        public bool didUseRareAndNeedsConfirmation = false;
        public bool waitingOnUseConfirmation = false;
        public WorldObject usingItem;

        public BotTinkering_UseBuffItemsState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            try {
                if (!itemBundle.HasBuffItems()) {
                    machine.ChangeState(new BotTinkering_TrySuccessState(itemBundle));
                }
                else {
                    CoreManager.Current.ChatBoxMessage += Current_ChatBoxMessage;
                }
            }
            catch (Exception e) { Util.LogException(e); }
        }

        private void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e) {
            try {
                var wo = usingItem;
                if (wo == null) return;

                Util.WriteToChat(e.Text);

                if (e.Color == 7 && e.Text.StartsWith(usingItem.Name)) {
                    waitingOnUseConfirmation = false;
                    itemBundle.RemoveItem(wo.Id);
                    Util.WriteToDebugLog($"Used: {wo.Name} ({wo.Id})");
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public void Exit(Machine machine) {
            try {
                CoreManager.Current.ChatBoxMessage -= Current_ChatBoxMessage;
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public void Think(Machine machine) {
            if (DateTime.UtcNow - lastThought > TimeSpan.FromSeconds(1)) {
                lastThought = DateTime.UtcNow;

                if (DateTime.UtcNow - firstThought > TimeSpan.FromSeconds(15)) {
                    machine.ChangeState(new BotTinkering_FinishedState(itemBundle));
                    Chat.ChatManager.Tell(itemBundle.GetOwner(), "Something went wrong using the buff items.");
                    return;
                }

                if (didUseRareAndNeedsConfirmation) {
                    PostMessageTools.ClickYes();
                    didUseRareAndNeedsConfirmation = false;

                    Util.WriteToDebugLog("Clicking yes to confirm rare usage.");

                    return;
                }

                if (CoreManager.Current.Actions.BusyState != 0 || waitingOnUseConfirmation) {
                    return;
                }

                if (itemBundle.HasBuffItems()) {
                    var itemId = itemBundle.GetBuffItem();

                    if (Util.IsRare(CoreManager.Current.WorldFilter[itemId]) && (uint)(Globals.Core.CharacterFilter.CharacterOptionFlags & 0x40000) != 0) {
                        didUseRareAndNeedsConfirmation = true;
                    }

                    var wo = CoreManager.Current.WorldFilter[itemId];

                    if (wo != null) {
                        Util.WriteToDebugLog($"Using buff item: {wo.Name}");
                        usingItem = wo;
                    }

                    CoreManager.Current.Actions.UseItem(itemId, 0);
                    waitingOnUseConfirmation = true;

                    return;
                }

                if (!itemBundle.HasItems()) {
                    machine.ChangeState(new BotTinkering_FinishedState(itemBundle));
                    Chat.ChatManager.Tell(itemBundle.GetOwner(), "Thanks for the buffs!");
                }
                else {
                    machine.ChangeState(new BotTinkering_TrySuccessState(itemBundle));
                }

            }
        }

        public ItemBundle GetItemBundle() {
            return itemBundle;
        }
    }
}
