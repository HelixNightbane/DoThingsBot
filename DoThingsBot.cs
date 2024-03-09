using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot;
using DoThingsBot.Chat;
using DoThingsBot.FSM;
using DoThingsBot.FSM.States;
using System.Reflection;
using System.Diagnostics;
using DoThingsBot.Lib;
using DoThingsBot.Lib.Recipes;
using System.Xml;
using System.Linq;
using System.Reflection.Emit;
using static DoThingsBot.Config;

namespace DoThingsBot {
    public class DoThingsBot {
        public bool isRunning = false;
        public DateTime botStartedAt = DateTime.MinValue;
        
        public Chat.ChatManager _chatManager;

        public Machine _machine;

        public struct PlayerCommand {
            public string PlayerName;
            public string Command;

            public PlayerCommand(string name, string cmd) {
                PlayerName = name;
                Command = cmd;
            }
        }

        public List<PlayerCommand> queue = new List<PlayerCommand>();

        public ItemBundle currentItemBundle;

        public bool hasTradeOpen = false;
        public int tradePartnerId = 0;

        private enum DoThingType {
            Tinker
        }

        public DoThingsBot() {
            _chatManager = new Chat.ChatManager();
            _machine = new Machine();

            CoreManager.Current.RenderFrame += new EventHandler<EventArgs>(Current_RenderFrame);
            CoreManager.Current.WorldFilter.EnterTrade += WorldFilter_EnterTrade;
            CoreManager.Current.WorldFilter.EndTrade += WorldFilter_EndTrade;
        }

        private void WorldFilter_EnterTrade(object sender, EnterTradeEventArgs e) {
            try {
                hasTradeOpen = true;
                tradePartnerId = e.TradeeId == Globals.Core.CharacterFilter.Id ? e.TraderId : e.TradeeId;
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void WorldFilter_EndTrade(object sender, EndTradeEventArgs e) {
            try {
                hasTradeOpen = false;
                tradePartnerId = 0;
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public void Start() {
            if (!CheckSettings()) {
                Config.Bot.Enabled.Value = false;
                return;
            }

            LoadQueue();

            Globals.Stats = new Stats.Stats();

            Recipes.Init();

            Util.WriteToChat($"DoThingsBot Started");

            CoreManager.Current.Actions.FaceHeading(Config.Bot.DefaultHeading.Value, true);

            isRunning = true;
            botStartedAt = DateTime.UtcNow;
            _machine.Start();

            _machine.ChangeState(new BotIdleState());

            if (Config.Announcements.Enabled.Value == true) {
                if (!string.IsNullOrEmpty(Config.Announcements.StartupMessage.Value)) {
                    ChatManager.AddSpamToChatBox(Config.Announcements.StartupMessage.Value);
                }
            }

            ChatManager.ResetAnnouncementTimer();
            ChatManager.RaiseChatCommandEvent += new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);
        }

        internal bool HasTradeOpen() {
            return hasTradeOpen;
        }

        internal int GetTradePartner() {
            return tradePartnerId;
        }

        private bool CheckSettings() {
            if ((Config.Tinkering.Enabled.Value || Config.CraftBot.Enabled.Value) && (Globals.Core.CharacterFilter.CharacterOptions & 0x80000000) == 0) {
                Util.WriteToChat("Error: You must enable the UseCraftSuccessDialog setting!");
                return false;
            }

            return true;
        }

        public void Stop() {
            if (!isRunning)
                return;

            Globals.Stats.globalStats.Save();

            Util.WriteToChat("DoThingsBot stopped");

            ChatManager.RaiseChatCommandEvent -= new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);

            _machine.Stop();
            queue.Clear();
            isRunning = false;

        }

        private bool disposed;

        public void Dispose() {

            CoreManager.Current.RenderFrame -= new EventHandler<EventArgs>(Current_RenderFrame);

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
                    //Remove the view
                    if (_machine != null)
                        _machine.Dispose();
                }

                // Indicate that the instance has been disposed.
                disposed = true;
            }
        }

        public bool IsLoggedIn = false;

        DateTime lastThought = DateTime.MinValue;

        void Current_RenderFrame(object sender, EventArgs e) {
            try {
                if (!IsLoggedIn) return;

                if (DateTime.UtcNow - lastThought < TimeSpan.FromMilliseconds(50))
                    return;

                lastThought = DateTime.UtcNow;

                Think();

            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        void ChatManager_ChatCommand(object sender, ChatCommandEventArgs e) {
            try {
                if (!IsLoggedIn) return;

                //Util.WriteToChat(String.Format("Got command: '{0}' from '{1}' args: '{2}'", e.Command, e.PlayerName, e.Arguments));
                ProcessCommand(e, false);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
        
        private static bool IsSubProfile(String s) {
            var profile = Buffs.Buffs.GetProfile(s);
            if (profile == null) return true;
            if (s.ToLower().StartsWith("_")) return true;
            if (profile.IsAutoGenerated()) return true;

            return false;
        }

        void ProcessCommand(ChatCommandEventArgs e, bool skipQueue) {
            if (!_machine.IsRunning) {
                return;
            }

            Util.WriteToChat($"cnd:{e.Command} p:{e.PlayerName} a:{e.Arguments}");

            if (currentItemBundle != null && queue.Count > 0) {
                currentItemBundle = null;
            }

            if (string.IsNullOrEmpty(e.Command)) return;

            switch (e.Command) {
                case "help":
                    Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);
                    PrintHelpMessage(e.PlayerName, e.Arguments);
                    break;

                case "about":
                    Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);
                    PrintAboutMessage(e.PlayerName, e.Arguments);
                    break;

                case "version":
                    Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);
                    PrintAboutMessage(e.PlayerName, e.Arguments);
                    break;

                case "reset":
                    if (!Config.Bot.EnableResetCommand.Value) {
                        ChatManager.Tell(e.PlayerName, "My reset command is disabled, sorry!");
                        return;
                    }
                    Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);
                    ChatManager.Say("I am restarting.");
                    Stop();
                    PostMessageTools.SendAltF4();
                    break;

                case "recipe":
                    if (!Config.CraftBot.Enabled.Value) {
                        ChatManager.Tell(e.PlayerName, "My crafting bot functionality is currently disabled, sorry!");
                        return;
                    }
                    Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);
                    PrintRecipeDetails(e.PlayerName, e.Arguments);
                    break;

                case "tool":
                    if (!Config.CraftBot.Enabled.Value) {
                        ChatManager.Tell(e.PlayerName, "My crafting bot functionality is currently disabled, sorry!");
                        return;
                    }
                    Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);
                    PrintToolDetails(e.PlayerName, e.Arguments);
                    break;

                case "recipes":
                    if (!Config.CraftBot.Enabled.Value) {
                        ChatManager.Tell(e.PlayerName, "My crafting bot functionality is currently disabled, sorry!");
                        return;
                    }
                    Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);
                    var message = $"I can make {Recipes.recipes.Count} recipes: ";

                    var names = new List<string>();

                    foreach (var recipe in Recipes.recipes) {
                        names.Add(recipe.name);
                    }

                    names.Sort();

                    foreach (var name in names) {
                        if (message.Length + name.Length + 2 > 230) {
                            ChatManager.Tell(e.PlayerName, message);
                            message = "";
                        }

                        message += $"{name}, ";
                    }

                    if (message.Length > 0) ChatManager.Tell(e.PlayerName, message);

                    ChatManager.Tell(e.PlayerName, "For more information about a recipe, tell me 'recipe Wedding Cake'");

                    break;

                case "message":
                    Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);
                    break;

                case "forcebuff":
                    if ((_machine.IsOrWillBeInState("BotIdleState") || skipQueue) && e.PlayerName == CoreManager.Current.CharacterFilter.Name) {
                        var itemBundle = new ItemBundle();
                        itemBundle.SetForceBuffMode(true);
                        Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);
                        _machine.ChangeState(new BotStartState(itemBundle));
                    }
                    break;

                case "skills":
                    if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue) {
                        var itemBundle = new ItemBundle(e.PlayerName);
                        currentItemBundle = itemBundle;

                        itemBundle.SetEquipMode(EquipMode.Tinker);
                        itemBundle.SetCraftMode(CraftMode.CheckSkills);
                        Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);
                        _machine.ChangeState(new BotStartState(itemBundle));
                    }
                    else {
                        AddToQueue(e.PlayerName, "skills");
                    }
                    break;

                case "whereto":
                    if (!Config.Portals.Enabled.Value) {
                        ChatManager.Tell(e.PlayerName, "My portal bot functionality is currently disabled, sorry!");
                        return;
                    }

                    RespondToWhereTo(e.PlayerName, e.Arguments);

                    break;

                case "remove":
                    Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);
                    RemoveFromQueue(e.PlayerName);
                    break;

                case "primary":
                    if (!Config.Portals.Enabled.Value) {
                        ChatManager.Tell(e.PlayerName, "My portal bot functionality is currently disabled, sorry!");
                        return;
                    }

                    if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue) {
                        var itemBundle = new ItemBundle(e.PlayerName);
                        currentItemBundle = itemBundle;
                        Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                        itemBundle.SetEquipMode(EquipMode.SummonPortal);
                        itemBundle.SetCraftMode(CraftMode.PrimaryPortal);
                        _machine.ChangeState(new BotStartState(itemBundle));
                    }
                    else {
                        AddToQueue(e.PlayerName, "primary");
                    }
                    break;

                case "secondary":
                    if (!Config.Portals.Enabled.Value) {
                        ChatManager.Tell(e.PlayerName, "My portal bot functionality is currently disabled, sorry!");
                        return;
                    }

                    if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue) {
                        var itemBundle = new ItemBundle(e.PlayerName);
                        currentItemBundle = itemBundle;
                        Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                        itemBundle.SetEquipMode(EquipMode.SummonPortal);
                        itemBundle.SetCraftMode(CraftMode.SecondaryPortal);
                        _machine.ChangeState(new BotStartState(itemBundle));
                    }
                    else {
                        AddToQueue(e.PlayerName, "secondary");
                    }
                    break;

                case "tinker":
                    if (!Config.Tinkering.Enabled.Value) {
                        ChatManager.Tell(e.PlayerName, "My tinker bot functionality is currently disabled, sorry!");
                        return;
                    }
                    if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue) {
                        var itemBundle = new ItemBundle(e.PlayerName);
                        itemBundle.SetCraftMode(CraftMode.WeaponTinkering);
                        currentItemBundle = itemBundle;
                        Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                        _machine.ChangeState(new BotStartState(itemBundle));
                    }
                    else {
                        AddToQueue(e.PlayerName, "tinker");
                    }
                    break;

                case "craft":
                    if (!Config.CraftBot.Enabled.Value) {
                        ChatManager.Tell(e.PlayerName, "My crafting bot functionality is currently disabled, sorry!");
                        return;
                    }

                    if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue) {
                        var itemBundle = new ItemBundle(e.PlayerName);
                        itemBundle.SetCraftMode(CraftMode.Crafting);
                        currentItemBundle = itemBundle;
                        Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                        _machine.ChangeState(new BotStartState(itemBundle));
                    }
                    else {
                        AddToQueue(e.PlayerName, "craft");
                    }
                    break;

                case "brilliance":
                    if (!Config.BrillBot.Enabled.Value)
                    {
                        ChatManager.Tell(e.PlayerName, "My Brilliance casting functionality is currently disabled, sorry!");
                        return;
                    }
                   
                    if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue)
                    {
                        var itemBundle = new ItemBundle(e.PlayerName);
                        itemBundle.SetCraftMode(CraftMode.Brill);
                        currentItemBundle = itemBundle;
                        Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                        _machine.ChangeState(new BotStartState(itemBundle));
                    }
                    else
                    {
                        AddToQueue(e.PlayerName, "brilliance");
                    }
                    break;

                case "stock":
                    if (!Config.Stock.Enabled.Value)
                    {
                        ChatManager.Tell(e.PlayerName, "My Stock Bot functionality is currently disabled, sorry!");
                        return;
                    }

                    if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue)
                    {
                        var itemBundle = new ItemBundle(e.PlayerName);
                        itemBundle.SetCraftMode(CraftMode.Stock);
                        currentItemBundle = itemBundle;
                        Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                        _machine.ChangeState(new BotStartState(itemBundle));
                    }
                    else
                    {
                        AddToQueue(e.PlayerName, "stock");
                    }
                    break;

                case "lostitems":
                    if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue) {
                        ItemBundle itemBundle = new ItemBundle(e.PlayerName);
                        currentItemBundle = itemBundle;
                        Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                        if (itemBundle.GetStolenItems().Count <= 0) {
                            ChatManager.Tell(itemBundle.GetOwner(), "I don't think I have any of your items.  If you think this is an error, leave me a message.");
                        }
                        else {
                            itemBundle.SetCraftMode(CraftMode.GiveBackItems);
                            _machine.ChangeState(new BotTradingState(itemBundle));
                        }
                    }
                    else {
                        AddToQueue(e.PlayerName, "lostitems");
                    }
                    break;

                case "go":
                    break;

                case "cancel":
                    break;

                case "stats":
                    Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                    if (!string.IsNullOrEmpty(e.Arguments) && e.Arguments.Trim() == "global") {
                        ChatManager.Tell(e.PlayerName, Globals.Stats.GetGlobalStatsMessage());
                    }
                    else {
                        ChatManager.Tell(e.PlayerName, Globals.Stats.GetCharacterStatsMessage(e.PlayerName));
                    }

                    break;

                case "comps":
                    List<string> comps = new List<string>();

                    foreach (var comp in ComponentManager.trackedComponents) {
                        comps.Add(string.Format("{0}: {1}", comp.Name, comp.Count()));
                    }

                    var gems = Config.Portals.GetUniqueGemNames();
                    foreach (var gem in gems) {
                        var lowCount = Config.Portals.PortalGemLowCount.Value;
                        var count = Util.GetItemCount(gem);
                        comps.Add($"{gem}: {count}");
                    }

                    ChatManager.Tell(e.PlayerName, string.Format("My component levels: {0}", string.Join(", ", comps.ToArray())));
                    break;

                case "profiles":
                    if (!Config.BuffBot.Enabled.Value) {
                        ChatManager.Tell(e.PlayerName, "My buff bot functionality is currently disabled, sorry!");
                        return;
                    }

                    Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                    var profiles = new List<string>(Buffs.Buffs.profiles.Keys);

                    profiles.RemoveAll(IsSubProfile);

                    var msg = "I support these buff profiles: " + string.Join(", ", profiles.ToArray()) + ".";

                    if (Config.BuffBot.EnableSingleBuffs.Value) {
                        msg += " Singles spells like 'focus warmagic' are also supported.";
                    }

                    Util.WriteToChat(msg);

                    ChatManager.Tell(e.PlayerName, msg);

                    if (Config.BuffBot.EnableTreeStatsBuffs.Value) {
                        ChatManager.Tell(e.PlayerName, "You can tell me 'buff' and I will buff you based on your TreeStats profile.");
                    }

                    break;

                case "buff":
                    if (!Config.BuffBot.EnableTreeStatsBuffs.Value) {
                        ChatManager.Tell(e.PlayerName, "My TreeStats buffing profile functionality is currently disabled, sorry!");
                        return;
                    }
                    if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue) {
                        ItemBundle itemBundle = new ItemBundle(e.PlayerName);
                        currentItemBundle = itemBundle;
                        itemBundle.SetCraftMode(CraftMode.Buff);
                        itemBundle.SetEquipMode(EquipMode.Buff);
                        itemBundle.SetBuffProfiles("treestats");

                        Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                        _machine.ChangeState(new BotStartState(itemBundle));
                    }
                    else {
                        AddToQueue(e.PlayerName, "buff");
                    }
                    break;

                case "rations":
                    if (!Config.Bot.HasInfiniteRations()) {
                        ChatManager.Tell(e.PlayerName, "Sorry, I don't have any infinite rations.");
                        return;
                    }
                    if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue) {
                        ItemBundle itemBundle = new ItemBundle(e.PlayerName);
                        currentItemBundle = itemBundle;
                        itemBundle.SetCraftMode(CraftMode.InfiniteRations);
                        itemBundle.SetEquipMode(EquipMode.Idle);

                        Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                        _machine.ChangeState(new BotStartState(itemBundle));
                    }
                    else {
                        AddToQueue(e.PlayerName, "rations");
                    }
                    break;

                case "leather":
                    if (!Config.Bot.HasInfiniteLeather()) {
                        ChatManager.Tell(e.PlayerName, "Sorry, I don't have any infinite leather.");
                        return;
                    }
                    if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue) {
                        ItemBundle itemBundle = new ItemBundle(e.PlayerName);
                        currentItemBundle = itemBundle;
                        itemBundle.SetCraftMode(CraftMode.InfiniteLeather);
                        itemBundle.SetEquipMode(EquipMode.Idle);

                        Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                        _machine.ChangeState(new BotStartState(itemBundle));
                    }
                    else {
                        AddToQueue(e.PlayerName, "leather");
                    }
                    break;

                case "dye":
                    if (!Config.Bot.HasInfiniteDyes()) {
                        ChatManager.Tell(e.PlayerName, "Sorry, I don't have any infinite leather.");
                        return;
                    }
                    if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue) {
                        ItemBundle itemBundle = new ItemBundle(e.PlayerName);
                        currentItemBundle = itemBundle;
                        itemBundle.SetCraftMode(CraftMode.InfiniteDye);
                        itemBundle.SetEquipMode(EquipMode.Idle);

                        Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                        _machine.ChangeState(new BotStartState(itemBundle));
                    }
                    else {
                        AddToQueue(e.PlayerName, "dye");
                    }
                    break;


                default:
                    // check for buff command
                    var allBuffProfiles = Buffs.Buffs.GetAllProfileCommands();

                    if (allBuffProfiles.Contains(e.Command)) {
                        if (!Config.BuffBot.Enabled.Value) {
                            ChatManager.Tell(e.PlayerName, "My Buff Bot functionality is currently disabled, sorry!");
                            return;
                        }

                        var commands = e.Command;
                        if (!string.IsNullOrEmpty(e.Arguments)) {
                            commands += " " + e.Arguments;
                        }

                        if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue) {
                            ItemBundle itemBundle = new ItemBundle(e.PlayerName);
                            currentItemBundle = itemBundle;
                            itemBundle.SetCraftMode(CraftMode.Buff);
                            itemBundle.SetEquipMode(EquipMode.Buff);
                            itemBundle.SetBuffProfiles(commands);

                            foreach (var command in commands.Split(' ')) {
                                if (allBuffProfiles.Contains(command)) {
                                    Globals.Stats.AddPlayerCommandIssued(e.PlayerName, command);
                                }
                            }

                            _machine.ChangeState(new BotStartState(itemBundle));
                        }
                        else {
                            AddToQueue(e.PlayerName, commands);
                        }
                        return;
                    }

                    // portal alternative (extra) commands
                    if (e.Command == Config.Portals.PrimaryPortalExtraCommand.Value || e.Command == Config.Portals.SecondaryPortalExtraCommand.Value) {
                        if (!Config.Portals.Enabled.Value) {
                            ChatManager.Tell(e.PlayerName, "My portal bot functionality is currently disabled, sorry!");
                            return;
                        }

                        if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue) {
                            var itemBundle = new ItemBundle(e.PlayerName);
                            currentItemBundle = itemBundle;
                            Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                            CraftMode portalMode;

                            if (e.Command == Config.Portals.PrimaryPortalExtraCommand.Value) {
                                portalMode = CraftMode.PrimaryPortal;
                            }
                            else {
                                portalMode = CraftMode.SecondaryPortal;
                            }

                            itemBundle.SetEquipMode(EquipMode.SummonPortal);
                            itemBundle.SetCraftMode(portalMode);
                            _machine.ChangeState(new BotStartState(itemBundle));
                        }
                        else {
                            AddToQueue(e.PlayerName, e.Command);
                        }
                        return;
                    }

                    // portal gem commands
                    if (Config.Portals.PortalGemCommands().ContainsKey(e.Command)) {
                        if (!Config.Portals.Enabled.Value) {
                            ChatManager.Tell(e.PlayerName, "My Portal Bot functionality is currently disabled, sorry!");
                            return;
                        }
                        if (_machine.IsOrWillBeInState("BotIdleState") || skipQueue) {
                            var itemBundle = new ItemBundle(e.PlayerName);
                            currentItemBundle = itemBundle;
                            Globals.Stats.AddPlayerCommandIssued(e.PlayerName, e.Command);

                            itemBundle.SetEquipMode(EquipMode.Idle);
                            itemBundle.SetCraftMode(CraftMode.PortalGem);
                            itemBundle.SetPortalCommand(e.Command);

                            _machine.ChangeState(new BotStartState(itemBundle));
                        }
                        else {
                            AddToQueue(e.PlayerName, e.Command);
                        }
                        return;
                    }
                    
                    if (Config.Bot.RespondToUnknownCommands.Value == true) {
                        if (!_machine.IsOrWillBeInState("BotIdleState") && currentItemBundle != null && currentItemBundle.GetOwner() == e.PlayerName)
                            return;

                        ChatManager.Tell(e.PlayerName, "Sorry, I'm a bot and do not understand that command.  Please tell me \"help\" for a list of available commands.");
                    }
                    break;
            }
        }

        internal bool HasBuffingJobInQueue() {
            foreach (var qi in queue) {
                if (IsBuffingJob(qi)) return true;
            }

            return false;
        }

        internal bool HasTinkeringJobInQueue() {
            foreach (var qi in queue) {
                if (IsTinkeringJob(qi)) return true;
            }

            return false;
        }

        internal bool HasLostItemsJobInQueue() {
            foreach (var qi in queue) {
                if (IsLostItemJob(qi)) return true;
            }

            return false;
        }

        internal bool IsBuffingJob(PlayerCommand command) {
            var allBuffProfiles = Buffs.Buffs.GetAllProfileCommands();

            if (command.Command.StartsWith("buff")) return true;
            if (allBuffProfiles.Contains(command.Command.Split(' ')[0])) return true;

            return false;
        }

        internal bool IsTinkeringJob(PlayerCommand command) {
            return command.Command.StartsWith("tinker");
        }

        internal bool IsLostItemJob(PlayerCommand command) {
            return command.Command.StartsWith("lostitems");
        }

        private void PrintToolDetails(string playerName, string arguments) {
            var toolLocation = Recipes.GetToolLocation(arguments);

            if (String.IsNullOrEmpty(toolLocation)) {
                ChatManager.Tell(playerName, $"Unable to find a tool with the name '{arguments}'");
            }
            else {
                ChatManager.Tell(playerName, $"{arguments} {toolLocation}");
            }
        }

        private void PrintRecipeDetails(string playerName, string arguments) {
            var recipe = Recipes.FindByName(arguments);

            if (recipe == null) {
                ChatManager.Tell(playerName, $"Unable to find a recipe with the name '{arguments}'");
            }
            else {
                ChatManager.Tell(playerName, recipe.summary());
            }
        }

        internal void RespondToWhereTo(string playerName, string arguments="") {
            if (!Config.Portals.Enabled.Value)
            {
                return;
            }

            Globals.Stats.AddPlayerCommandIssued(playerName, "whereto");

            if (string.IsNullOrEmpty(arguments)) {
                ChatManager.Tell(playerName, String.Format("I am currently tied to {0} and {1}. '/t {2}, primary' for {0}. '/t {2}, secondary' for {1}",
                    Config.Portals.PrimaryPortalTieLocation.Value,
                    Config.Portals.SecondaryPortalTieLocation.Value,
                    CoreManager.Current.CharacterFilter.Name));

                var validPortalCommands = Config.Portals.GetValidPortalGemCommands();
                if (validPortalCommands.Length > 0) {
                    ChatManager.Tell(playerName, string.Format("I can also summon: {0}", string.Join(", ", validPortalCommands)));
                }
            }
            else {
                string willSummon = "";

                if (arguments == Config.Portals.PrimaryPortalExtraCommand.Value) {
                    willSummon = Config.Portals.PrimaryPortalTieLocation.Value;
                }
                else if (arguments == Config.Portals.SecondaryPortalExtraCommand.Value) {
                    willSummon = Config.Portals.PrimaryPortalTieLocation.Value;
                }
                else if (Config.Portals.PortalGemCommands().ContainsKey(arguments)) {
                    willSummon = Config.Portals.PortalGemCommands()[arguments].Name;
                }

                if (string.IsNullOrEmpty(willSummon)) {
                    ChatManager.Tell(playerName, string.Format("I don't know how to summon '{0}'", arguments));
                }
                else {
                    ChatManager.Tell(playerName, string.Format("If you '/t {0}, {1}' I will summon {2}",
                        Globals.Core.CharacterFilter.Name,
                        arguments,
                        willSummon));
                }
            }
        }

        void PrintHelpMessage(string playerName, string arguments) {

            switch (arguments) {
                case "tinker":
                    ChatManager.Tell(playerName, "tinker - Tinkers a loot generated item by adding salvage. Make sure you are standing nearby.");
                    break;

                case "lostitems":
                    ChatManager.Tell(playerName, "lostitems - I will check my inventory for any items you may have lost to me. Make sure you are standing nearby.");
                    break;

                case "message":
                    ChatManager.Tell(playerName, "message - Leave me a message. eg: /tell " + CoreManager.Current.CharacterFilter.Name + ", message I think your bot is broken.");
                    break;

                case "remove":
                    ChatManager.Tell(playerName, "remove - Remove yourself from the queue.");
                    break;

                case "skills":
                    ChatManager.Tell(playerName, "skills - I will tell you my current skill levels.");
                    break;

                case "tool":
                    ChatManager.Tell(playerName, "tool [tool name] - I will tell where to find a tool.");
                    break;

                case "recipe":
                    ChatManager.Tell(playerName, "recipe [recipe name] - I will tell you information about a recipe.");
                    break;

                case "recipes":
                    ChatManager.Tell(playerName, "recipes - I will tell you all the recipes I know. Careful, there are a lot of them!");
                    break;

                case "reset":
                    ChatManager.Tell(playerName, "I will reset my client, in case I am stuck.");
                    break;

                case "whereto":
                    ChatManager.Tell(playerName, "whereto [location] - I will tell you where my portals are currently tied to, and what portal gems I can use.");
                    break;

                case "about":
                    ChatManager.Tell(playerName, "about - I will tell you about my software.");
                    break;

                case "stats":
                    ChatManager.Tell(playerName, "stats [global] - I will tell you some stats about yourself, or global stats about the bot.");
                    break;

                case "comps":
                    ChatManager.Tell(playerName, "comps - I will tell you my current component levels.");
                    break;

                case "buff":
                    ChatManager.Tell(playerName, "buff - I will automatically buff you based on your TreeStats profile.");
                    break;

                case "profiles":
                    ChatManager.Tell(playerName, "profiles - I will tell you what buff profiles I support.");
                    break;

                case "brilliance":
                    ChatManager.Tell(playerName, "brilliance - I will attempt to cast Brilliance on you.");
                    break;
                case "stock":
                    ChatManager.Tell(playerName, "stock - I will attempt to supply stocked items for use.");
                    break;
                default:
                    ChatManager.Tell(playerName, $"Hello! I am a DoThingsBot. You can issue me the following command profiles to get started:");

                    if (Config.BuffBot.Enabled.Value)
                    {
                        ChatManager.Tell(playerName, "'buff' for TreeStats profiles, or 'profiles' to see what buff profiles I support; ");
                    }
                    if (Config.CraftBot.Enabled.Value)
                    {
                        ChatManager.Tell(playerName, "'craft'; ");
                    }
                    if (Config.Portals.Enabled.Value)
                    {
                        ChatManager.Tell(playerName, "'whereto' to see where I can summon; ");
                    }
                    if (Config.Tinkering.Enabled.Value)
                    {
                        ChatManager.Tell(playerName, "'tinker'; ");
                    }
                    if (Config.BrillBot.Enabled.Value)
                    {
                        ChatManager.Tell(playerName, "'brilliance' to get extra Focus; ");
                    }
                    if (Config.Stock.Enabled.Value)
                    {
                        ChatManager.Tell(playerName, "'stock' to see what items I can restock you with; ");
                    }
                    ChatManager.Tell(playerName, $"additional commands: lostitems, message, about, stats, comps, recipes, recipe, tool, reset.");
                    ChatManager.Tell(playerName, "You can also try 'help [command]' to get more information about a specific command.");
                    DisplayInfinites(playerName);
                    break;

            }
        }

        private void DisplayInfinites(string playerName) {
            var message = "";
            if (Config.Bot.HasInfiniteLeather()) {
                message += "I have Infinite Leather! Tell me 'leather' and add your item to trade, and i'll add the leather to it. ";
            }
            if (Config.Bot.HasInfiniteRations()) {
                message += "I have infinite rations! Tell me 'rations' and I'll give you some. ";
            }
            if (Config.Bot.HasInfiniteDyes()) {
                var colors = string.Join(", ", Config.Bot.InfiniteDyeColors().ToArray());
                message += $"I have infinite dye ({colors})! Tell me 'dye' and I'll dye your stuff.";
            }
            if (message != "")
            {
                ChatManager.Tell(playerName, message);
            }
        }

        void PrintAboutMessage(string playerName, string arguments) {
            ChatManager.Tell(playerName, String.Format("I'm a Bot running DoThingsBot v{0}. - Download the plugin yourself at https://github.com/HelixNightbane/DoThingsBot .", Util.GetVersion()));
        }

        void RemoveFromQueue(string playerName, bool silent=false) {
            if (queue.Exists(x => x.Equals(playerName))) {
                int index = queue.FindIndex(x => x.Equals(playerName));
                queue.RemoveAt(index);

                if (!silent) ChatManager.Tell(playerName, "You have been removed from the queue");
            }
            else {
                if (!silent) ChatManager.Tell(playerName, "You aren't in line!");
            }

            SaveQueue();
        }

        public void AddToQueue(string command) {
            queue.Add(new PlayerCommand(CoreManager.Current.CharacterFilter.Name, command));

            SaveQueue();
        }

        public void AddToQueue(string playerName, string command, bool silent=false) {
            if (currentItemBundle != null && currentItemBundle.HasOwner() && currentItemBundle.GetOwner() == playerName) {
                if (!silent) ChatManager.Tell(playerName, "I am already helping you.  Please wait until you are finished before issuing more commands.");
                return;
            }

            foreach (PlayerCommand pc in queue) {
                if (pc.PlayerName == playerName) {
                    if (!silent) ChatManager.Tell(playerName, "You are already in line!");
                    return;
                }
            }

            if (_machine.IsOrWillBeInState("BotBuffingState")) {
                if (!silent) ChatManager.Tell(playerName, String.Format("I am currently buffing, but you have been added to the queue."));
            }
            else {
                if (!silent) ChatManager.Tell(playerName, String.Format("I am currently helping someone else, but you have been added to the queue.  There are currently {0} people ahead of you.", queue.Count + 1));
            }

            queue.Add(new PlayerCommand(playerName, command));

            SaveQueue();
        }

        private void SaveQueue() {
            try {
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                settings.NewLineOnAttributes = true;

                XmlWriter writer = XmlWriter.Create(Path.Combine(Util.GetCharacterDataDirectory(), "queue.xml"), settings);

                writer.WriteStartElement("Queue");
                foreach (var item in queue) {
                    writer.WriteStartElement("Item");
                    writer.WriteAttributeString("PlayerName", null, item.PlayerName);
                    writer.WriteAttributeString("Command", null, item.Command);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();

                writer.Close();
                Util.WriteToChat($"Queue count: {queue.Count}");
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void LoadQueue() {
            try {
                if (!File.Exists(Path.Combine(Util.GetCharacterDataDirectory(), "queue.xml"))) return;

                XmlDocument doc = new XmlDocument();
                doc.Load(Path.Combine(Util.GetCharacterDataDirectory(), "queue.xml"));

                foreach (XmlNode node in doc.DocumentElement.ChildNodes) {
                    try {
                        if (node.Attributes["PlayerName"] != null && node.Attributes["PlayerName"].Value.Length > 0 && node.Attributes["Command"] != null && node.Attributes["Command"].Value.Length > 0) {
                            AddToQueue(node.Attributes["PlayerName"].Value, node.Attributes["Command"].Value, true);
                        }
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        DateTime lastUpdatedUptime = DateTime.UtcNow;
        DateTime lastDequeue = DateTime.UtcNow;
        internal bool needsEquipmentCheck = true;

        void Think() {
            try {
                if (!IsLoggedIn) return;
                
                if (Config.Bot.Enabled.Value == true && !isRunning) {
                    Start();
                }
                else if (Config.Bot.Enabled.Value == false && isRunning) {
                    Stop();
                }

                if (isRunning) {
                    DangerousMonsterDetector.Think();

                    if (_machine.IsInState("BotIdleState") && DateTime.UtcNow - lastDequeue > TimeSpan.FromMilliseconds(1000)) {
                        if (queue.Count > 0) {
                            if (!HasPausedJobs() || !TryResumePausedJob()) {
                                lastDequeue = DateTime.UtcNow;
                                var parts = queue[0].Command.Split(' ');

                                var command = "";
                                var arguments = "";

                                if (parts.Length == 1) {
                                    command = parts[0];
                                    ProcessCommand(new ChatCommandEventArgs(queue[0].PlayerName, command, queue[0].Command), true);
                                }
                                else {
                                    command = parts[0];
                                    var p2 = (new List<string>(parts));
                                    p2.RemoveAt(0);
                                    arguments = string.Join(" ", p2.ToArray());
                                    ProcessCommand(new ChatCommandEventArgs(queue[0].PlayerName, command, queue[0].Command, arguments), true);
                                }

                                queue.RemoveAt(0);
                                SaveQueue();
                            }
                        }
                        else {
                            if (HasPausedJobs()) {
                                TryResumePausedJob();
                            }
                        }
                    }

                    ChatManager.Think();

                    _machine.Think();

                    Globals.Stats.Think();
                    Globals.StatsView.Think();
                    LostItems.Think();
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private bool HasPausedJobs() {
            DirectoryInfo d = new DirectoryInfo(Util.GetResumablePlayersDataDirectory());
            FileInfo[] fileList = d.GetFiles("*.json");

            return fileList.Length > 0;
        }

        private bool TryResumePausedJob() {
            DirectoryInfo d = new DirectoryInfo(Util.GetResumablePlayersDataDirectory());
            FileInfo[] fileList = d.GetFiles("*.json");

            Array.Sort<FileInfo>(fileList, delegate (FileInfo m, FileInfo n) {
                return (int)((n.LastWriteTimeUtc - m.LastWriteTimeUtc).TotalSeconds);
            });

            foreach (var file in fileList) {
                var playerName = file.Name.Replace(".json", "");
                var bundle = new ItemBundle(playerName);
                var shouldBreak = false;

                if (bundle == null || bundle.playerData == null || !bundle.DidLoad) {
                    Util.WriteToChat($"Unable to resume job for: {playerName}. Skipping.");
                    File.Delete(Path.Combine(Util.GetResumablePlayersDataDirectory(), file.Name));
                    continue;
                }

                if (bundle.playerData.jobType == "craft" && Config.CraftBot.PauseSessionForOtherJobs.Value == true && queue.Count > 0) {
                    continue;
                }

                Util.WriteToChat($"Attempting to resume job type '{bundle.playerData.jobType}' for player {playerName}");
                
                switch (bundle.playerData.jobType) {
                    case "craft":
                        bundle.SetCraftMode(CraftMode.Crafting);
                        bundle.SetEquipMode(EquipMode.Craft);
                        currentItemBundle = bundle;
                        _machine.ChangeState(new BotStartState(bundle));
                        shouldBreak = true;
                        break;

                    case "tinker":
                        bundle.SetCraftMode(CraftMode.WeaponTinkering);
                        bundle.SetEquipMode(EquipMode.Tinker);
                        currentItemBundle = bundle;
                        _machine.ChangeState(new BotStartState(bundle));
                        shouldBreak = true;
                        break;

                    case "buff":
                        bundle.SetCraftMode(CraftMode.Buff);
                        bundle.SetEquipMode(EquipMode.Buff);
                        currentItemBundle = bundle;
                        _machine.ChangeState(new BotStartState(bundle));
                        shouldBreak = true;
                        break;

                    default:
                        Util.WriteToChat($"I don't know how to resume job type '{bundle.playerData.jobType}' for player {playerName}");
                        bundle.Unpause();
                        break;
                }

                if (shouldBreak) return true;
            }

            return false;
        }
    }
}
