using Decal.Adapter;
using DoThingsBot.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace DoThingsBot.FSM.States {
    class BotCheckStolenItemsState : IBotState {
        public string Name { get => "BotCheckStolenItemsState"; }
        private DateTime lastThought = DateTime.MinValue;


        private ItemBundle itemBundle;
        private List<int> idsRequested = new List<int>();

        public BotCheckStolenItemsState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            try {
                if (itemBundle.GetStolenItems().Count <= 0) {
                    ChatManager.Tell(itemBundle.GetOwner(), "I don't think I have anything of yours...");
                    machine.ChangeState(new BotIdleState());
                    return;
                }
                else {
                    itemBundle.AddStolenItemsToMainItems();

                    foreach (var id in itemBundle.GetItems().ToArray()) {
                        bool hasItemInInventory = false;

                        foreach (var wo in CoreManager.Current.WorldFilter.GetInventory()) {
                            if (id == wo.Id) {
                                hasItemInInventory = true;
                                break;
                            }
                        }

                        if (!hasItemInInventory) {
                            itemBundle.RemoveItem(id);
                            Util.WriteToChat("Removed " + id + " because it seems to be missing from inventory");
                        }
                    }

                    if (itemBundle.GetItems().Count > 0) {
                        ChatManager.Tell(itemBundle.GetOwner(), "I will attempt to return your: " + itemBundle.GetItemNames(true));
                        machine.ChangeState(new BotTrading_ReturnItemsState(itemBundle));
                        return;
                    }
                    else {
                        ChatManager.Tell(itemBundle.GetOwner(), "I don't think I have anything of yours...");
                        machine.ChangeState(new BotIdleState());
                        return;
                    }
                }

            }
            catch (Exception e) {
                Util.LogException(e);
                machine.ChangeState(new BotIdleState());
            }
        }

        public void Exit(Machine machine) {
            //Util.WriteToChat("Exited Idle State.");
        }

        private DateTime startedThinking = DateTime.UtcNow;

        public void Think(Machine machine) {
            if (DateTime.UtcNow - startedThinking > TimeSpan.FromSeconds(60)) {
                ChatManager.Tell(itemBundle.GetOwner(), "Something went wrong... I told my owner about it.");
                machine.ChangeState(new BotIdleState());
                return;
            }
        }

        public ItemBundle GetItemBundle() {
            return null;
        }
    }
}
