using Decal.Adapter;
using Decal.Filters;
using DoThingsBot.Lib;
using System;
using System.Collections.Generic;
using System.Globalization;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class ConfigPage : IDisposable {
        HudTextBox UIDefaultHeading { get; set; }
        HudCheckBox UIRespondToUnknownCommands { get; set; }
        HudButton UIManageBotSpellProfiles { get; set; }
        HudCheckBox UIFastCastSelfBuffs { get; set; }
        HudFixedLayout UIConfigTabLayout;
        HudTextBox UIDangerousMonsterLogoffDistance { get; set; }
        HudCheckBox UIEnableResetCommand { get; set; }

        MainView mainView;

        public ConfigPage(MainView mainView) {
            try {
                this.mainView = mainView;

                UIConfigTabLayout = (HudFixedLayout)mainView.view["UIConfigTabLayout"];

                UIManageBotSpellProfiles = (HudButton)mainView.view["UIManageBotSpellProfiles"];
                UIDefaultHeading = mainView.view != null ? (HudTextBox)mainView.view["UIDefaultHeading"] : new HudTextBox();
                UIRespondToUnknownCommands = mainView.view != null ? (HudCheckBox)mainView.view["UIRespondToUnknownCommands"] : new HudCheckBox();
                UIFastCastSelfBuffs = (HudCheckBox)mainView.view["UIFastCastSelfBuffs"];
                UIDangerousMonsterLogoffDistance = (HudTextBox)mainView.view["UIDangerousMonsterLogoffDistance"];
                UIEnableResetCommand = (HudCheckBox)mainView.view["UIEnableResetCommand"];

                UIDefaultHeading.Text = Config.Bot.DefaultHeading.Value.ToString(CultureInfo.InvariantCulture);
                UIRespondToUnknownCommands.Checked = Config.Bot.RespondToUnknownCommands.Value;
                UIFastCastSelfBuffs.Checked = Config.Bot.FastCastSelfBuffs.Value;
                UIDangerousMonsterLogoffDistance.Text = Config.Bot.DangerousMonsterLogoffDistance.Value.ToString(CultureInfo.InvariantCulture);
                UIEnableResetCommand.Checked = Config.Bot.EnableResetCommand.Value;

                Config.Bot.DefaultHeading.Changed += obj => { UIDefaultHeading.Text = obj.Value.ToString(CultureInfo.InvariantCulture); };
                Config.Bot.RespondToUnknownCommands.Changed += obj => { UIRespondToUnknownCommands.Checked = obj.Value; };
                Config.Bot.FastCastSelfBuffs.Changed += obj => { UIFastCastSelfBuffs.Checked = obj.Value; };
                Config.Bot.DangerousMonsterLogoffDistance.Changed += obj => { UIDangerousMonsterLogoffDistance.Text = obj.Value.ToString(CultureInfo.InvariantCulture); };
                Config.Bot.EnableResetCommand.Changed += obj => { UIEnableResetCommand.Checked = obj.Value; };

                UIDefaultHeading.LostFocus += (s, e) => {
                    try {
                        if (!int.TryParse(UIDefaultHeading.Text, out int value))
                            value = Config.Bot.DefaultHeading.Value;
                        Config.Bot.DefaultHeading.Value = value;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIRespondToUnknownCommands.Change += (s, e) => {
                    try {
                        Config.Bot.RespondToUnknownCommands.Value = ((HudCheckBox)s).Checked;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIFastCastSelfBuffs.Change += (s, e) => {
                    try {
                        Config.Bot.FastCastSelfBuffs.Value = ((HudCheckBox)s).Checked;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIEnableResetCommand.Change += (s, e) => {
                    try {
                        Config.Bot.EnableResetCommand.Value = ((HudCheckBox)s).Checked;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIManageBotSpellProfiles.Hit += UIManageBotSpellProfiles_Hit;

                UIDangerousMonsterLogoffDistance.LostFocus += (s, e) => {
                    try {
                        if (!int.TryParse(UIDangerousMonsterLogoffDistance.Text, out int value))
                            value = Config.Bot.DangerousMonsterLogoffDistance.Value;
                        Config.Bot.DangerousMonsterLogoffDistance.Value = value;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                DrawTrackedComponents();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIManageBotSpellProfiles_Hit(object sender, EventArgs e) {
            try {
                Globals.ProfileManagerView.EditBotProfiles();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void DrawTrackedComponents() {
            int row = 0;
            int col = 0;

            foreach (var trackedComponent in ComponentManager.trackedComponents) {
                DrawTrackedComponent(trackedComponent, row, col);

                row++;

                if (row == 3) {
                    row = 0;
                    col++;
                }
            }
        }

        private void DrawTrackedComponent(Lib.Component trackedComponent, int row, int col) {
            int rowHeight = 20;
            int rowWidth = (mainView.view.Width / 3) + 12;

            HudPictureBox icon = new HudPictureBox();
            icon.Image = new VirindiViewService.ACImage(trackedComponent.Icon);
            icon.Visible = true;
            UIConfigTabLayout.AddControl(icon, new System.Drawing.Rectangle(col * rowWidth, 120 + (row * rowHeight), rowHeight, rowHeight));

            HudTextBox text = new HudTextBox();
            text.Text = trackedComponent.LowWarningAmount().ToString();
            UIConfigTabLayout.AddControl(text, new System.Drawing.Rectangle((col * rowWidth) + rowHeight, 120 + (row * rowHeight) + 1, 35, rowHeight - 2));

            text.LostFocus += (s, e) => {
                int value = Config.Bot.GetComponentLowWarningLevel(trackedComponent.ConfigKey);

                if (!Int32.TryParse(text.Text, out value)) {
                    Util.WriteToChat("Invalid number for " + trackedComponent.Name);
                    text.Text = Config.Bot.GetComponentLowWarningLevel(trackedComponent.ConfigKey).ToString();
                    return;
                }

                Config.Bot.SetComponentLowWarningLevel(trackedComponent.ConfigKey, value);
            };

            HudStaticText label = new HudStaticText();
            label.Text = trackedComponent.Name.Replace(" Taper", "").Replace(" Scarab", "");
            UIConfigTabLayout.AddControl(label, new System.Drawing.Rectangle((col * rowWidth) + rowHeight + 38, 120 + (row * rowHeight) + 2, 70, rowHeight));
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