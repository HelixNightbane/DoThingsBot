using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Text;

namespace DoThingsBot.FSM.States {
    public class BotBuffingState : IBotState {
        public string Name { get => "BotBuffingState"; }

        private Machine _machine;
        private bool IsRunning = false;
        private ItemBundle itemBundle;

        public BotBuffingState(ItemBundle items) {
            _machine = new Machine();
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            IsRunning = true;

            _machine.SetParentState(this.Name);
            _machine.ChangeState(new BotBuffing_EnsureBuffedState(itemBundle));
            _machine.Start();
        }

        public void Exit(Machine machine) {
            IsRunning = false;
            _machine.Stop();

        }

        public void Think(Machine machine) {
            if (_machine.IsInState("BotBuffing_FinishedState")) {
                if (itemBundle.GetCraftMode() == CraftMode.None) {
                    itemBundle.SetEquipMode(EquipMode.Idle);
                    machine.ChangeState(new BotEquipItemsState(_machine.CurrentState.GetItemBundle()));
                }
                else if (itemBundle.GetCraftMode() == CraftMode.CheckSkills) {
                    itemBundle.SetEquipMode(EquipMode.Tinker);
                    machine.ChangeState(new BotEquipItemsState(_machine.CurrentState.GetItemBundle()));
                }
                else if (itemBundle.GetCraftMode() == CraftMode.Buff) {
                    machine.ChangeState(new BotBuffState(_machine.CurrentState.GetItemBundle()));
                }
                else {
                    if (itemBundle.WasPaused) {
                        if (itemBundle.GetCraftMode() == CraftMode.Crafting) {
                            machine.ChangeState(new BotCraftingState(_machine.CurrentState.GetItemBundle()));
                        }
                        else if (itemBundle.GetCraftMode() == CraftMode.WeaponTinkering) {
                            machine.ChangeState(new BotTinkeringState(_machine.CurrentState.GetItemBundle()));
                        }
                    }
                    else {
                        machine.ChangeState(new BotTradingState(_machine.CurrentState.GetItemBundle()));
                    }
                }

                return;
            }

            if (IsRunning) _machine.Think();
        }

        public ItemBundle GetItemBundle() {
            return itemBundle;
        }
    }
}
