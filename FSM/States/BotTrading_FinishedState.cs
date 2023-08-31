using System;
using System.Collections.Generic;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;

namespace DoThingsBot.FSM.States {
    public class BotTrading_FinishedState : IBotState {
        public string Name { get => "BotTrading_FinishedState"; }
        public ItemBundle itemBundle;

        public BotTrading_FinishedState(ItemBundle items) {
            itemBundle = items;
            itemBundle.SavePlayerData();
            
            if (itemBundle.GetCraftMode() != CraftMode.GiveBackItems) {
                if (itemBundle.GetSalvages().Count > 6) {
                    ChatManager.Tell(items.GetOwner(), String.Format("I will apply the salvages to your {0} in the following order:", Util.GetItemName(items.GetTargetItem())));
                    ChatManager.Tell(items.GetOwner(), String.Format("{0}", items.sortedSalvageNames));
                }
                else if (itemBundle.GetSalvages().Count > 1) {
                    ChatManager.Tell(items.GetOwner(), String.Format("I will apply the salvages to your {0} in this order: {1}", Util.GetItemName(items.GetTargetItem()), items.sortedSalvageNames));
                }
            }
            else {
                ChatManager.Tell(items.GetOwner(), "You should have all of your items back.  If you don't think so, try the lostitems command or leave me a message.");
            }
        }

        public ItemBundle GetItemBundle() {
            return itemBundle;
        }

        public void Enter(Machine machine) {
            CoreManager.Current.Actions.TradeEnd();

        }

        public void Exit(Machine machine) {

        }

        public void Think(Machine machine) {
        }
    }
}
