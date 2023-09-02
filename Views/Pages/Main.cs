using System;


using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class MainPage : IDisposable {
        HudCheckBox UIBotEnabled { get; set; }
        HudCheckBox UIBotPortalsEnabled { get; set; }
        HudCheckBox UIBotBuffBotEnabled { get; set; }
        HudCheckBox UIBotCraftBotEnabled { get; set; }
        HudCheckBox UIBotTinkerBotEnabled { get; set; }
        HudCheckBox UIBotBrillBotEnabled { get; set; }
        HudStaticText UIGitlabLink { get; set; }

        public MainPage(MainView mainView) {
            try {
                UIBotEnabled = mainView.view != null ? (HudCheckBox)mainView.view["UIBotEnabled"] : new HudCheckBox();
                UIBotPortalsEnabled = mainView.view != null ? (HudCheckBox)mainView.view["UIBotPortalsEnabled"] : new HudCheckBox();

                UIBotPortalsEnabled.Checked = Config.Portals.Enabled.Value;
                Config.Portals.Enabled.Changed += obj => { UIBotPortalsEnabled.Checked = obj.Value; };
                UIBotPortalsEnabled.Change += (s, e) => {
                    try {
                        Config.Portals.Enabled.Value = ((HudCheckBox)s).Checked;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIBotCraftBotEnabled = mainView.view != null ? (HudCheckBox)mainView.view["UIBotCraftBotEnabled"] : new HudCheckBox();

                UIBotCraftBotEnabled.Checked = Config.CraftBot.Enabled.Value;
                Config.Portals.Enabled.Changed += obj => { UIBotCraftBotEnabled.Checked = obj.Value; };
                UIBotCraftBotEnabled.Change += (s, e) => {
                    try {
                        Config.CraftBot.Enabled.Value = ((HudCheckBox)s).Checked;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIGitlabLink = (HudStaticText)mainView.view["UIGitlabLink"];
                UIGitlabLink.Hit += UIGitlabLink_Hit;

                UIBotBuffBotEnabled = (HudCheckBox)mainView.view["UIBotBuffBotEnabled"];
                UIBotBuffBotEnabled.Checked = Config.BuffBot.Enabled.Value;
                Config.BuffBot.Enabled.Changed += obj => { UIBotBuffBotEnabled.Checked = obj.Value; };
                UIBotBuffBotEnabled.Change += (s, e) => { try { Config.BuffBot.Enabled.Value = ((HudCheckBox)s).Checked; } catch (Exception ex) { Util.LogException(ex); } };

                UIBotTinkerBotEnabled = (HudCheckBox)mainView.view["UIBotTinkerBotEnabled"];
                UIBotTinkerBotEnabled.Checked = Config.Tinkering.Enabled.Value;
                Config.Tinkering.Enabled.Changed += obj => { UIBotTinkerBotEnabled.Checked = obj.Value; };
                UIBotTinkerBotEnabled.Change += (s, e) => { try { Config.Tinkering.Enabled.Value = ((HudCheckBox)s).Checked; } catch (Exception ex) { Util.LogException(ex); } };

                UIBotBrillBotEnabled = (HudCheckBox)mainView.view["UIBotBrillBotEnabled"];
                UIBotBrillBotEnabled.Checked = Config.BrillBot.Enabled.Value;
                Config.BrillBot.Enabled.Changed += obj => { UIBotBrillBotEnabled.Checked = obj.Value; };
                UIBotBrillBotEnabled.Change += (s, e) => { try { Config.BrillBot.Enabled.Value = ((HudCheckBox)s).Checked; } catch (Exception ex) { Util.LogException(ex); } };

                UIBotEnabled.Checked = Config.Bot.Enabled.Value;
                Config.Bot.Enabled.Changed += obj => { UIBotEnabled.Checked = obj.Value; };
                UIBotEnabled.Change += (s, e) => { try { Config.Bot.Enabled.Value = ((HudCheckBox)s).Checked; } catch (Exception ex) { Util.LogException(ex); } };
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIGitlabLink_Hit(object sender, EventArgs e) {
            try {
                System.Diagnostics.Process.Start("https://github.com/HelixNightbane/DoThingsBot");
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