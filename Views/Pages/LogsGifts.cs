using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class LogsGiftsPage : IDisposable {
        public VirindiViewService.ViewProperties properties;
        public VirindiViewService.ControlGroup controls;
        public VirindiViewService.HudView confirmView;

        HudList UILogsGiftsList { get; set; }
        HudButton UILogsGiftsOpenLogFile { get; set; }
        HudButton UILogsGiftsClearLogFile { get; set; }

        HudStaticText UIConfirmHeading { get; set; }
        HudButton UICancel { get; set; }
        HudButton UIConfirm { get; set; }

        public LogsGiftsPage(MainView mainView) {
            try {
                UILogsGiftsList = mainView.view != null ? (HudList)mainView.view["UILogsGiftsList"] : new HudList();
                UILogsGiftsOpenLogFile = mainView.view != null ? (HudButton)mainView.view["UILogsGiftsOpenLogFile"] : new HudButton();
                UILogsGiftsClearLogFile = mainView.view != null ? (HudButton)mainView.view["UILogsGiftsClearLogFile"] : new HudButton();

                CoreManager.Current.ChatBoxMessage += new EventHandler<ChatTextInterceptEventArgs>(Current_ChatBoxMessage);

                UILogsGiftsOpenLogFile.Hit += (s, e) => {
                    try {
                        System.Diagnostics.Process.Start(Util.GetCharacterDataDirectory() + @"gifts.txt");
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UILogsGiftsClearLogFile.Hit += (s, e) => {
                    try {
                        if (confirmView == null) {
                            // Create the view
                            VirindiViewService.XMLParsers.Decal3XMLParser parser = new VirindiViewService.XMLParsers.Decal3XMLParser();
                            parser.ParseFromResource("DoThingsBot.Views.confirmView.xml", out properties, out controls);

                            // Display the view
                            confirmView = new VirindiViewService.HudView(properties, controls);

                            UIConfirmHeading = confirmView != null ? (HudStaticText)confirmView["UIConfirmHeading"] : new HudStaticText();
                            UICancel = confirmView != null ? (HudButton)confirmView["UICancel"] : new HudButton();
                            UIConfirm = confirmView != null ? (HudButton)confirmView["UIConfirm"] : new HudButton();

                            UIConfirmHeading.Text = "Are you sure you want to clear the gifts log file?  This action cannot be undone.";
                            //UIConfirmHeading.TextAlignment = VirindiViewService.WriteTextFormats.Center;

                            int x = (mainView.view.Location.X + (mainView.view.Width / 2)) - (confirmView.Width / 2);
                            int y = (mainView.view.Location.Y + (mainView.view.Height / 2)) - (confirmView.Height / 2);

                            confirmView.Location = new System.Drawing.Point(x, y);

                            confirmView.ForcedZOrder = 9999;
                            confirmView.Visible = true;

                            UICancel.Hit += (a, b) => {
                                try {
                                    UICancel.Dispose();
                                    UIConfirm.Dispose();
                                    confirmView.Dispose();
                                    confirmView = null;
                                }
                                catch (Exception ex) { Util.LogException(ex); }
                            };

                            UIConfirm.Hit += (a, b) => {
                                try {
                                    UICancel.Dispose();
                                    UIConfirm.Dispose();
                                    confirmView.Dispose();
                                    confirmView = null;

                                    System.IO.File.WriteAllText(Util.GetCharacterDataDirectory() + @"gifts.txt", string.Empty);
                                    RefreshGiftsList();

                                    Util.WriteToChat("Gifts log file has been cleared.");
                                }
                                catch (Exception ex) { Util.LogException(ex); }
                            };
                        }
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshGiftsList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private static readonly Regex GivesYouRegex = new Regex(@"^(?<player>[^""]+) gives you (?<amount>(\d|,)*) ?(?<item>[^""]+)\.$");


        private void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e) {
            if (!Config.Bot.Enabled.Value) return;

            if (GivesYouRegex.IsMatch(e.Text)) {
                Match match = GivesYouRegex.Match(e.Text);

                if (match.Success) {
                    string player = match.Groups["player"].Value;
                    string item = match.Groups["item"].Value;
                    string amountString = match.Groups["amount"].Value.Replace(",", "");
                    int amount = 1;
                    if (!string.IsNullOrEmpty(amountString))
                        Int32.TryParse(amountString, out amount);

                    WorldObject wo = null;
                    using (var wos = CoreManager.Current.WorldFilter.GetInventory()) {
                        foreach (var iwo in wos) {
                            if ((amount == 1 && iwo.Name == item) || (amount > 1 && iwo.Values(StringValueKey.SecondaryName, "") == item)) {
                                wo = iwo;
                                break;
                            }
                        }
                    }

                    if (wo != null)
                        item = wo.Name;

                    Globals.Stats.AddPlayerDonation(player, item, amount);
                    if (amount > 1) {
                        ChatManager.Tell(player, String.Format("Thank you for the {0} x{1}!", item, amount));
                    }
                    else {
                        ChatManager.Tell(player, String.Format("Thank you for the {0}!", item));
                    }

                    Util.WriteGiftToLog(player, item);

                    RefreshGiftsList();
                }
            }
        }

        private void RefreshGiftsList() {
            try {
                string logFile = Util.GetCharacterDataDirectory() + @"gifts.txt";
                if (!File.Exists(logFile)) {
                    File.Create(logFile).Close();
                }

                UILogsGiftsList.ClearRows();
                StreamReader objReader = new StreamReader(logFile);
                string sLine = "";
                ArrayList arrText = new ArrayList();

                while (sLine != null) {
                    sLine = objReader.ReadLine();
                    if (sLine != null)
                        arrText.Add(sLine);
                }
                objReader.Close();

                arrText.Reverse();

                if (arrText.Count > 0) {
                    foreach (string sOutput in arrText) {
                        var parts = sOutput.Split('|');

                        if (parts.Length != 3) continue;

                        HudList.HudListRowAccessor newRow = UILogsGiftsList.AddRow();
                        ((HudStaticText)newRow[0]).Text = parts[0];
                        ((HudStaticText)newRow[1]).Text = parts[1];
                        ((HudStaticText)newRow[2]).Text = parts[2];
                    }
                }
                else {
                    HudList.HudListRowAccessor newRow = UILogsGiftsList.AddRow();
                    ((HudStaticText)newRow[0]).Text = "";
                    ((HudStaticText)newRow[1]).Text = "";
                    ((HudStaticText)newRow[2]).Text = "No gifts received";
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
                        CoreManager.Current.ChatBoxMessage -= new EventHandler<ChatTextInterceptEventArgs>(Current_ChatBoxMessage);
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}