using System;
using System.Collections.Generic;
using System.Globalization;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {

    class TinkerBotPage : IDisposable {
        HudTextBox UIKeepTinkerEquipmentWhileIdleDelay { get; set; }
        HudCheckBox UITinkerBotSkipMaxSuccessConfirmations { get; set; }

        MainView mainView;

        public TinkerBotPage(MainView mainView) {
            try {
                this.mainView = mainView;

                UITinkerBotSkipMaxSuccessConfirmations = (HudCheckBox)mainView.view["UITinkerBotSkipMaxSuccessConfirmations"];
                UIKeepTinkerEquipmentWhileIdleDelay = (HudTextBox)mainView.view["UIKeepTinkerEquipmentWhileIdleDelay"];

                UITinkerBotSkipMaxSuccessConfirmations.Checked = Config.Tinkering.SkipMaxSuccessConfirmation.Value;
                Config.Tinkering.SkipMaxSuccessConfirmation.Changed += obj => { UITinkerBotSkipMaxSuccessConfirmations.Checked = obj.Value; };
                UITinkerBotSkipMaxSuccessConfirmations.Change += (s, e) => {
                    try {
                        Config.Tinkering.SkipMaxSuccessConfirmation.Value = ((HudCheckBox)s).Checked;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIKeepTinkerEquipmentWhileIdleDelay.Text = Config.Tinkering.KeepEquipmentOnDelay.Value.ToString(CultureInfo.InvariantCulture);
                Config.Tinkering.KeepEquipmentOnDelay.Changed += obj => { UIKeepTinkerEquipmentWhileIdleDelay.Text = obj.Value.ToString(CultureInfo.InvariantCulture); };
                UIKeepTinkerEquipmentWhileIdleDelay.LostFocus += (s, e) => {
                    try {
                        if (!int.TryParse(UIKeepTinkerEquipmentWhileIdleDelay.Text, out int value))
                            value = Config.Tinkering.KeepEquipmentOnDelay.Value;
                        Config.Tinkering.KeepEquipmentOnDelay.Value = value;
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