using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using DoThingsBot.Buffs;
using System;
using System.Collections.Generic;

namespace DoThingsBot.FSM.States {

    class BotBuffState : IBotState {
        public string Name { get => "BotBuffState"; }
        public List<int> SkippedEnchantments = new List<int>();

        public List<int> buffIds = new List<int>();
        private List<string> CastedEnchantments = new List<string>();
        private string castingSpellName = "";
        private DateTime startedBuffing = DateTime.UtcNow;
        private int fizzleCounter = 0;
        private Dictionary<string, int> burnedComponents = new Dictionary<string, int>();
        private int targetId = 0;
        private bool waitingForTreeStatsData = false;
        private BuffProfile treeStatsProfile = null;
        private bool doneCasting = false;
        private bool hasWarnedAboutShield = false;
        private bool retriedBanes = false;

        private DateTime lastThought = DateTime.UtcNow;
        private DateTime firstThought = DateTime.UtcNow;
        private DateTime lastCasted = DateTime.UtcNow;

        private ItemBundle itemBundle;
        private int castCount = 0;
        private bool readyToCast = true;
        private int profileCount = 0;

        public BotBuffState(ItemBundle items) {
            itemBundle = items;
            itemBundle.playerData.jobType = "buff";
        }

        public void Enter(Machine machine) {
            foreach (var obj in CoreManager.Current.WorldFilter.GetByName(itemBundle.GetOwner())) {
                if (obj.ObjectClass == ObjectClass.Player) {
                    targetId = obj.Id;
                }
            }

            var player = CoreManager.Current.WorldFilter[targetId];

            if (player == null) {
                machine.ChangeState(new BotFinishState(machine.CurrentState.GetItemBundle()));
                return;
            }

            itemBundle.SetOwner(player.Name);

            if (itemBundle.WasPaused) {
                buffIds = itemBundle.playerData.buffIds;
                ChatManager.Tell(itemBundle.GetOwner(), $"I am resuming your buffing session, I still have {buffIds.Count} buff{(buffIds.Count==1?"":"s")} left to cast on you");
            }
            else {
                if (itemBundle.GetBuffProfiles() == "treestats") {
                    waitingForTreeStatsData = true;
                    treeStatsProfile = new BuffProfile(player.Name, true);
                }
                else {
                    var profiles = itemBundle.GetBuffProfiles().Split(' ');
                    List<string> invalidProfiles = new List<string>();
                    List<string> validProfiles = new List<string>();

                    foreach (var profile in profiles) {
                        if (!Buffs.Buffs.IsValidProfile(profile)) {
                            invalidProfiles.Add(profile);
                            continue;
                        }

                        if (!validProfiles.Contains(profile)) {
                            validProfiles.Add(profile);

                            AddProfile(profile);
                        }
                    }

                    if (invalidProfiles.Count > 0) {
                        ChatManager.Tell(itemBundle.GetOwner(), string.Format("The following profiles were invalid: {0}", string.Join(", ", invalidProfiles.ToArray())));
                    }

                    ChatManager.Tell(itemBundle.GetOwner(), string.Format("Casting {0} buffs on you ({1})", buffIds.Count, string.Join(", ", validProfiles.ToArray())));

                    SaveBuffState();
                }
            }

            CoreManager.Current.ChatBoxMessage += new EventHandler<ChatTextInterceptEventArgs>(Current_ChatBoxMessage);
        }

        private void SaveBuffState() {

            var ids = new List<int>();
            foreach (var spellId in buffIds) {
                var spellName = Spells.GetNameFromId(spellId);
                if (CastedEnchantments.Contains(spellName)) continue;
                if (SkippedEnchantments.Contains(spellId)) continue;
                ids.Add(spellId);
            }
            itemBundle.playerData.buffIds = ids;

            itemBundle.SavePlayerData();
        }

        private void AddProfile(BuffProfile buffProfile) {
            bool hasSpells = false;

            foreach (var family in buffProfile.familyIds) {
                var spell = Spells.GetBestKnownSpellByClass(family, false, Config.BuffBot.LimitBuffOtherLevel.Value);

                if (spell == null) {
                    Util.WriteToChat("I dont know any spell for: " + family);
                    continue;
                }

                if (!buffIds.Contains(spell.Id)) {
                    buffIds.Add(spell.Id);
                    hasSpells = true;
                }
            }

            if (hasSpells) profileCount++;
        }

        private void AddProfile(string profile) {
            AddProfile(Buffs.Buffs.GetProfile(profile));
        }

        public void Exit(Machine machine) {
            CoreManager.Current.ChatBoxMessage -= new EventHandler<ChatTextInterceptEventArgs>(Current_ChatBoxMessage);
            CoreManager.Current.Actions.FaceHeading(Config.Bot.DefaultHeading.Value, true);
        }

        void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e) {
            try {
                if (Util.IsChat(e.Text, Util.ChatFlags.PlayerTellsYou)) {
                    string playerName = Util.GetSourceOfChat(e.Text);
                    string command = Util.GetMessageFromChat(e.Text);
                    if (playerName == itemBundle.GetOwner() && command == "cancel") {
                        ChatManager.Tell(playerName, "Ok, cancelling your current buff queue.");
                        doneCasting = true;
                        return;
                    }
                }

                if (e.Text.StartsWith(String.Format("You cast {0}", castingSpellName))) {
                    Globals.Stats.AddPlayerBuffsCasted(itemBundle.GetOwner(), 1);
                    CastedEnchantments.Add(castingSpellName);
                    castingSpellName = "";
                    castCount = 0;
                    readyToCast = true;
                    lastCasted = DateTime.UtcNow - TimeSpan.FromMinutes(1);
                    SaveBuffState();
                }
                else if (e.Text.StartsWith("Your spell fizzled.")) {
                    readyToCast = true;
                    fizzleCounter++;
                    Globals.Stats.AddPlayerFizzle(itemBundle.GetOwner());
                }
                else if (e.Text.StartsWith("The spell consumed the following components: ")) {
                    var components = e.Text.Replace("The spell consumed the following components: ", "").Trim();

                    foreach (var component in components.Split(',')) {
                        var name = component.Trim();
                        
                        Globals.Stats.AddPlayerBurnedComponent(itemBundle.GetOwner(), name, 1);

                        if (burnedComponents.ContainsKey(name)) {
                            burnedComponents[name]++;
                        }
                        else {
                            burnedComponents.Add(name, 1);
                        }
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public void Think(Machine machine) {
            if (DateTime.UtcNow - lastThought > TimeSpan.FromMilliseconds(300)) {
                lastThought = DateTime.UtcNow;

                if (waitingForTreeStatsData && treeStatsProfile != null) {
                    if (treeStatsProfile.IsLoaded()) {
                        waitingForTreeStatsData = false;

                        if (!treeStatsProfile.IsValid()) {
                            doneCasting = true;
                            ChatManager.Tell(itemBundle.GetOwner(), "I was unable to load your profile from treestats. Try telling me 'profiles' to see what I can buff you with.");
                            return;
                        }

                        AddProfile(treeStatsProfile);
                        var lastUpdated = "unknown";
                        if (treeStatsProfile.treeStatsProfile != null) {
                            lastUpdated = treeStatsProfile.treeStatsProfile.updated_at;
                        }
                        ChatManager.Tell(itemBundle.GetOwner(), string.Format("Casting {0} buffs on you, based on your treestats character profile (last updated {1}).", buffIds.Count, lastUpdated));

                        SaveBuffState();
                    }
                    else {
                        return;
                    }
                }
                
                if (doneCasting == false) {
                    // enter magic combat state before casting buffs
                    if (!Util.EnsureCombatState(CombatState.Magic)) return;

                    // make sure we have enough stamina
                    if (!Spells.EnsureEnoughStamina()) return;

                    // make sure we have enough mana
                    if (!Spells.EnsureEnoughMana()) return;

                    if (!CoreManager.Current.Actions.IsValidObject(targetId)) {
                        ChatManager.Tell(itemBundle.GetOwner(), "You moved too far away, cancelling your buffs.");
                        doneCasting = true;
                        return;
                    }

                    var player = CoreManager.Current.WorldFilter[targetId];

                    if (player == null || Util.GetDistanceFromPlayer(player) > 30) {
                        ChatManager.Tell(itemBundle.GetOwner(), "You moved too far away, cancelling your buffs.");
                        doneCasting = true;
                        return;
                    }

                    var hasShield = false;

                    if (!Config.BuffBot.AlwaysEnableBanes.Value) {
                        // find shield?
                        foreach (var item in CoreManager.Current.WorldFilter.GetByContainer(targetId)) {
                            if (item.Name.Contains("Buckler") || item.Name.Contains("Shield")) {
                                hasShield = true;
                                break;
                            }
                        }
                    }

                    // cast next needed buff
                    foreach (var spellId in buffIds) {
                        var spellName = Spells.GetNameFromId(spellId);
                        if (CastedEnchantments.Contains(spellName)) continue;
                        if (SkippedEnchantments.Contains(spellId)) continue;

                        if (castCount > 15) {
                            castCount = 0;
                            CastedEnchantments.Add(spellName);
                            return;
                        }

                        castingSpellName = spellName;
                        
                        if (!Config.BuffBot.AlwaysEnableBanes.Value && spellName.Contains(" Bane")) {
                            if (!hasShield && !hasWarnedAboutShield) {
                                hasWarnedAboutShield = true;
                                ChatManager.Tell(itemBundle.GetOwner(), "I cannot cast banes on you since you do not have a shield equipped.");
                            }

                            if (!hasShield) {
                                SkippedEnchantments.Add(spellId);
                                continue;
                            }
                        }

                        if (DateTime.UtcNow - lastCasted > TimeSpan.FromMilliseconds(1000)) {
                            lastCasted = DateTime.UtcNow;
                            CoreManager.Current.Actions.CastSpell(spellId, targetId);
                            castCount++;
                        }
                        return;
                    }

                    if (hasWarnedAboutShield && !retriedBanes) {
                        retriedBanes = true;
                        SkippedEnchantments.Clear();
                        return;
                    }

                    doneCasting = true;
                }
                else {
                    // make sure we are in peace mode
                    if (!Util.EnsureCombatState(CombatState.Peace)) return;

                    itemBundle.SetEquipMode(EquipMode.Idle);

                    var secondsTaken = (int)Math.Round((DateTime.UtcNow - startedBuffing).TotalSeconds);
                    var response = string.Format("This session took {0} to cast {1} buff{2}", Util.GetFriendlyTimeDifference((ulong)secondsTaken), CastedEnchantments.Count, CastedEnchantments.Count==1?"":"s");

                    Globals.Stats.AddPlayerTimeSpentBuffing(itemBundle.GetOwner(), secondsTaken);
                    Globals.Stats.AddPlayerProfilesCasted(itemBundle.GetOwner(), profileCount);

                    if (fizzleCounter > 0 || burnedComponents.Keys.Count > 0) {
                        response += ",";
                    }
                    else {
                        response += ".";
                    }

                    if (fizzleCounter > 0) {
                        var end = ".";
                        if (burnedComponents.Keys.Count > 0) {
                            end = " and ";
                        }
                        response += string.Format(" I fizzled {0} time{1}{2}", fizzleCounter, fizzleCounter== 1?"":"s", end);
                    }

                    if (burnedComponents.Keys.Count > 0) {
                        var components = new List<string>();

                        foreach (var ckey in burnedComponents.Keys) {
                            components.Add(ckey + " *" + burnedComponents[ckey]);
                        }

                        response += string.Format("{1}burned: {0}.", string.Join(", ", components.ToArray()), fizzleCounter > 0 ? " and " : " I ");
                    }

                    var timeSpent = string.Format("{0}", Util.GetFriendlyTimeDifference((ulong)itemBundle.playerData.totalTimeSpentBuffing));

                    // if (config.ShowPersonalBuffStatsAfterJob)
                    response += string.Format(" Your overall stats: Buffs: {0}, Time: {1}", itemBundle.playerData.totalBuffsCast, timeSpent);

                    if (CastedEnchantments.Count > 0) {
                        ChatManager.Tell(itemBundle.GetOwner(), response);
                    }

                    machine.ChangeState(new BotFinishState(itemBundle));
                }
            }
        }

        public ItemBundle GetItemBundle() {
            return itemBundle;
        }
    }
}
