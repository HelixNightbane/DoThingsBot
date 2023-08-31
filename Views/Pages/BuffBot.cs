using System;
using System.Collections.Generic;
using System.Globalization;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {

    class BuffBotPage : IDisposable {
        HudCheckBox UIBotBuffBotEnableTreeStatsBuffs { get; set; }
        HudCheckBox UIBotBuffBotEnableSingleBuffs { get; set; }
        HudCheckBox UIBotBuffBotAlwaysEnableBanes { get; set; }
        HudButton UIBotBuffBotReloadBuffProfiles { get; set; }
        HudButton UIBotBuffBotManageBuffProfiles { get; set; }
        HudTextBox UIBotBuffBotLimitBuffOtherLevel { get; set; }
        HudFixedLayout UIBuffBotLayout { get; set; }
        HudHSlider UIBotBuffBotGetManaAt { get; set; }
        HudHSlider UIBotBuffBotGetStaminaAt { get; set; }
        HudStaticText UIBotBuffBotGetManaAtDisplay { get; set; }
        HudStaticText UIBotBuffBotGetStaminaAtDisplay { get; set; }

        MainView mainView;

        public BuffBotPage(MainView mainView) {
            try {
                this.mainView = mainView;

                UIBotBuffBotReloadBuffProfiles = (HudButton)mainView.view["UIBotBuffBotReloadBuffProfiles"];
                UIBotBuffBotManageBuffProfiles = (HudButton)mainView.view["UIBotBuffBotManageBuffProfiles"];
                UIBuffBotLayout = (HudFixedLayout)mainView.view["UIBuffBotLayout"];

                UIBotBuffBotEnableTreeStatsBuffs = (HudCheckBox)mainView.view["UIBotBuffBotEnableTreeStatsBuffs"];
                UIBotBuffBotEnableTreeStatsBuffs.Checked = Config.BuffBot.EnableTreeStatsBuffs.Value;
                Config.BuffBot.EnableTreeStatsBuffs.Changed += obj => { UIBotBuffBotEnableTreeStatsBuffs.Checked = obj.Value; };
                UIBotBuffBotEnableTreeStatsBuffs.Change += (s, e) => { try { Config.BuffBot.EnableTreeStatsBuffs.Value = ((HudCheckBox)s).Checked; } catch (Exception ex) { Util.LogException(ex); } };

                UIBotBuffBotEnableSingleBuffs = (HudCheckBox)mainView.view["UIBotBuffBotEnableSingleBuffs"];
                UIBotBuffBotEnableSingleBuffs.Checked = Config.BuffBot.EnableSingleBuffs.Value;
                Config.BuffBot.EnableSingleBuffs.Changed += obj => { UIBotBuffBotEnableSingleBuffs.Checked = obj.Value; };
                UIBotBuffBotEnableSingleBuffs.Change += (s, e) => {
                    try {
                        Config.BuffBot.EnableSingleBuffs.Value = ((HudCheckBox)s).Checked;
                        Buffs.Buffs.ReloadProfiles();
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIBotBuffBotAlwaysEnableBanes = (HudCheckBox)mainView.view["UIBotBuffBotAlwaysEnableBanes"];
                UIBotBuffBotAlwaysEnableBanes.Checked = Config.BuffBot.AlwaysEnableBanes.Value;
                Config.BuffBot.AlwaysEnableBanes.Changed += obj => { UIBotBuffBotAlwaysEnableBanes.Checked = obj.Value; };
                UIBotBuffBotAlwaysEnableBanes.Change += (s, e) => { try { Config.BuffBot.AlwaysEnableBanes.Value = ((HudCheckBox)s).Checked; } catch (Exception ex) { Util.LogException(ex); } };

                UIBotBuffBotReloadBuffProfiles.Hit += (a, b) => {
                    try {
                        Buffs.Buffs.ReloadProfiles();
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIBotBuffBotManageBuffProfiles.Hit += (a, b) => {
                    try {
                        Globals.ProfileManagerView.EditBuffProfiles();
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                //UIBotBuffBotLimitBuffOtherLevel
                UIBotBuffBotLimitBuffOtherLevel = (HudTextBox)mainView.view["UIBotBuffBotLimitBuffOtherLevel"];
                UIBotBuffBotLimitBuffOtherLevel.Text = Config.BuffBot.LimitBuffOtherLevel.Value.ToString();
                Config.BuffBot.LimitBuffOtherLevel.Changed += obj => { UIBotBuffBotLimitBuffOtherLevel.Text = obj.Value.ToString(); };
                UIBotBuffBotLimitBuffOtherLevel.LostFocus += (s, e) => {
                    try {
                        if (!int.TryParse(UIBotBuffBotLimitBuffOtherLevel.Text, out int value))
                            value = Config.BuffBot.LimitBuffOtherLevel.Value;
                        Config.BuffBot.LimitBuffOtherLevel.Value = value;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIBotBuffBotGetManaAtDisplay = (HudStaticText)mainView.view["UIBotBuffBotGetManaAtDisplay"];
                UIBotBuffBotGetManaAtDisplay.Text = $"{Config.BuffBot.GetManaAt.Value}%";
                UIBotBuffBotGetManaAt = (HudHSlider)mainView.view["UIBotBuffBotGetManaAt"];
                UIBotBuffBotGetManaAt.Position = Config.BuffBot.GetManaAt.Value;
                UIBotBuffBotGetManaAt.Changed += (min, max, pos) => {
                    UIBotBuffBotGetManaAtDisplay.Text = $"{pos}%";
                    Config.BuffBot.GetManaAt.Value = pos;
                };
                Config.BuffBot.GetManaAt.Changed += (obj) => {
                    UIBotBuffBotGetManaAtDisplay.Text = $"{Config.BuffBot.GetManaAt.Value}%";
                    UIBotBuffBotGetManaAt.Position = Config.BuffBot.GetManaAt.Value;
                };

                UIBotBuffBotGetStaminaAtDisplay = (HudStaticText)mainView.view["UIBotBuffBotGetStaminaAtDisplay"];
                UIBotBuffBotGetStaminaAtDisplay.Text = $"{Config.BuffBot.GetStaminaAt.Value}%";
                UIBotBuffBotGetStaminaAt = (HudHSlider)mainView.view["UIBotBuffBotGetStaminaAt"];
                UIBotBuffBotGetStaminaAt.Position = Config.BuffBot.GetStaminaAt.Value;
                UIBotBuffBotGetStaminaAt.Changed += (min, max, pos) => {
                    UIBotBuffBotGetStaminaAtDisplay.Text = $"{pos}";
                    Config.BuffBot.GetStaminaAt.Value = pos;
                };
                Config.BuffBot.GetStaminaAt.Changed += (obj) => {
                    UIBotBuffBotGetStaminaAtDisplay.Text = $"{Config.BuffBot.GetStaminaAt.Value}%";
                    UIBotBuffBotGetStaminaAt.Position = Config.BuffBot.GetStaminaAt.Value;
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