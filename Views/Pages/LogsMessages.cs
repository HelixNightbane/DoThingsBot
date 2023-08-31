using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class LogsMessagesPage : IDisposable {
        public VirindiViewService.ViewProperties properties;
        public VirindiViewService.ControlGroup controls;
        public VirindiViewService.HudView confirmView;

        HudList UILogsMessagesList { get; set; }
        HudButton UILogsMessagesOpenLogFile { get; set; }
        HudButton UILogsMessagesClearLogFile { get; set; }

        HudStaticText UIConfirmHeading { get; set; }
        HudButton UICancel { get; set; }
        HudButton UIConfirm { get; set; }

        public LogsMessagesPage(MainView mainView) {
            try {
                UILogsMessagesList = mainView.view != null ? (HudList)mainView.view["UILogsMessagesList"] : new HudList();
                UILogsMessagesOpenLogFile = mainView.view != null ? (HudButton)mainView.view["UILogsMessagesOpenLogFile"] : new HudButton();
                UILogsMessagesClearLogFile = mainView.view != null ? (HudButton)mainView.view["UILogsMessagesClearLogFile"] : new HudButton();

                ChatManager.RaiseChatCommandEvent += new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);

                UILogsMessagesOpenLogFile.Hit += (s, e) => {
                    try {
                        System.Diagnostics.Process.Start(Util.GetCharacterDataDirectory() + @"messages.txt");
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UILogsMessagesClearLogFile.Hit += (s, e) => {
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

                            UIConfirmHeading.Text = "Are you sure you want to clear the messages log file?  This action cannot be undone.";
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

                                    System.IO.File.WriteAllText(Util.GetCharacterDataDirectory() + @"messages.txt", string.Empty);
                                    RefreshMessagesList();

                                    Util.WriteToChat("Messages log file has been cleared.");
                                }
                                catch (Exception ex) { Util.LogException(ex); }
                            };
                        }
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshMessagesList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        void ChatManager_ChatCommand(object sender, ChatCommandEventArgs e) {
            try {
                if (!Config.Bot.Enabled.Value) return;

                if (e.Command == "message") {
                    if (e.Arguments == null || e.Arguments.Trim().Length == 0) {
                        ChatManager.Tell(e.PlayerName, "You should specify a message after that command.  eg: /t " + CoreManager.Current.CharacterFilter.Name + ", message hello!");
                        return;
                    }

                    Util.WriteMessageToLog(e.PlayerName, e.Arguments);

                    ChatManager.Tell(e.PlayerName, "I got your message, thanks!");

                    RefreshMessagesList();
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshMessagesList() {
            try {
                string logFile = Util.GetCharacterDataDirectory() + @"messages.txt";
                if (!File.Exists(logFile)) {
                    File.Create(logFile).Close();
                }

                UILogsMessagesList.ClearRows();
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

                        HudList.HudListRowAccessor newRow = UILogsMessagesList.AddRow();
                        ((HudStaticText)newRow[0]).Text = parts[0];
                        ((HudStaticText)newRow[1]).Text = parts[1];
                        ((HudStaticText)newRow[2]).Text = parts[2];
                    }
                }
                else {
                    HudList.HudListRowAccessor newRow = UILogsMessagesList.AddRow();
                    ((HudStaticText)newRow[0]).Text = "";
                    ((HudStaticText)newRow[1]).Text = "";
                    ((HudStaticText)newRow[2]).Text = "No messages received";
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
                        ChatManager.RaiseChatCommandEvent -= new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);
                        if (UICancel != null) UICancel.Dispose();
                        if (UIConfirm != null) UIConfirm.Dispose();
                        if (UIConfirmHeading != null) UIConfirmHeading.Dispose();
                        if (confirmView != null) confirmView.Dispose();
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}