using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using DoThingsBot.Lib;
using DoThingsBot.Lib.Recipes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DoThingsBot.FSM.States {
    public class BotCraftingState : IBotState {
        public string Name { get => "BotCraftingState"; }

        private Machine machine;
        private ItemBundle itemBundle;
        private bool IsRunning = false;
        private DateTime lastAction = DateTime.UtcNow;
        private int actionCount = 0;
        private List<RecipeStep> steps;
        private bool needsChoice = false;
        private bool loadedRecipes = false;

        public Recipe recipe;
        public List<Recipe> recipes;
        private bool shouldCraft = false;
        private bool isCrafting = false;
        private bool shouldPause = false;
        private List<int> stackThese = new List<int>();
        private List<int> itemsAwaitingIdentity = new List<int>();
        private string waitingForItem = "";

        public BotCraftingState(ItemBundle items) {
            itemBundle = items;
            itemBundle.playerData.jobType = "craft";
        }

        public void Enter(Machine machine) {
            try {
                this.machine = machine;
                ChatManager.RaiseChatCommandEvent += new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);
                CoreManager.Current.ChatBoxMessage += Current_ChatBoxMessage;
                CoreManager.Current.EchoFilter.ServerDispatch += new EventHandler<NetworkMessageEventArgs>(EchoFilter_ServerDispatch);
                CoreManager.Current.WorldFilter.CreateObject += WorldFilter_CreateObject;
                CoreManager.Current.WorldFilter.ChangeObject += WorldFilter_ChangeObject;

                IsRunning = true;

                try {
                    PostMessageTools.ClickNo();
                }
                catch (Exception e) { Util.LogException(e); }

                if (itemBundle.WasPaused) {
                    shouldCraft = true;
                    recipe = Recipes.FindByName(itemBundle.playerData.recipe);
                    itemBundle.recipe = recipe;
                    itemBundle.SetRecipeIngredientList(itemBundle.GetValidRecipeIngredients());
                    loadedRecipes = true;
                }
                else {
                    foreach (var item in itemBundle.GetItems()) {
                        var wo = CoreManager.Current.WorldFilter[item];
                        if (!wo.HasIdData) {
                            itemsAwaitingIdentity.Add(wo.Id);
                            CoreManager.Current.Actions.RequestId(wo.Id);
                        }
                    }

                    if (itemsAwaitingIdentity.Count == 0)
                        FindRecipes();
                }
            }
            catch (Exception e) { Util.LogException(e); }
        }

        private void FindRecipes() {
            loadedRecipes = true;
            recipes = Recipes.FindByItemBundle(itemBundle);
            itemBundle.SetRecipeIngredientList(itemBundle.GetIngredientList());

            if (recipes.Count > 1) {
                needsChoice = true;
                List<string> choices = GetRecipeChoices();

                Chat.ChatManager.Tell(itemBundle.GetOwner(), $"I am able to make multiple recipes with those ingredients.  Please respond with a number from the following list of recipes to make: ");
                Chat.ChatManager.Tell(itemBundle.GetOwner(), String.Join(" ", choices.ToArray()));
            }
            else if (recipes.Count == 1) {
                recipe = recipes[0];
                itemBundle.playerData.recipe = recipe.name;
                shouldCraft = true;
            }
            else {
                ChatManager.Tell(itemBundle.GetOwner(), "Something went wrong and I don't know a recipe for that I guess... ");
                Bail();
            }
        }

        private void WorldFilter_ChangeObject(object sender, ChangeObjectEventArgs e) {
            try {
                if (e.Change == WorldChangeType.IdentReceived && itemsAwaitingIdentity.Contains(e.Changed.Id))
                    itemsAwaitingIdentity.Remove(e.Changed.Id);
            }
            catch (Exception ex) {
                Util.LogException(ex);
            }
        }

        private void WorldFilter_CreateObject(object sender, CreateObjectEventArgs e) {
            try {
                var step = itemBundle.GetCurrentRecipeStep();

                //if (step == null) {
                //    return;
                //}

                // dont capture tools, those are ours
                if (Recipes.allTools.Contains(e.New.Name))
                    return;

                if (!itemBundle.playerData.itemIds.Contains(e.New.Id)) {
                    itemBundle.playerData.itemIds.Add(e.New.Id);
                    itemBundle.SavePlayerData();

                    foreach (var id in itemBundle.playerData.itemIds) {
                        var wo = CoreManager.Current.WorldFilter[id];
                        if (wo != null && wo.Name == e.New.Name) {
                            stackThese.Add(e.New.Id);
                        }
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e) {
            try {
                var step = itemBundle.GetCurrentRecipeStep();

                if (step == null) return;

                if (e.Text.StartsWith(step.successMessage)) {
                    Util.WriteToDebugLog("Got Success Message: " + step.successMessage);
                    if (step.successDestroySourceAmount > 0) {
                        itemBundle.recipeIngredients.Remove(step.use, step.successDestroySourceAmount);
                    }
                    if (step.successDestroyTargetAmount > 0) {
                        itemBundle.recipeIngredients.Remove(step.on, step.successDestroyTargetAmount);
                    }

                    itemBundle.recipeIngredients.Add(step.result, step.successAmount);
                    itemBundle.recipeSteps.RemoveAt(0);
                    isCrafting = false;
                    lastThought = DateTime.UtcNow + TimeSpan.FromMilliseconds(300);

                    //Chat.ChatManager.Tell(itemBundle.GetOwner(), $"RecipeIngredients now: {itemBundle.recipeIngredients.Summary()}");
                }
                else if (e.Text.StartsWith(step.failMessage)) {
                    Util.WriteToDebugLog("Got Fail Message: " + step.failMessage);
                    if (step.failDestroySourceAmount > 0) {
                        itemBundle.recipeIngredients.Remove(step.use, step.failDestroySourceAmount);
                    }
                    if (step.failDestroyTargetAmount > 0) {
                        itemBundle.recipeIngredients.Remove(step.on, step.failDestroyTargetAmount);
                    }

                    waitingForItem = "";
                    itemBundle.recipeSteps.RemoveAt(0);
                    isCrafting = false;
                    lastThought = DateTime.UtcNow;

                    Chat.ChatManager.Tell(itemBundle.GetOwner(), $"RecipeIngredients now: {itemBundle.recipeIngredients.Summary()}");
                }

                if (IsRunning && !itemBundle.HasRecipeStepsLeft()) {
                    if (ShouldPause()) {
                        shouldPause = true;
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private bool ShouldPause() {
            var limit = Config.CraftBot.LimitCraftingSessionsToSeconds.Value;
            if (limit > 0 && DateTime.UtcNow - firstThought > TimeSpan.FromSeconds(limit)) {
                return true;
            }

            if (Config.CraftBot.PauseSessionForOtherJobs.Value && (Globals.DoThingsBot.HasTinkeringJobInQueue() || Globals.DoThingsBot.HasBuffingJobInQueue() || Globals.DoThingsBot.HasLostItemsJobInQueue())) {
                return true;
            }

            return false;
        }

        private void DoPause() {
            // check crafting time limit
            var limit = Config.CraftBot.LimitCraftingSessionsToSeconds.Value;
            if (limit > 0 && DateTime.UtcNow - firstThought > TimeSpan.FromSeconds(limit)) {
                var friendlyLimit = Util.GetFriendlyTimeDifference((ulong)(limit * 1000));
                Util.WriteToChat($"Crafting session has hit its time limit of " + friendlyLimit);
                ChatManager.Tell(itemBundle.GetOwner(), $"Your crafting session has hit the session time limit of {friendlyLimit}, please come pick up your items.");
                Bail();
                return;
            }

            // check if we have a tinkering/buffing job to do instead of this crafting
            if (Config.CraftBot.PauseSessionForOtherJobs.Value && (Globals.DoThingsBot.HasTinkeringJobInQueue() || Globals.DoThingsBot.HasBuffingJobInQueue() || Globals.DoThingsBot.HasLostItemsJobInQueue())) {
                ChatManager.Tell(itemBundle.GetOwner(), "I am pausing your crafting job for a higher priority job, I'll message you when your job is finished.");
                itemBundle.Pause();
                machine.ChangeState(new BotFinishState(itemBundle));
                IsRunning = false;
                return;
            }
        }

        private List<string> GetRecipeChoices() {
            var choices = new List<string>();

            var i = 1;
            foreach (var recipe in recipes) {
                choices.Add($"{i}) {recipe.name}");
                i++;
            }

            return choices;
        }

        public void Exit(Machine machine) {
            IsRunning = false;

            ChatManager.RaiseChatCommandEvent -= new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);
            CoreManager.Current.ChatBoxMessage -= Current_ChatBoxMessage;
            CoreManager.Current.EchoFilter.ServerDispatch -= new EventHandler<NetworkMessageEventArgs>(EchoFilter_ServerDispatch);
            CoreManager.Current.WorldFilter.CreateObject -= WorldFilter_CreateObject;
            CoreManager.Current.WorldFilter.ChangeObject -= WorldFilter_ChangeObject;
        }

        private bool needsConfirmation = false;
        private bool needsYesClick = false;
        private DateTime gotCraftingSuccessDialogAt = DateTime.UtcNow;

        void EchoFilter_ServerDispatch(object sender, NetworkMessageEventArgs e) {
            try {

                if (e.Message.Type == 0xF7B0 && (int)e.Message["event"] == 0x0274 && e.Message.Value<int>("type") == 5) {
                    Match match = Globals.PercentConfirmation.Match(e.Message.Value<string>("text"));

                    Util.WriteToChat("I got: " + e.Message.Value<string>("text"));
                    // You have a 33.3% chance of using Black Garnet Salvage (100) on Green Jade Heavy Crossbow.

                    if (match.Success) {
                        double percent;

                        double.TryParse(match.Groups["percent"].Value, out percent);

                        itemBundle.successChanceFullString = match.Groups["msg"].Value;
                        itemBundle.successChance = percent;

                        if (skipConfirmations || (percent >= 100 && Config.CraftBot.SkipMaxSuccessConfirmation.Value)) {
                            needsConfirmation = false;
                            needsYesClick = true;
                            gotCraftingSuccessDialogAt = DateTime.UtcNow;
                        }
                        else {
                            needsConfirmation = true;
                            ChatManager.Tell(itemBundle.GetOwner(), String.Format("I {0}'", itemBundle.successChanceFullString) + ". Respond with 'go', 'always', or 'cancel'.");
                        }
                    }
                    else {
                        Util.WriteToChat($"Did not match: {e.Message.Value<string>("text")}");
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        bool skipConfirmations = false;

        void ChatManager_ChatCommand(object sender, ChatCommandEventArgs e) {
            try {
                lastAction = DateTime.UtcNow;

                switch (e.Command) {
                    case "cancel":
                        ChatManager.Tell(itemBundle.GetOwner(), "I am cancelling this crafting session.");
                        PostMessageTools.ClickNo();
                        Bail();
                        break;

                    default:
                        if (needsConfirmation) {
                            if (e.Command == "go") {
                                needsConfirmation = false;
                                PostMessageTools.ClickYes();
                            }
                            else if (e.Command == "always") {
                                skipConfirmations = true;
                                needsConfirmation = false;
                                PostMessageTools.ClickYes();
                            }
                        }
                        else if (needsChoice) {
                            var validChoices = GetRecipeChoices();
                            int choice = 0;
                            if (Int32.TryParse(e.Command, out choice)) {
                                if (choice >= 1 && choice <= recipes.Count) {
                                    needsChoice = false;
                                    recipe = recipes[choice - 1];
                                    itemBundle.playerData.recipe = recipe.name;
                                    shouldCraft = true;
                                    return;
                                }
                            }

                            Chat.ChatManager.Tell(itemBundle.GetOwner(), $"Invalid choice, please respond with a number from this list: eg '/tell {CoreManager.Current.CharacterFilter.Name}, 1'");
                            Chat.ChatManager.Tell(itemBundle.GetOwner(), String.Join(" ", validChoices.ToArray()));
                        }
                        break;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        DateTime firstThought = DateTime.UtcNow;
        DateTime lastThought = DateTime.UtcNow;
        DateTime finishedAt = DateTime.UtcNow;
        bool didFinish = false;

        public void Think(Machine machine) {
            if (!IsRunning) return;

            if (didFinish && DateTime.UtcNow - finishedAt > TimeSpan.FromSeconds(2)) {
                ChatManager.Tell(itemBundle.GetOwner(), $"Your craft session has finished, I crafted {actionCount} items in {Util.GetFriendlyTimeDifference(DateTime.UtcNow - firstThought)}.");
                Bail();
                return;
            }

            if (didFinish) return;
            
            if (DateTime.UtcNow - lastAction > TimeSpan.FromSeconds(15)) {
                ChatManager.Tell(itemBundle.GetOwner(), "The crafting request timed out.");
                Bail();

                return;
            }

            if (itemsAwaitingIdentity.Count > 0)
                return;

            if (!loadedRecipes)
                FindRecipes();

            if (needsYesClick) {
                if (DateTime.UtcNow - gotCraftingSuccessDialogAt > TimeSpan.FromMilliseconds(100)) {
                    needsYesClick = false;
                    PostMessageTools.ClickYes();
                }
                else {
                    return;
                }
            }

            if (DateTime.UtcNow - lastThought > TimeSpan.FromMilliseconds(300)) {
                lastThought = DateTime.UtcNow;

                if (!shouldCraft) return;

                if (steps == null) {
                    itemBundle.SetRecipe(recipe);
                    steps = recipe.GetSteps(itemBundle.GetIngredientList());
                    var tools = recipe.GetToolsNeeded(itemBundle.GetIngredientList());

                    if (tools.Count > 0) {
                        var toolDisplay = String.Join(", ", tools.ToArray());
                        if (tools.Count > 1) {
                            Chat.ChatManager.Tell(itemBundle.GetOwner(), $"I am missing the tools '{toolDisplay}' in order to craft '{recipe.name}'.  For information on where to get a tool, tell me 'tool {tools[0]}'");
                        }
                        else {
                            var toolLocation = Recipes.GetToolLocation(tools[0]);
                            Chat.ChatManager.Tell(itemBundle.GetOwner(), $"I am missing the tools '{toolDisplay}' in order to craft '{recipe.name}'.  {toolLocation}");
                        }
                        Bail();
                        return;
                    }

                    if (!itemBundle.WasPaused) {
                        Chat.ChatManager.Tell(itemBundle.GetOwner(), "I will craft the following recipe: " + recipe.name);
                    }
                }

                if (isCrafting) return;

                if (CoreManager.Current.Actions.BusyState != 0) {
                    return;
                }

                foreach (var stackThis in stackThese) {
                    var stackThisWo = CoreManager.Current.WorldFilter[stackThis];

                    if (stackThisWo == null) continue;

                    foreach (var stackTo in itemBundle.playerData.itemIds) {
                        var stackToWo = CoreManager.Current.WorldFilter[stackTo];
                        if (stackToWo != null && stackToWo.Name == stackThisWo.Name && stackToWo.Id != stackThisWo.Id && stackToWo.Values(LongValueKey.StackMax) - stackToWo.Values(LongValueKey.StackCount) >= stackThisWo.Values(LongValueKey.StackCount)) {
                            Util.TryStackItemTo(stackToWo, stackThisWo, stackToWo.Values(LongValueKey.Slot, 0));
                            return;
                        }
                    }
                }

                if (shouldPause) {
                    DoPause();
                    return;
                }

                var step = itemBundle.GetCurrentRecipeStep();

                if (step != null) {
                    if (!Recipes.allTools.Contains(step.use) && !itemBundle.recipeIngredients.Contains(step.use)) {
                        Util.WriteToDebugLog($"I ran out of {step.use}.");
                        didFinish = true;
                        finishedAt = DateTime.UtcNow;
                        return;
                    }
                    if (!Recipes.allTools.Contains(step.on) && !itemBundle.recipeIngredients.Contains(step.on)) {
                        Util.WriteToDebugLog($"I ran out of {step.on}.");
                        didFinish = true;
                        finishedAt = DateTime.UtcNow;
                        return;
                    }

                    var useItem = Recipes.allTools.Contains(step.use) ? Util.GetInventoryItemByName(step.use) : step.GetUseItem(itemBundle);
                    var targetItem = Recipes.allTools.Contains(step.on) ? Util.GetInventoryItemByName(step.on) : step.GetTargetItem(itemBundle, useItem);


                    if (useItem == null || targetItem == null) {
                        if (DateTime.UtcNow - lastAction > TimeSpan.FromMilliseconds(10000)) {
                            Chat.ChatManager.Tell(itemBundle.GetOwner(), $"Something went wrong, could not find items to work on... {step.use} {step.on}");
                            Bail();
                        }
                        return;
                    }
                    Util.WriteToDebugLog($"Using {step.use}({useItem.HasIdData}) on {step.on}({targetItem.HasIdData}) to get {step.result}");

                    waitingForItem = step.result;
                    CoreManager.Current.Actions.ApplyItem(useItem.Id, targetItem.Id);
                    isCrafting = true;
                    lastAction = DateTime.UtcNow;
                    actionCount++;
                }
                else {
                    didFinish = true;
                    finishedAt = DateTime.UtcNow;
                }
            }
        }

        private void Bail() {
            shouldCraft = false;
            IsRunning = false;

            if (itemBundle.GetItems().Count > 0) {
                itemBundle.SetCraftMode(CraftMode.GiveBackItems);
                machine.ChangeState(new BotTradingState(itemBundle));
            }
            else {
                machine.ChangeState(new BotFinishState(itemBundle));
            }
        }

        public ItemBundle GetItemBundle() {
            return itemBundle;
        }
    }
}
