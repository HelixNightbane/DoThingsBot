using Decal.Adapter;
using DoThingsBot.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DoThingsBot.FSM.States {
    class BotInfinites_ChooseDyeState : IBotState {
        public string Name { get => "BotInfinites_ChooseDyeState"; }

        public int ThinkCount { get => _thinkCounter; }
        private int _thinkCounter = 0;

        private ItemBundle itemBundle;
        private Machine _machine;

        public BotInfinites_ChooseDyeState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            _machine = machine;

            var dyes = Config.Bot.InfiniteDyeColors();

            if (dyes.Count == 1) {
                ChatManager.Tell(itemBundle.GetOwner(), $"I will dye your items with {dyes[0]} ({Util.FriendlyDyeColor(dyes[0])})");
                machine.ChangeState(new BotTradingState(itemBundle));
                return;
            }

            var colors = string.Join(", ", dyes.Select(c => $"{c} ({Util.FriendlyDyeColor(c)})").ToArray());
            ChatManager.Tell(itemBundle.GetOwner(), $"I have the following colors available, please /t me with the name of a color: {colors}");

            ChatManager.RaiseChatCommandEvent += ChatManager_ChatCommand;
        }

        void ChatManager_ChatCommand(object sender, ChatCommandEventArgs e) {
            try {
                //Util.WriteToChat(String.Format("Got command: '{0}' from '{1}' args: '{2}'", e.Command, e.PlayerName, e.Arguments));
                var dyes = Config.Bot.InfiniteDyeColors();
                var colors = string.Join(", ", dyes.Select(c => $"{c} ({Util.FriendlyDyeColor(c)})").ToArray());

                switch (e.Command) {
                    case "argenory":
                    case "white":
                        if (!Config.Bot.HasInfiniteDye("Perennial Argenory Dye")) {
                            ChatManager.Tell(itemBundle.GetOwner(), $"I only have the following dyes: {colors}.");
                        }
                        itemBundle.SetInfiniteDye("Perennial Argenory Dye");
                        ChatManager.Tell(itemBundle.GetOwner(), $"I will use the Argenory ({Util.FriendlyDyeColor("Argenory")}) dye.");
                        _machine.ChangeState(new BotTradingState(itemBundle));
                        break;
                    case "berimphur":
                    case "yellow":
                        if (!Config.Bot.HasInfiniteDye("Perennial Berimphur Dye")) {
                            ChatManager.Tell(itemBundle.GetOwner(), $"I only have the following dyes: {colors}.");
                        }
                        itemBundle.SetInfiniteDye("Perennial Berimphur Dye");
                        ChatManager.Tell(itemBundle.GetOwner(), $"I will use the Berimphur ({Util.FriendlyDyeColor("Berimphur")}) dye.");
                        _machine.ChangeState(new BotTradingState(itemBundle));
                        break;
                    case "botched":
                        if (!Config.Bot.HasInfiniteDye("Perennial Botched Dye")) {
                            ChatManager.Tell(itemBundle.GetOwner(), $"I only have the following dyes: {colors}.");
                        }
                        itemBundle.SetInfiniteDye("Perennial Botched Dye");
                        ChatManager.Tell(itemBundle.GetOwner(), $"I will use the Botched ({Util.FriendlyDyeColor("Botched")}) dye.");
                        _machine.ChangeState(new BotTradingState(itemBundle));
                        break;
                    case "colban":
                    case "darkblue":
                        if (!Config.Bot.HasInfiniteDye("Perennial Colban Dye")) {
                            ChatManager.Tell(itemBundle.GetOwner(), $"I only have the following dyes: {colors}.");
                        }
                        itemBundle.SetInfiniteDye("Perennial Colban Dye");
                        ChatManager.Tell(itemBundle.GetOwner(), $"I will use the Colban ({Util.FriendlyDyeColor("Colban")}) dye.");
                        _machine.ChangeState(new BotTradingState(itemBundle));
                        break;
                    case "hennacin":
                    case "red":
                        if (!Config.Bot.HasInfiniteDye("Perennial Hennacin Dye")) {
                            ChatManager.Tell(itemBundle.GetOwner(), $"I only have the following dyes: {colors}.");
                        }
                        itemBundle.SetInfiniteDye("Perennial Hennacin Dye");
                        ChatManager.Tell(itemBundle.GetOwner(), $"I will use the Hennacin ({Util.FriendlyDyeColor("Hennacin")}) dye.");
                        _machine.ChangeState(new BotTradingState(itemBundle));
                        break;
                    case "lapyan":
                    case "blue":
                        if (!Config.Bot.HasInfiniteDye("Perennial Lapyan Dye")) {
                            ChatManager.Tell(itemBundle.GetOwner(), $"I only have the following dyes: {colors}.");
                        }
                        itemBundle.SetInfiniteDye("Perennial Lapyan Dye");
                        ChatManager.Tell(itemBundle.GetOwner(), $"I will use the Lapyan ({Util.FriendlyDyeColor("Lapyan")}) dye.");
                        _machine.ChangeState(new BotTradingState(itemBundle));
                        break;
                    case "minalim":
                    case "green":
                        if (!Config.Bot.HasInfiniteDye("Perennial Minalim Dye")) {
                            ChatManager.Tell(itemBundle.GetOwner(), $"I only have the following dyes: {colors}.");
                        }
                        itemBundle.SetInfiniteDye("Perennial Minalim Dye");
                        ChatManager.Tell(itemBundle.GetOwner(), $"I will use the Minalim ({Util.FriendlyDyeColor("Minalim")}) dye.");
                        _machine.ChangeState(new BotTradingState(itemBundle));
                        break;
                    case "relanim":
                    case "purple":
                        if (!Config.Bot.HasInfiniteDye("Perennial Relanim Dye")) {
                            ChatManager.Tell(itemBundle.GetOwner(), $"I only have the following dyes: {colors}.");
                        }
                        itemBundle.SetInfiniteDye("Perennial Relanim Dye");
                        ChatManager.Tell(itemBundle.GetOwner(), $"I will use the Relanim ({Util.FriendlyDyeColor("Relanim")}) dye.");
                        _machine.ChangeState(new BotTradingState(itemBundle));
                        break;
                    case "thananim":
                    case "black":
                        if (!Config.Bot.HasInfiniteDye("Perennial Thananim Dye")) {
                            ChatManager.Tell(itemBundle.GetOwner(), $"I only have the following dyes: {colors}.");
                        }
                        itemBundle.SetInfiniteDye("Perennial Thananim Dye");
                        ChatManager.Tell(itemBundle.GetOwner(), $"I will use the Thananim ({Util.FriendlyDyeColor("Thananim")}) dye.");
                        _machine.ChangeState(new BotTradingState(itemBundle));
                        break;
                    case "verdalim":
                    case "darkgreen":
                        if (!Config.Bot.HasInfiniteDye("Perennial Verdalim Dye")) {
                            ChatManager.Tell(itemBundle.GetOwner(), $"I only have the following dyes: {colors}.");
                        }
                        itemBundle.SetInfiniteDye("Perennial Verdalim Dye");
                        ChatManager.Tell(itemBundle.GetOwner(), $"I will use the Verdalim ({Util.FriendlyDyeColor("Verdalim")}) dye.");
                        _machine.ChangeState(new BotTradingState(itemBundle));
                        break;

                    case "cancel":
                        _machine.ChangeState(new BotFinishState(itemBundle));
                        break;

                    default:
                        ChatManager.Tell(itemBundle.GetOwner(), $"Invalid color. I have the following dyes: {colors}.");
                        break;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public void Exit(Machine machine) {
            ChatManager.RaiseChatCommandEvent -= ChatManager_ChatCommand;
        }

        private DateTime lastThought = DateTime.UtcNow;
        private DateTime startTime = DateTime.UtcNow;

        public void Think(Machine machine) {
            try {
                if (DateTime.UtcNow - lastThought > TimeSpan.FromMilliseconds(1000)) {
                    lastThought = DateTime.UtcNow;

                    if (DateTime.UtcNow - startTime > TimeSpan.FromSeconds(30)) {
                        ChatManager.Tell(itemBundle.GetOwner(), "Timed out. Please start over.");
                        machine.ChangeState(new BotFinishState(itemBundle));
                        return;
                    }
                }
            }
            catch (Exception e) { Util.LogException(e); }
        }

        public ItemBundle GetItemBundle() {
            return null;
        }
    }
}
