using Decal.Adapter;
using DoThingsBot.Chat;
using DoThingsBot.Lib;
using System;
using System.Collections.Generic;
using System.Text;

namespace DoThingsBot.FSM.States {
    class BotFinishState : IBotState {
        public string Name { get => "BotFinishState"; }
        public ItemBundle itemBundle;

        public BotFinishState(ItemBundle items) {
            itemBundle = items;
            if (itemBundle.HasOwner()) {
                itemBundle.SavePlayerData();
            }

            if (itemBundle.HasOwner() && !itemBundle.IsPaused) {
                itemBundle.Unpause();
            }

            Globals.Stats.globalStats.Save();

            Globals.DoThingsBot.currentItemBundle = null;
            Globals.DoThingsBot.needsEquipmentCheck = true;

            try {
                PostMessageTools.ClickNo();
            }
            catch (Exception e) { Util.LogException(e); }

            CoreManager.Current.Actions.TradeEnd();
        }

        public void Enter(Machine machine) {
            if (ComponentManager.IsLowOnComps() && Config.Bot.AnnounceLowComponentsAfterJob.Value) {
                ChatManager.AddSpamToChatBox("/s " + ComponentManager.LowComponentAnnouncement());
            }

            Util.WriteToChat($"IsLowOnComps? {ComponentManager.IsLowOnComps()} and afterJob {Config.Bot.AnnounceLowComponentsAfterJob.Value}");
            Util.WriteToChat($"LowMessage: {ComponentManager.LowComponentAnnouncement()}");

            if (itemBundle.playerData != null && itemBundle.playerData.jobType == "tinker" && Config.Tinkering.KeepEquipmentOnDelay.Value > 0) {
                machine.ChangeState(new BotIdleState());
            }
            else if (itemBundle.playerData != null && itemBundle.playerData.jobType == "craft" && Config.CraftBot.KeepEquipmentOnDelay.Value > 0) {
                machine.ChangeState(new BotIdleState());
            }
            else {
                itemBundle.SetEquipMode(EquipMode.Idle);
                machine.ChangeState(new BotEquipItemsState(GetItemBundle()));
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
