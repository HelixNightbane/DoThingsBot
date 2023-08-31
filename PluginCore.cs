using System;
using System.IO;

using Decal.Adapter;
using MyClasses.MetaViewWrappers;
using DoThingsBot.Views;
using Decal.Filters;
using System.Runtime.InteropServices;
using Decal.Adapter.Messages;
using Decal.Adapter.Wrappers;
using DoThingsBot.Lib;

namespace DoThingsBot {
    //Attaches events from core
    [WireUpBaseEvents]

    // FriendlyName is the name that will show up in the plugins list of the decal agent (the one in windows, not in-game)
    [FriendlyName("DoThingsBot")]
	public class PluginCore : PluginBase {

        internal static string PluginName = "DoThingsBot";
        private DoThingsBot bot;

        // Views, depends on VirindiViewService.dll
        internal MainView mainView;

        internal static DirectoryInfo PluginPersonalFolder {
            get {
                
                DirectoryInfo pluginPersonalFolder = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\Decal Plugins\" + PluginName);

                try {
                    if (!pluginPersonalFolder.Exists)
                        pluginPersonalFolder.Create();
                }
                catch (Exception ex) { Util.LogException(ex); }

                return pluginPersonalFolder;
            }
        }

        /// <summary>
        /// This is called when the plugin is started up. This happens only once.
        /// </summary>
        protected override void Startup()
		{
			try
			{
				// This initializes our static Globals class with references to the key objects your plugin will use, Host and Core.
				// The OOP way would be to pass Host and Core to your objects, but this is easier.
				Globals.Init("DoThingsBot", Host, Core);

                CoreManager.Current.PluginInitComplete += new EventHandler<EventArgs>(Current_PluginInitComplete);
                CoreManager.Current.CommandLineText += new EventHandler<ChatParserInterceptEventArgs>(Current_CommandLineText);
            }
			catch (Exception ex) { Util.LogException(ex); }
		}

		/// <summary>
		/// This is called when the plugin is shut down. This happens only once.
		/// </summary>
		protected override void Shutdown()
		{
			try {
                CoreManager.Current.PluginInitComplete -= new EventHandler<EventArgs>(Current_PluginInitComplete);
                CoreManager.Current.CommandLineText -= new EventHandler<ChatParserInterceptEventArgs>(Current_CommandLineText);

                if (bot != null) bot.Dispose();
                if (mainView != null) mainView.Dispose();
            }
			catch (Exception ex) { Util.LogException(ex); }
        }

        void Current_PluginInitComplete(object sender, EventArgs e) {
            try {

            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        [BaseEvent("LoginComplete", "CharacterFilter")]
		private void CharacterFilter_LoginComplete(object sender, EventArgs e)
		{
            try {
                string configFilePath = Util.GetCharacterDataDirectory() + "config.xml";

                Util.CreateDataDirectories();

                Util.TruncateLogFiles();
                Util.PruneOldLogs();

                Util.WriteToDebugLog("LoginComplete");
                Util.WriteToChat(String.Format("Config file: {0}", configFilePath));

                Mag.Shared.Settings.SettingsFile.Init(configFilePath, PluginName);

                Config.Init();

                Mag.Shared.Settings.SettingsFile.SaveXmlDocument();
                Buffs.Buffs.LoadProfiles();
                
                mainView = new MainView();
                Globals.MainView = mainView;
                Globals.Stats = new Stats.Stats();
                Globals.StatsView = new StatsView();
                bot = new DoThingsBot();
                Globals.DoThingsBot = bot;

                bot.IsLoggedIn = true;

                if (Config.Bot.Enabled.Value == true) {
                    bot.Start();
                }

                Globals.ProfileManagerView = new ProfileManagerView();
                Globals.ProfileManagerView.ReloadProfiles();

                UpdateChecker.CheckForUpdate();
            }
            catch (Exception ex) { Util.LogException(ex); }
		}

        [BaseEvent("Logoff", "CharacterFilter")]
        private void CharacterFilter_Logoff(object sender, Decal.Adapter.Wrappers.LogoffEventArgs e) {
            try {
                if (bot.isRunning) {
                    bot.Stop();
                }

                bot.IsLoggedIn = false;

                if (bot != null) bot.Dispose();
                if (Globals.StatsView != null) Globals.StatsView.Dispose();
                if (mainView != null) mainView.Dispose();

                Util.WriteToDebugLog("Logoff");
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        void Current_CommandLineText(object sender, ChatParserInterceptEventArgs e) {
            try {
                if (e.Text == "/dtb test") {
                    e.Eat = true;
                }

                if (e.Text == "/dtb start") {
                    Config.Bot.Enabled.Value = true;
                    e.Eat = true;
                }

                if (e.Text == "/dtb stop") {
                    Config.Bot.Enabled.Value = false;
                    e.Eat = true;
                }

                if (e.Text == "/dtb forcebuff") {
                    e.Eat = true;
                    if (bot.isRunning) {
                        Util.WriteToChat("Adding forcebuff to queue");
                        bot.AddToQueue("forcebuff");
                    }
                    else {
                        Util.WriteToChat("Bot is not running, won't forcebuff.");
                    }
                }

                if (e.Text.StartsWith("/dtb buff")) {
                    if (!bot.isRunning) {
                        e.Eat = true;
                        Util.WriteToChat("Bot is not running, won't add buffs to queue.");
                        return;
                    }

                    if (e.Text == "/dtb buff") {
                        var current = CoreManager.Current.Actions.CurrentSelection;

                        if (CoreManager.Current.Actions.IsValidObject(current)) {
                            var selected = CoreManager.Current.WorldFilter[current];
                            if (selected.ObjectClass == Decal.Adapter.Wrappers.ObjectClass.Player) {
                                Util.WriteToChat(string.Format("Adding {0} to the buff queue using treestats profile buffs", selected.Name));
                                Globals.DoThingsBot.queue.Add(new DoThingsBot.PlayerCommand(selected.Name, "buff"));
                                e.Eat = true;
                                return;
                            }
                        }

                        Util.WriteToChat("You must have a player selected to use this command.");
                        return;
                    }

                    if (e.Text.Contains(" with ")) {
                        var parts = e.Text.Replace("/dtb buff ", "").Replace(" with ", "|").Split('|');
                        if (parts.Length == 2) {
                            Util.WriteToChat(string.Format("Adding {0} to the buff queue with profiles: ", parts[0], parts[1]));
                            Globals.DoThingsBot.queue.Add(new DoThingsBot.PlayerCommand(parts[0], parts[1]));
                            e.Eat = true;
                        }
                    }
                    else {
                        var playerName = e.Text.Replace("/dtb buff ", "");

                        if (!Config.BuffBot.EnableTreeStatsBuffs.Value) {
                            e.Eat = true;
                            Util.WriteToChat("My TreeStats buffing profile functionality is currently disabled!");
                            return;
                        }

                        if (!string.IsNullOrEmpty(playerName)) {
                            Util.WriteToChat(string.Format("Adding {0} to the buff queue using treestats profile buffs", playerName));
                            Globals.DoThingsBot.queue.Add(new DoThingsBot.PlayerCommand(playerName, "buff"));
                            e.Eat = true;
                        }
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}
