using Decal.Adapter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class NavPage : IDisposable {
        HudCheckBox UIEnableNav { get; set; }
        HudButton UINavSetStickyPoint { get; set; }
        HudTextBox UINavMaxDistance { get; set; }
        HudStaticText UINavStickyPoint { get; set; }

        MainView mainView;

        public NavPage(MainView mainView) {
            try {
                this.mainView = mainView;

                UIEnableNav = (HudCheckBox)mainView.view["UIEnableNav"];
                UINavSetStickyPoint = (HudButton)mainView.view["UINavSetStickyPoint"];
                UINavMaxDistance = (HudTextBox)mainView.view["UINavMaxDistance"];
                UINavStickyPoint = (HudStaticText)mainView.view["UINavStickyPoint"];

                UIEnableNav.Checked = Config.Bot.EnableStickySpot.Value;
                UINavStickyPoint.Text = GetStickySpotText();
                UINavMaxDistance.Text = Config.Bot.StickySpotMaxDistance.Value.ToString(CultureInfo.InvariantCulture);

                Config.Bot.EnableStickySpot.Changed += obj => { UIEnableNav.Checked = obj.Value; };
                Config.Bot.StickySpotMaxDistance.Changed += obj => { UINavMaxDistance.Text = obj.Value.ToString(CultureInfo.InvariantCulture); };
                Config.Bot.StickySpotNS.Changed += obj => { UINavStickyPoint.Text = GetStickySpotText(); };
                Config.Bot.StickySpotEW.Changed += obj => { UINavStickyPoint.Text = GetStickySpotText(); };

                UINavMaxDistance.LostFocus += (s, e) => {
                    try {
                        if (double.TryParse(UINavMaxDistance.Text, out double value))
                            Config.Bot.StickySpotMaxDistance.Value = value;
                        else
                            UINavMaxDistance.Text = Config.Bot.StickySpotMaxDistance.Value.ToString(CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIEnableNav.Change += (s, e) => {
                    try {
                        Config.Bot.EnableStickySpot.Value = ((HudCheckBox)s).Checked;
                        if (Config.Bot.EnableStickySpot.Value && Config.Bot.StickySpotNS.Value == 0 && Config.Bot.StickySpotEW.Value == 0)
                            UINavSetStickyPoint_Hit(null, null);
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UINavSetStickyPoint.Hit += UINavSetStickyPoint_Hit;

                if (Config.Bot.EnableStickySpot.Value && Config.Bot.StickySpotNS.Value == 0 && Config.Bot.StickySpotEW.Value == 0)
                    UINavSetStickyPoint_Hit(null, null);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UINavSetStickyPoint_Hit(object sender, EventArgs e) {
            try {
                var loc = CoreManager.Current.WorldFilter[CoreManager.Current.CharacterFilter.Id].Coordinates();
                Config.Bot.StickySpotNS.Value = loc.NorthSouth;
                Config.Bot.StickySpotEW.Value = loc.EastWest;
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private string GetStickySpotText() {
            var ns = Math.Round(Math.Abs(Config.Bot.StickySpotNS.Value), 2);
            var ew = Math.Round(Math.Abs(Config.Bot.StickySpotEW.Value), 2);
            var nst = Config.Bot.StickySpotNS.Value > 0 ? "N" : "S";
            var ewt = Config.Bot.StickySpotEW.Value > 0 ? "E" : "W";
            return $"{ns}{nst}, {ew}{ewt}";
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
