using System;
using System.IO;
using System.Collections.ObjectModel;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DoThingsBot.Lib;
using DoThingsBot.Stats;

namespace DoThingsBot.Chat {
    public class ChatCommandEventArgs : EventArgs {
        private string command;
        private string playerName;
        private string arguments;
        private string fullText;

        public ChatCommandEventArgs(string sender, string text, string full) {
            command = text;
            playerName = sender;
            arguments = null;
            fullText = full;
        }

        public ChatCommandEventArgs(string sender, string text, string full, string args) {
            command = text;
            playerName = sender;
            arguments = args;
            fullText = full;
        }

        public string Command {
            get { return command; }
            set { command = value; }
        }

        public string PlayerName {
            get { return playerName; }
            set { playerName = value; }
        }

        public string Arguments {
            get { return arguments; }
            set { arguments = value; }
        }

        public string Text {
            get { return fullText; }
            set { fullText = value; }
        }
    }

    public class ChatManager : IDisposable {
        public const int ChatCommandDelay = 500;

        private static Queue<string> messageQueue;
        private static Queue<string> commandQueue;
        static DateTime lastChatCommandSentAt = DateTime.MinValue;
        static DateTime lastAnnouncementTime = DateTime.UtcNow;
        static List<DateTime> chatMessageTimes = new List<DateTime>();

        public static event EventHandler<ChatCommandEventArgs> RaiseChatCommandEvent;

        static Random rnd = new Random();

        public static DateTime firstThought = DateTime.UtcNow;

        private static readonly Regex PublicChatMessageRegex = new Regex("^([\\/@](cg|ct|s|a|e) |:|^(?![:\\/@\\*])).*$");
        private static readonly Regex PrivateChatMessageRegex = new Regex("^([\\/@](tell|reply|rt|r|t) ).*$");

        private static string lastMessage = "";
        private static bool lastMessageWasCompsWarning = false;

        public ChatManager() {
            try {
                messageQueue = new Queue<string>();
                commandQueue = new Queue<string>();
                CoreManager.Current.ChatBoxMessage += new EventHandler<ChatTextInterceptEventArgs>(Current_ChatBoxMessage);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private bool disposed;

        public void Dispose() {
            Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            // If you need thread safety, use a lock around these 
            // operations, as well as in your methods that use the resource.
            if (!disposed) {
                if (disposing) {
                    CoreManager.Current.ChatBoxMessage -= new EventHandler<ChatTextInterceptEventArgs>(Current_ChatBoxMessage);
                }

                // Indicate that the instance has been disposed.
                disposed = true;
            }
        }

        // Wrap event invocations inside a protected virtual method
        // to allow derived classes to override the event invocation behavior
        protected static void OnRaiseChatCommandEvent(ChatCommandEventArgs e) {
            try {
                RaiseChatCommandEvent?.Invoke(null, e);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e) {
            try {
                if (Config.Bot.Enabled.Value == false) return;
                
                if (Util.IsChat(e.Text, Util.ChatFlags.PlayerTellsYou)) {
                    Util.WriteToDebugLog(Util.CleanMessage(e.Text));

                    string playerName = Util.GetSourceOfChat(e.Text);
                    string command = Util.GetMessageFromChat(e.Text);
                    System.Collections.Generic.List<string> args = new List<string>(command.Split(' '));
                    command = args.GetRange(0, 1)[0].ToLower();

                    if (playerName == null || command == null) return;

                    if (args.Count > 1) {
                        RaiseChatCommandEvent(this, new ChatCommandEventArgs(playerName, command, e.Text, String.Join(" ", args.GetRange(1, args.Count - 1).ToArray())));
                    }
                    else {
                        RaiseChatCommandEvent(this, new ChatCommandEventArgs(playerName, command, e.Text));
                    }
                }
                else if (Util.IsChat(e.Text, Util.ChatFlags.PlayerSaysLocal)) {
                    var message = Util.GetMessageFromChat(e.Text);
                    var playerName = Util.GetSourceOfChat(e.Text);

                    if (message == "whereto") {
                        Globals.DoThingsBot.RespondToWhereTo(playerName);
                    }
                    else if (message.StartsWith("whereto ")) {
                        Globals.DoThingsBot.RespondToWhereTo(playerName, message.Replace("whereto ", ""));
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public static void ResetAnnouncementTimer() {
            lastAnnouncementTime = DateTime.UtcNow;
        }

        public static void Tell(string playerName, string message) {
            messageQueue.Enqueue(String.Format("/tell {0}, {1}", playerName, message));
        }

        public static void Say(string message) {
            messageQueue.Enqueue(String.Format("/say {0}", message));
        }

        public static void AddSpamToChatBox(string message) {
            if (!message.StartsWith("*") && (PublicChatMessageRegex.IsMatch(message.ToLower()) || !message.StartsWith("/"))) {
                if (message.StartsWith("/s ") || (!message.StartsWith("/") && !message.StartsWith(":"))) {
                    message = string.Format("/e says, \"{0}\" -b-", message.Replace("/s ", ""));
                }
                else {
                    message = string.Format("{0} -b-", message);
                }
            }

            AddToChatBox(message);
        }

        public static void AddToChatBox(string command) {
            if (IsChatCommand(command)) {
                messageQueue.Enqueue(command);
            }
            else {
                commandQueue.Enqueue(command);
            }
        }

        public static bool IsChatCommand(string command) {
            return (PublicChatMessageRegex.IsMatch(command.ToLower()) || PrivateChatMessageRegex.IsMatch(command.ToLower()));
        }

        private static void CleanChatMessageTimes() {
            var times = new List<DateTime>();

            foreach (var time in chatMessageTimes) {
                if (DateTime.UtcNow - time <= TimeSpan.FromSeconds(9)) {
                    times.Add(time);
                }
            }

            chatMessageTimes.Clear();
            chatMessageTimes.AddRange(times);
        }

        public static bool CanSendChatMessage() {
            CleanChatMessageTimes();

            return !(chatMessageTimes.Count >= 8);
        }

        public static void Think() {
            if (DateTime.UtcNow - firstThought < TimeSpan.FromSeconds(3)) return;

            if (DateTime.UtcNow - lastChatCommandSentAt > TimeSpan.FromSeconds(Config.Bot.DontResendDuplicateMessagesWindow.Value)) {
                lastMessage = "";
            }

            // announcements
            if (DateTime.UtcNow - lastAnnouncementTime > TimeSpan.FromMinutes(Config.Announcements.SpamInterval.Value) && Config.Bot.Enabled.Value == true) {
                if (Config.Announcements.Enabled.Value == true) {
                    lastAnnouncementTime = DateTime.UtcNow;

                    var announcements = new List<string>();
                    announcements.AddRange(Config.Announcements.Messages.Value);

                    if (Config.Announcements.EnableStatSpam.Value) {
                        var statAnnouncement = StatAnnouncements.GetRandom();

                        if (!string.IsNullOrEmpty(statAnnouncement)) {
                            announcements.Add("/s " + statAnnouncement);
                        }
                    }

                    if (lastMessageWasCompsWarning == false && ComponentManager.IsLowOnComps() && Config.Bot.AnnounceLowComponents.Value) {
                        AddSpamToChatBox("/s " + ComponentManager.LowComponentAnnouncement());
                        lastMessageWasCompsWarning = true;
                    }
                    else if (announcements.Count > 0) {
                        int r = rnd.Next(announcements.Count);
                        string message = announcements[r];
                        AddSpamToChatBox(message);
                        lastMessageWasCompsWarning = false;
                    }
                }
            }

            // commands
            if (commandQueue.Count > 0) {
                var command = commandQueue.Dequeue();
                DecalProxy.DispatchChatToBoxWithPluginIntercept(command);
                Util.WriteToDebugLog(command);
                return;
            }

            // chat spam
            if (DateTime.UtcNow - lastChatCommandSentAt > TimeSpan.FromMilliseconds(ChatCommandDelay)) {
                if (messageQueue.Count > 0 && CanSendChatMessage()) {
                    var command = messageQueue.Dequeue();

                    if (lastMessage != command) {
                        lastMessage = command;
                        lastChatCommandSentAt = DateTime.UtcNow;

                        chatMessageTimes.Add(DateTime.UtcNow);
                        var parts = command.Split('\n');
                        var prefix = "";
                        foreach (var part in parts) {
                            if (part.Length <= 0) continue;
                            var message = part;

                            if (PrivateChatMessageRegex.IsMatch(part.ToLower())) {
                                prefix = part.Split(',')[0];
                            }
                            else if (prefix.Length > 0) {
                                message = (prefix + ", " + part);
                            }
                            
                            DecalProxy.DispatchChatToBoxWithPluginIntercept(message);
                            Util.WriteToDebugLog(message);
                        }
                    }
                    else {
                        Util.WriteToDebugLog("Skipping command because it's a dupe: " + command);
                    }
                }
            }
        }
    }
}
