using DoThingsBot.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace DoThingsBot.FSM.States {
    class BotStartState : IBotState {
        public string Name { get => "BotStartState"; }
        public ItemBundle itemBundle;

        public BotStartState(ItemBundle items) {
            itemBundle = items;
        }

        public BotStartState() {
            throw new Exception("BotStartState() called without itemBundle");
        }

        public void Enter(Machine machine) {
            if (itemBundle.GetForceBuffMode() == true) {
                itemBundle.SetEquipMode(EquipMode.Buff);
                itemBundle.SetCraftMode(CraftMode.None);

                machine.ChangeState(new BotEquipItemsState(itemBundle));
                return;
            }

            if (itemBundle.HasOwner()) {
                itemBundle.MarkAsActive();

                if (Globals.StatsView.view.Visible && Globals.StatsView.StatTabs.CurrentTab == 2) {
                    Globals.StatsView.ShowCharacterStats(itemBundle.GetOwner());
                }

                if (itemBundle.IsPortalCraftMode()) {
                    if (itemBundle.GetCraftMode() == CraftMode.PortalGem) {
                        machine.ChangeState(new BotSummonPortalState(itemBundle));
                    }
                    else {
                        machine.ChangeState(new BotEquipItemsState(itemBundle));
                    }
                }
                else if (itemBundle.GetCraftMode() == CraftMode.Brill)
                {
                    itemBundle.SetEquipMode(EquipMode.Brill);
                    machine.ChangeState(new BotEquipItemsState(itemBundle));
                }
                else if (itemBundle.GetCraftMode() == CraftMode.WeaponTinkering && Spells.DoesAnySpellNeedRefresh(Config.Bot.GetWantedTinkerEnchantments())) {
                    if (!itemBundle.WasPaused) ChatManager.Tell(itemBundle.GetOwner(), "One moment please, I need to buff.");

                    itemBundle.SetEquipMode(EquipMode.Buff);
                    machine.ChangeState(new BotEquipItemsState(itemBundle));
                }
                else if (itemBundle.GetCraftMode() == CraftMode.Crafting && Spells.DoesAnySpellNeedRefresh(Config.Bot.GetWantedCraftingEnchantments())) {
                    if (!itemBundle.WasPaused) ChatManager.Tell(itemBundle.GetOwner(), "One moment please, I need to buff.");

                    itemBundle.SetEquipMode(EquipMode.Buff);
                    machine.ChangeState(new BotEquipItemsState(itemBundle));
                }
                else if (itemBundle.GetCraftMode() == CraftMode.Buff && Spells.DoesAnySpellNeedRefresh(Config.Bot.GetWantedBuffEnchantments())) {
                    if (!itemBundle.WasPaused) ChatManager.Tell(itemBundle.GetOwner(), "One moment please, I need to buff.");

                    itemBundle.SetEquipMode(EquipMode.Buff);
                    machine.ChangeState(new BotEquipItemsState(itemBundle));
                }
                else if (itemBundle.GetCraftMode() == CraftMode.InfiniteRations) {
                    machine.ChangeState(new BotInfinites_RationsState(itemBundle));
                }
                else if (itemBundle.GetCraftMode() == CraftMode.InfiniteLeather) {
                    machine.ChangeState(new BotTradingState(itemBundle));
                }
                else if (itemBundle.GetCraftMode() == CraftMode.InfiniteDye) {
                    machine.ChangeState(new BotInfinites_ChooseDyeState(itemBundle));
                }
                else {
                    if (itemBundle.GetCraftMode() == CraftMode.CheckSkills) {
                        itemBundle.SetEquipMode(EquipMode.Tinker);
                        machine.ChangeState(new BotEquipItemsState(itemBundle));
                    }
                    else if (itemBundle.GetCraftMode() == CraftMode.Buff) {
                        itemBundle.SetEquipMode(EquipMode.Buff);
                        machine.ChangeState(new BotEquipItemsState(itemBundle));
                    }
                    else {
                        if (itemBundle.WasPaused) {
                            machine.ChangeState(new BotEquipItemsState(itemBundle));
                        }
                        else {
                            itemBundle.SetEquipMode(EquipMode.Idle);
                            machine.ChangeState(new BotTradingState(itemBundle));
                        }
                    }
                }
            }
            else {
                itemBundle.SetEquipMode(EquipMode.Buff);
                itemBundle.SetCraftMode(CraftMode.None);

                machine.ChangeState(new BotEquipItemsState(itemBundle));
            }
        }

        public void Exit(Machine machine) {

        }

        public void Think(Machine machine) {

        }

        public ItemBundle GetItemBundle() {
            return itemBundle;
        }
    }
}
