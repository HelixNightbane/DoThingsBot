using Decal.Adapter;
using DoThingsBot.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace DoThingsBot.FSM.States {
    public class BotTradingState : IBotState {
        public string Name { get => "BotTradingState"; }
        public string PlayerName;

        private Machine _machine;
        private Machine parentMachine;
        private bool IsRunning = false;
        private ItemBundle itemBundle;

        public BotTradingState(ItemBundle items) {
            _machine = new Machine();
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            parentMachine = machine;
            IsRunning = true;

            ChatManager.RaiseChatCommandEvent += new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);

            _machine.SetParentState(this.Name);
            _machine.ChangeState(new BotTrading_OpenTradeState(itemBundle));
            _machine.Start();
        }

        public void Exit(Machine machine) {
            IsRunning = false;
            _machine.Stop();

            ChatManager.RaiseChatCommandEvent -= new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);
        }

        DateTime firstThought = DateTime.UtcNow;
        bool didFail = false;
        bool didCancel = false;

        void ChatManager_ChatCommand(object sender, ChatCommandEventArgs e) {
            try {
                switch (e.Command) {
                    case "cancel":
                        didCancel = true;
                        CoreManager.Current.Actions.TradeDecline();
                        parentMachine.ChangeState(new BotFinishState(itemBundle));
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
                    ChatManager.Tell(itemBundle.GetOwner(), "The trade request timed out, probably because something went wrong.  Tell me 'lostitems' if you think I have something of yours.");
                    machine.ChangeState(new BotFinishState(itemBundle));
                }
                return;
            }

            if (itemBundle.GetCraftMode() == CraftMode.GiveBackItems) {
                if (_machine.IsInState("BotTrading_TradeCancelledState") || _machine.IsInState("BotTrading_FinishedState")) {
                    machine.ChangeState(new BotFinishState(itemBundle));
                    return;
                }
            }
            else {
                if (_machine.IsInState("BotTrading_TradeCancelledState")) {
                    machine.ChangeState(new BotFinishState(itemBundle));
                    return;
                }
                else if (_machine.IsInState("BotTrading_FinishedState")) {
                    if (itemBundle.GetCraftMode() == CraftMode.WeaponTinkering && itemBundle.HasOnlyBuffItems()) {
                        machine.ChangeState(new BotTinkeringState(itemBundle));
                        return;
                    }
                    else if (itemBundle.GetCraftMode() == CraftMode.Crafting) {
                        itemBundle.SetEquipMode(EquipMode.Craft);
                        machine.ChangeState(new BotEquipItemsState(itemBundle));
                        return;
                    }
                    else if (itemBundle.GetCraftMode() == CraftMode.InfiniteLeather) {
                        machine.ChangeState(new BotInfinites_LeatherState(itemBundle));
                        return;
                    }
                    else if (itemBundle.GetCraftMode() == CraftMode.InfiniteDye) {
                        machine.ChangeState(new BotInfinites_ApplyDye(itemBundle));
                        return;
                    }
                    else if (itemBundle.targetType == CraftTargetType.Armor)
                    {
                        itemBundle.SetEquipMode(EquipMode.TinkerArmor);
                        machine.ChangeState(new BotEquipItemsState(itemBundle));
                        return;
                    }
                    else if (itemBundle.targetType == CraftTargetType.Weapon && itemBundle.weaponType == WeaponType.Wand)
                    {
                        itemBundle.SetEquipMode(EquipMode.TinkerMagic);
                        machine.ChangeState(new BotEquipItemsState(itemBundle));
                        return;
                    }
                    else if (itemBundle.targetType == CraftTargetType.Weapon)
                    {
                        itemBundle.SetEquipMode(EquipMode.TinkerWeapon);
                        machine.ChangeState(new BotEquipItemsState(itemBundle));
                        return;
                    }
                    else if (itemBundle.targetType == CraftTargetType.Jewelry)
                    {
                        itemBundle.SetEquipMode(EquipMode.TinkerItem);
                        machine.ChangeState(new BotEquipItemsState(itemBundle));
                        return;
                    }
                    itemBundle.SetEquipMode(EquipMode.TinkerArmor);
                    machine.ChangeState(new BotEquipItemsState(itemBundle));
                    return;
                }
            }

            if (IsRunning) _machine.Think();
            //Util.WriteToChat(String.Format("{0}: Thinking", Name));
        }

        public ItemBundle GetItemBundle() {
            return null;
        }
    }
}
