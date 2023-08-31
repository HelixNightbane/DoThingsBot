using System;
using System.Globalization;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class AnnouncementsPage : IDisposable {
        HudCheckBox UIAnnouncementsEnabled { get; set; }
        HudCheckBox UIAnnouncementsEnableStatSpam { get; set; }
        HudTextBox UIAnnouncementsAnnounceInterval { get; set; }
        HudTextBox UIAnnouncementsNewMessage { get; set; }
        HudButton UIAnnouncementsAddNewMessage { get; set; }
        HudList UIAnnouncementsList { get; set; }
        HudTextBox UIStartupCommand { get; set; }

        public AnnouncementsPage(MainView mainView) {
            try {
                UIAnnouncementsEnabled = mainView.view != null ? (HudCheckBox)mainView.view["UIAnnouncementsEnabled"] : new HudCheckBox();
                UIAnnouncementsEnableStatSpam = mainView.view != null ? (HudCheckBox)mainView.view["UIAnnouncementsEnableStatSpam"] : new HudCheckBox();
                UIAnnouncementsAnnounceInterval = mainView.view != null ? (HudTextBox)mainView.view["UIAnnouncementsAnnounceInterval"] : new HudTextBox();
                UIAnnouncementsNewMessage = mainView.view != null ? (HudTextBox)mainView.view["UIAnnouncementsNewMessage"] : new HudTextBox();
                UIAnnouncementsAddNewMessage = mainView.view != null ? (HudButton)mainView.view["UIAnnouncementsAddNewMessage"] : new HudButton();
                UIAnnouncementsList = mainView.view != null ? (HudList)mainView.view["UIAnnouncementsList"] : new HudList();
                UIStartupCommand = mainView.view != null ? (HudTextBox)mainView.view["UIStartupCommand"] : new HudTextBox();

                UIAnnouncementsEnabled.Checked = Config.Announcements.Enabled.Value;
                UIAnnouncementsEnableStatSpam.Checked = Config.Announcements.EnableStatSpam.Value;
                UIAnnouncementsAnnounceInterval.Text = Config.Announcements.SpamInterval.Value.ToString(CultureInfo.InvariantCulture);
                UIAnnouncementsNewMessage.Text = "/s ";
                UIStartupCommand.Text = Config.Announcements.StartupMessage.Value;

                Config.Announcements.Enabled.Changed += obj => { UIAnnouncementsEnabled.Checked = obj.Value; };
                Config.Announcements.EnableStatSpam.Changed += obj => { UIAnnouncementsEnableStatSpam.Checked = obj.Value; };
                Config.Announcements.StartupMessage.Changed += obj => { UIStartupCommand.Text = obj.Value; };
                Config.Announcements.SpamInterval.Changed += obj => { UIAnnouncementsAnnounceInterval.Text = obj.Value.ToString(CultureInfo.InvariantCulture); };
                Config.Announcements.StartupMessage.Changed += obj => { UIStartupCommand.Text = obj.Value; };
                Config.Announcements.Messages.Changed += obj => { RefreshAnnouncementsMessages(); };

                UIAnnouncementsEnabled.Change += (s, e) => {
                    try {
                        Config.Announcements.Enabled.Value = ((HudCheckBox)s).Checked;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIAnnouncementsEnableStatSpam.Change += (s, e) => {
                    try {
                        Config.Announcements.EnableStatSpam.Value = ((HudCheckBox)s).Checked;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIAnnouncementsList.Click += new HudList.delClickedControl(UIAnnouncementsList_Click);

                UIAnnouncementsAnnounceInterval.LostFocus += (s, e) => {
                    try {
                        if (!int.TryParse(UIAnnouncementsAnnounceInterval.Text, out int value))
                            value = Config.Announcements.SpamInterval.Value;
                        Config.Announcements.SpamInterval.Value = value;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIAnnouncementsAddNewMessage.Hit += (s, e) => {
                    try {
                        if (UIAnnouncementsNewMessage.Text.Length > 0) {
                            var newList = Config.Announcements.Messages.Value;
                            newList.Add(UIAnnouncementsNewMessage.Text);
                            Config.Announcements.Messages.Value = newList;
                        }
                        else {
                            Util.WriteToChat("Announcement message cannot be blank");
                        }

                        UIAnnouncementsNewMessage.Text = "/s ";
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIStartupCommand.LostFocus += (s, e) => {
                    try {
                        Config.Announcements.StartupMessage.Value = UIStartupCommand.Text;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshAnnouncementsMessages();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshAnnouncementsMessages() {
            try {
                UIAnnouncementsList.ClearRows();

                var announcements = Config.Announcements.Messages.Value;

                for (int announcementIndex = 0; announcementIndex < announcements.Count; announcementIndex++) {
                    HudList.HudListRowAccessor newRow = UIAnnouncementsList.AddRow();
                    ((HudStaticText)newRow[0]).Text = announcements[announcementIndex];
                    ((HudStaticText)newRow[1]).Text = announcementIndex.ToString();
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIAnnouncementsList_Click(object sender, int row, int col) {
            try {
                if (Config.Announcements.Messages.Value.Count > row) {
                    var newList = Config.Announcements.Messages.Value;
                    newList.RemoveAt(row);
                    Config.Announcements.Messages.Value = newList;
                }
                else {
                    Util.WriteToDebugLog("Cant remove announcement at index: " + row);
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
                        UIAnnouncementsList.Click -= new HudList.delClickedControl(UIAnnouncementsList_Click);
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}