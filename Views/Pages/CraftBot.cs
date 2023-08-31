using System;
using System.Collections.Generic;
using System.Globalization;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {

    class CraftBotPage : IDisposable {
        HudCheckBox UICraftBotPauseSessionForOtherJobs { get; set; }
        HudCheckBox UICraftBotSkipMaxSuccessConfirmations { get; set; }
        HudTextBox UICraftBotLimitCraftingSessionsToSeconds { get; set; }
        HudTextBox UIKeepCraftEquipmentWhileIdleDelay { get; set; }

        MainView mainView;

        public CraftBotPage(MainView mainView) {
            try {
                this.mainView = mainView;

                UICraftBotPauseSessionForOtherJobs = (HudCheckBox)mainView.view["UICraftBotPauseSessionForOtherJobs"];
                UICraftBotSkipMaxSuccessConfirmations = (HudCheckBox)mainView.view["UICraftBotSkipMaxSuccessConfirmations"];
                UICraftBotLimitCraftingSessionsToSeconds = (HudTextBox)mainView.view["UICraftBotLimitCraftingSessionsToSeconds"];
                UIKeepCraftEquipmentWhileIdleDelay = (HudTextBox)mainView.view["UIKeepCraftEquipmentWhileIdleDelay"];

                UICraftBotPauseSessionForOtherJobs.Checked = Config.CraftBot.PauseSessionForOtherJobs.Value;
                Config.CraftBot.PauseSessionForOtherJobs.Changed += obj => { UICraftBotPauseSessionForOtherJobs.Checked = obj.Value; };
                UICraftBotPauseSessionForOtherJobs.Change += (s, e) => {
                    try {
                        Config.CraftBot.PauseSessionForOtherJobs.Value = ((HudCheckBox)s).Checked;
                    } catch (Exception ex) { Util.LogException(ex); }
                };

                UICraftBotSkipMaxSuccessConfirmations.Checked = Config.CraftBot.SkipMaxSuccessConfirmation.Value;
                Config.CraftBot.SkipMaxSuccessConfirmation.Changed += obj => { UICraftBotSkipMaxSuccessConfirmations.Checked = obj.Value; };
                UICraftBotSkipMaxSuccessConfirmations.Change += (s, e) => {
                    try {
                        Config.CraftBot.SkipMaxSuccessConfirmation.Value = ((HudCheckBox)s).Checked;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UICraftBotLimitCraftingSessionsToSeconds.Text = Config.CraftBot.LimitCraftingSessionsToSeconds.Value.ToString();
                Config.CraftBot.LimitCraftingSessionsToSeconds.Changed += obj => { UICraftBotLimitCraftingSessionsToSeconds.Text = obj.Value.ToString(); };
                UICraftBotLimitCraftingSessionsToSeconds.LostFocus += (s, e) => {
                    try {
                        if (!int.TryParse(UICraftBotLimitCraftingSessionsToSeconds.Text, out int value))
                            value = Config.CraftBot.LimitCraftingSessionsToSeconds.Value;
                        Config.CraftBot.LimitCraftingSessionsToSeconds.Value = value;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIKeepCraftEquipmentWhileIdleDelay.Text = Config.CraftBot.KeepEquipmentOnDelay.Value.ToString(CultureInfo.InvariantCulture);
                Config.CraftBot.KeepEquipmentOnDelay.Changed += obj => { UIKeepCraftEquipmentWhileIdleDelay.Text = obj.Value.ToString(CultureInfo.InvariantCulture); };
                UIKeepCraftEquipmentWhileIdleDelay.LostFocus += (s, e) => {
                    try {
                        if (!int.TryParse(UIKeepCraftEquipmentWhileIdleDelay.Text, out int value))
                            value = Config.CraftBot.KeepEquipmentOnDelay.Value;
                        Config.CraftBot.KeepEquipmentOnDelay.Value = value;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private bool disposed;

        public void Dispose() {
            try {
                Dispose(true);

                GC.SuppressFinalize(this);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        protected virtual void Dispose(bool disposing) {
            try {
                if (!disposed) {
                    if (disposing) {

                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}