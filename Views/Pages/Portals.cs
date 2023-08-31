using System;


using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class PortalsPage : IDisposable {
        HudTextBox UIPrimaryPortalLocation { get; set; }
        HudTextBox UIPrimaryPortalHeading { get; set; }
        HudTextBox UIPrimaryPortalExtraCommand { get; set; }
        HudTextBox UISecondaryPortalLocation { get; set; }
        HudTextBox UISecondaryPortalHeading { get; set; }
        HudTextBox UISecondaryPortalExtraCommand { get; set; }
        HudTextBox UIPortalGemAddCommandText { get; set; }
        HudTextBox UIPortalGemAddHeadingText { get; set; }
        HudButton UIPortalGemAddSelected { get; set; }
        HudList UIPortalGemCommands { get; set; }
        HudTextBox UIPortalGemLowCount { get; set; }

        public PortalsPage(MainView mainView) {
            try {
                UIPrimaryPortalLocation = mainView.view != null ? (HudTextBox)mainView.view["UIPrimaryPortalLocation"] : new HudTextBox();
                UIPrimaryPortalHeading = mainView.view != null ? (HudTextBox)mainView.view["UIPrimaryPortalHeading"] : new HudTextBox();
                UIPrimaryPortalExtraCommand = (HudTextBox)mainView.view["UIPrimaryPortalExtraCommand"];

                UISecondaryPortalLocation = mainView.view != null ? (HudTextBox)mainView.view["UISecondaryPortalLocation"] : new HudTextBox();
                UISecondaryPortalHeading = mainView.view != null ? (HudTextBox)mainView.view["UISecondaryPortalHeading"] : new HudTextBox();
                UISecondaryPortalExtraCommand = (HudTextBox)mainView.view["UISecondaryPortalExtraCommand"];

                UIPortalGemAddCommandText = (HudTextBox)mainView.view["UIPortalGemAddCommandText"];
                UIPortalGemAddHeadingText = (HudTextBox)mainView.view["UIPortalGemAddHeadingText"];
                UIPortalGemAddSelected = (HudButton)mainView.view["UIPortalGemAddSelected"];
                UIPortalGemCommands = (HudList)mainView.view["UIPortalGemCommands"];

                UIPortalGemLowCount = (HudTextBox)mainView.view["UIPortalGemLowCount"];

                UIPrimaryPortalLocation.Text = Config.Portals.PrimaryPortalTieLocation.Value;
                UIPrimaryPortalHeading.Text = Config.Portals.PrimaryPortalHeading.Value.ToString();
                UIPrimaryPortalExtraCommand.Text = Config.Portals.PrimaryPortalExtraCommand.Value.ToString();

                UISecondaryPortalLocation.Text = Config.Portals.SecondaryPortalTieLocation.Value;
                UISecondaryPortalHeading.Text = Config.Portals.SecondaryPortalHeading.Value.ToString();
                UISecondaryPortalExtraCommand.Text = Config.Portals.SecondaryPortalExtraCommand.Value.ToString();

                UIPortalGemLowCount.Text = Config.Portals.PortalGemLowCount.Value.ToString();

                Config.Portals.PrimaryPortalTieLocation.Changed += obj => { UIPrimaryPortalLocation.Text = obj.Value; };
                Config.Portals.PrimaryPortalHeading.Changed += obj => { UIPrimaryPortalHeading.Text = obj.Value.ToString(); };
                Config.Portals.PrimaryPortalExtraCommand.Changed += obj => { UIPrimaryPortalExtraCommand.Text = obj.Value; };

                Config.Portals.SecondaryPortalTieLocation.Changed += obj => { UISecondaryPortalLocation.Text = obj.Value; };
                Config.Portals.SecondaryPortalHeading.Changed += obj => { UISecondaryPortalHeading.Text = obj.Value.ToString(); };

                Config.Portals.PortalGemLowCount.Changed += obj => { UIPortalGemLowCount.Text = obj.Value.ToString(); };

                UIPrimaryPortalLocation.LostFocus += (s, e) => {
                    try {
                        Config.Portals.PrimaryPortalTieLocation.Value = UIPrimaryPortalLocation.Text;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIPrimaryPortalHeading.LostFocus += (s, e) => {
                    try {
                        if (!int.TryParse(UIPrimaryPortalHeading.Text, out int value))
                            value = Config.Portals.PrimaryPortalHeading.Value;
                        Config.Portals.PrimaryPortalHeading.Value = value;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIPrimaryPortalExtraCommand.LostFocus += (s, e) => {
                    try {
                        Config.Portals.PrimaryPortalExtraCommand.Value = UIPrimaryPortalExtraCommand.Text;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UISecondaryPortalLocation.LostFocus += (s, e) => {
                    try {
                        Config.Portals.SecondaryPortalTieLocation.Value = UISecondaryPortalLocation.Text;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UISecondaryPortalHeading.LostFocus += (s, e) => {
                    try {
                        if (!int.TryParse(UISecondaryPortalHeading.Text, out int value))
                            value = Config.Portals.SecondaryPortalHeading.Value;
                        Config.Portals.SecondaryPortalHeading.Value = value;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UISecondaryPortalExtraCommand.LostFocus += (s, e) => {
                    try {
                        Config.Portals.SecondaryPortalExtraCommand.Value = UISecondaryPortalExtraCommand.Text;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIPortalGemAddSelected.Hit += UIPortalGemAddSelected_Hit;

                UIPortalGemCommands.Click += UIPortalGemCommands_Click;

                UIPortalGemLowCount.LostFocus += (s, e) => {
                    try {
                        if (!int.TryParse(UIPortalGemLowCount.Text, out int value))
                            value = Config.Portals.PortalGemLowCount.Value;
                        Config.Portals.PortalGemLowCount.Value = value;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshPortalGemsList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIPortalGemCommands_Click(object sender, int row, int col) {
            if (col != 4) return;
            if (row > Config.Portals.PortalGems.Value.Count) return;

            var newList = Config.Portals.PortalGems.Value;

            UIPortalGemCommands.RemoveRow(row);
            newList.RemoveAt(row);

            Config.Portals.PortalGems.Value = newList;
        }

        private void UIPortalGemAddSelected_Hit(object sender, EventArgs e) {
            try {
                var command = UIPortalGemAddCommandText.Text.Trim().Replace("|", "");
                int heading = 0;

                if (!Int32.TryParse(UIPortalGemAddHeadingText.Text.Trim(), out heading) || heading < 0 || heading > 360) {
                    Util.WriteToChat("Invalid heading, must be between 0 and 360");
                    return;
                }

                if (string.IsNullOrEmpty(command)) {
                    Util.WriteToChat("You must enter something in the command textbox. This is what the user needs to tell you in order for you to use the gem you want to add.");
                    return;
                }

                if (command.Contains(" ")) {
                    Util.WriteToChat("Commands must not contain spaces");
                    return;
                }

                if (!Globals.Core.Actions.IsValidObject(Globals.Core.Actions.CurrentSelection)) {
                    Util.WriteToChat("No item is currently selected.");
                    return;
                }

                var wo = Globals.Core.WorldFilter[Globals.Core.Actions.CurrentSelection];

                if (wo == null) {
                    Util.WriteToChat("Something went wrong, couldn't find selected item.");
                    return;
                }

                var newList = Config.Portals.PortalGems.Value;
                newList.Add(string.Format("{0}|{1}|{2}|{3}", command, wo.Name, heading, wo.Icon));
                Config.Portals.PortalGems.Value = newList;

                UIPortalGemAddCommandText.Text = "";
                UIPortalGemAddHeadingText.Text = "0";

                RefreshPortalGemsList();
                UIPortalGemCommands.ScrollPosition = UIPortalGemCommands.MaxScroll;
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshPortalGemsList() {
            try {
                UIPortalGemCommands.ClearRows();

                var commands = Config.Portals.PortalGemCommands();

                foreach (var cmd in commands) {
                    HudList.HudListRowAccessor newRow = UIPortalGemCommands.AddRow();

                    ((HudStaticText)newRow[0]).Text = cmd.Key;
                    ((HudPictureBox)newRow[1]).Image = cmd.Value.Icon + 0x6000000;
                    ((HudStaticText)newRow[2]).Text = cmd.Value.Name;
                    ((HudStaticText)newRow[3]).Text = cmd.Value.Heading.ToString();
                    ((HudPictureBox)newRow[4]).Image = 4600 + 0x6000000;
                }
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