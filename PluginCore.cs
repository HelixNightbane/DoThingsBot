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
using DoThingsBot.Buffs;

namespace DoThingsBot {
    //Attaches events from core
    [WireUpBaseEvents]

    // FriendlyName is the name that will show up in the plugins list of the decal agent (the one in windows, not in-game)
    [FriendlyName("DoThingsBot")]
	public class PluginCore : PluginBase {

        internal static string PluginName = "DoThingsBot";
        private static string _assemblyDirectory = null;
        private DoThingsBot bot;
        private bool started;

        // Views, depends on VirindiViewService.dll
        internal MainView mainView;
        public static string AssemblyDirectory
        {
            get
            {
                if (_assemblyDirectory == null)
                {
                    try
                    {
                        _assemblyDirectory = System.IO.Path.GetDirectoryName(typeof(PluginCore).Assembly.Location);
                    }
                    catch
                    {
                        _assemblyDirectory = Environment.CurrentDirectory;
                    }
                }
                return _assemblyDirectory;
            }
            set
            {
                _assemblyDirectory = value;
            }
        }
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
            {;
                Globals.Init("DoThingsBot", Core);


                CoreManager.Current.PluginInitComplete += Current_PluginInitComplete;
                CoreManager.Current.CommandLineText += Current_CommandLineText;

                CoreManager.Current.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
                if (CoreManager.Current.CharacterFilter.LoginStatus == 3)
                {
                    CharacterFilter_LoginComplete(this,EventArgs.Empty);
                }
                
            }
            catch (Exception ex) { Log(ex); }
		}

		/// <summary>
		/// This is called when the plugin is shut down. This happens only once.
		/// </summary>
		protected override void Shutdown()
		{
			try {
                CoreManager.Current.PluginInitComplete -= new EventHandler<EventArgs>(Current_PluginInitComplete);
                CoreManager.Current.CommandLineText -= new EventHandler<ChatParserInterceptEventArgs>(Current_CommandLineText);

                CoreManager.Current.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;

                if (bot != null) bot.Dispose();
                if (Globals.StatsView != null) {
                    Globals.StatsView.Dispose(); 
                }
                if (mainView != null) mainView.Dispose();
            }
			catch (Exception ex) { Util.LogException(ex); }
        }

        void Current_PluginInitComplete(object sender, EventArgs e) {
            try {
                
            }
            catch (Exception ex) { Log(ex); }
        }
        
   

        //[BaseEvent("CharacterFilter")]
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
                if (Globals.StatsView != null)
                {
                    Globals.StatsView.Dispose();
                }
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
        #region logging
        /// <summary>
        /// Log an exception to log.txt in the same directory as the plugin.
        /// </summary>
        /// <param name="ex"></param>
        internal static void Log(Exception ex)
        {
            Log(ex.ToString());
        }

        /// <summary>
        /// Log a string to log.txt in the same directory as the plugin.
        /// </summary>
        /// <param name="message"></param>
        internal static void Log(string message)
        {
            try
            {
                File.AppendAllText(System.IO.Path.Combine(AssemblyDirectory, "log.txt"), $"{message}\n");

                CoreManager.Current.Actions.AddChatText(message, 1);
            }
            catch { }
        }
        #endregion // logging
    }
}
