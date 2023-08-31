using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using DoThingsBot.Views.Pages;
using VirindiViewService;
using VirindiViewService.Controls;

namespace DoThingsBot.Views {
    public class StatsView : IDisposable {
        public readonly VirindiViewService.ViewProperties properties;
        public readonly VirindiViewService.ControlGroup controls;
        public readonly VirindiViewService.HudView view;

        private HudFixedLayout SessionStatsLayout { get; set; }
        private HudFixedLayout GlobalStatsLayout { get; set; }
        private HudFixedLayout CharacterStatsLayout { get; set; }
        private HudButton StatsEchoGlobalStats { get; set; }
        private HudButton StatsEchoSessionStats { get; set; }
        private HudButton StatsEchoCharacterStats { get; set; }
        private HudCombo SelectCharacter { get; set; }
        public HudTabView StatTabs { get; set; }

        private List<HudControl> statControls = new List<HudControl>();

        public int viewWidth = 0;
        public int halfWidth = 0;
        public int thirdWidth = 0;
        public int lineHeight = 16;

        public int padding = 8;
        public int lineWidth = 0;

        private Dictionary<int, int> columnYOffsets = new Dictionary<int, int>();
        private Dictionary<int, int> columnXOffsets = new Dictionary<int, int>();
        private Dictionary<int, List<int>> columnBlockHeights = new Dictionary<int, List<int>>();
        private Dictionary<int, List<int>> columnBlockHeaders = new Dictionary<int, List<int>>();

        private Bitmap backgroundBmp = null;
        private bool echoToChat = false;
        private bool backgroundNeedsRedraw = true;

        private Dictionary<string, HudList> statLists = new Dictionary<string, HudList>();
        private Dictionary<string, List<HudStaticText>> statBlockChildren = new Dictionary<string, List<HudStaticText>>();


        private string currentBlockName = "";
        private string currentBlockHeaderKey = "";
        private int lastTab = -1;
        private string selectedCharacter = "";

        public StatsView() {
            try {
                // Create the view
                VirindiViewService.XMLParsers.Decal3XMLParser parser = new VirindiViewService.XMLParsers.Decal3XMLParser();
                parser.ParseFromResource("DoThingsBot.Views.statsView.xml", out properties, out controls);

                // Display the view
                view = new VirindiViewService.HudView(properties, controls);
                //view.LoadUserSettings();
                //view.Visible = true;

                view.ThemeChanged += View_ThemeChanged;
                view.Resize += View_Resize;

                StatTabs = (HudTabView)view["UIStatsMainTabs"];
                StatTabs.OpenTabChange += Tabs_OpenTabChange;

                StatsEchoGlobalStats = (HudButton)view["UIStatsEchoGlobalStats"];
                StatsEchoGlobalStats.Hit += StatsEchoGlobalStats_Hit;
                StatsEchoSessionStats = (HudButton)view["UIStatsEchoSessionStats"];
                StatsEchoSessionStats.Hit += StatsEchoGlobalStats_Hit;
                StatsEchoCharacterStats = (HudButton)view["UIStatsEchoCharacterStats"];
                StatsEchoCharacterStats.Hit += StatsEchoGlobalStats_Hit;

                GlobalStatsLayout = (HudFixedLayout)view["UIStatsGlobalStatsLayout"];
                SessionStatsLayout = (HudFixedLayout)view["UIStatsSessionStatsLayout"];
                CharacterStatsLayout = (HudFixedLayout)view["UIStatsCharacterStatsLayout"];

                SelectCharacter = (HudCombo)view["UIStatsSelectCharacter"];

                SelectCharacter.Change += SelectCharacter_Change;

                //DrawCharacterSelect();
                //Redraw();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        internal void ShowCharacterStats(string playerName) {
            selectedCharacter = playerName;
            DrawCharacterSelect();
            StatTabs.CurrentTab = 2;
        }

        internal void ShowGlobalStats() {
            StatTabs.CurrentTab = 1;
        }

        internal void ShowSessionStats() {
            StatTabs.CurrentTab = 0;
        }

        private void SelectCharacter_Change(object sender, EventArgs e) {
            HudStaticText c = (HudStaticText)(SelectCharacter[SelectCharacter.Current]);
            selectedCharacter = c.Text;
            RemoveControls();
            DrawCharacterSelect();
            Redraw();
        }

        private void View_Resize(object sender, EventArgs e) {
            try {
                Redraw();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void StatsEchoGlobalStats_Hit(object sender, EventArgs e) {
            echoToChat = true;
            Redraw();
            echoToChat = false;
        }

        private void Tabs_OpenTabChange(object sender, EventArgs e) {
            try {
                RemoveControls();

                if (StatTabs.CurrentTab == 2) DrawCharacterSelect();

                Redraw();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void View_ThemeChanged(object sender, EventArgs e) {
            try {
                backgroundNeedsRedraw = true;
                Redraw();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private HudFixedLayout GetCurrentLayout() {
            HudTabView tabs = (HudTabView)view["UIStatsMainTabs"];
            switch (tabs.CurrentTab) {
                case 0:
                    return SessionStatsLayout;
                case 1:
                    return GlobalStatsLayout;
                case 2:
                    return CharacterStatsLayout;
            }

            return null;
        }

        private void Redraw() {
            if (view == null || !view.Visible) return;
            
            ResetControlOffsets();

            HudTabView tabs = (HudTabView)view["UIStatsMainTabs"];

            if (lastTab != tabs.CurrentTab) {
                lastTab = tabs.CurrentTab;
                backgroundNeedsRedraw = true;
            }

            switch (tabs.CurrentTab) {
                case 0:
                    if (echoToChat) Util.WriteToChat("Session Stats");
                    DrawSessionStats();
                    break;
                case 1:
                    if (echoToChat) Util.WriteToChat("Global Stats");
                    DrawGlobalStats();
                    break;
                case 2:
                    if (echoToChat) Util.WriteToChat("Character Stats: " + selectedCharacter);
                    DrawCharacterStats();
                    break;
            }

            if (backgroundNeedsRedraw) {
                backgroundNeedsRedraw = false;
                var bg = GetBackground();
                if (bg != null) {
                    GetCurrentLayout().Image = bg;
                }
            }

            view.MainControl.Invalidate();
        }

        private void DrawCharacterSelect() {
            string[] files = Directory.GetFiles(Util.GetPlayerDataDirectory(), "*.json", SearchOption.TopDirectoryOnly);

            SelectCharacter.Clear();
            SelectCharacter.AddItem("", "");
            var index = 1;
            var selected = 0;
            foreach (var file in files) {
                var parts = file.Split('\\');
                var name = parts[parts.Length - 1].Replace(".json", "");
                SelectCharacter.AddItem(name, name);
                if (name == selectedCharacter) {
                    selected = index;
                }
                index++;
            }
            
            SelectCharacter.Current = selected;
        }

        private void RemoveControls() {
            foreach (var control in statControls) {
                control.Visible = false;
                control.Dispose();
            }

            statControls.Clear();
            statLists.Clear();
            statBlockChildren.Clear();
            backgroundNeedsRedraw = true;
        }

        private void ResetControlOffsets() {
            viewWidth = view.Width;
            halfWidth = viewWidth / 2;
            thirdWidth = viewWidth / 3;
            lineWidth = thirdWidth - (int)(padding * 2);
            
            columnXOffsets.Clear();
            columnYOffsets.Clear();
            columnBlockHeights.Clear();
            columnBlockHeaders.Clear();

            columnBlockHeights.Add(0, new List<int>());
            columnBlockHeights.Add(1, new List<int>());
            columnBlockHeights.Add(2, new List<int>());

            columnBlockHeaders.Add(0, new List<int>());
            columnBlockHeaders.Add(1, new List<int>());
            columnBlockHeaders.Add(2, new List<int>());

            columnYOffsets.Add(0, 0);
            columnYOffsets.Add(1, 0);
            columnYOffsets.Add(2, 0);

            columnXOffsets.Add(0, padding);
            columnXOffsets.Add(1, thirdWidth + padding);
            columnXOffsets.Add(2, (thirdWidth * 2) + padding);
        }

        public HudStaticText DrawTextInColumn(int column, string text, int lineWidth, int lineHeight, int xOffset=0, VirindiViewService.WriteTextFormats alignment = VirindiViewService.WriteTextFormats.None, bool skipYIncrememnt=false, string key="") {
            HudStaticText textControl = new HudStaticText();
            Rectangle region = new Rectangle(columnXOffsets[column], columnYOffsets[column], lineWidth, lineHeight);

            if (!string.IsNullOrEmpty(key)) {
                textControl.InternalName = key;
            }

            textControl.Text = text;
            textControl.TextAlignment = alignment;
            GetCurrentLayout().AddControl(textControl, region);
            statControls.Add(textControl);

            if (!skipYIncrememnt) {
                columnYOffsets[column] += lineHeight;
            }

            if (echoToChat && !skipYIncrememnt && alignment != WriteTextFormats.Right) {
                Util.WriteToChat(text, true);
            }

            return textControl;
        }

        public void DrawBlockHeader(int column, string text, string extra="", string key="") {
            currentBlockName = text;
            var viewKey = string.Format("{0}_{1}", column, string.IsNullOrEmpty(key) ? text : key);
            HudStaticText control = null;
            var headerText = text + (string.IsNullOrEmpty(extra) ? "" : string.Format(" {0}", extra));

            currentBlockHeaderKey = viewKey;

            try {
                control = (HudStaticText)view[viewKey];
            }
            catch (Exception ex) { }

            var headerHeight = lineHeight;

            columnBlockHeaders[column].Add(columnYOffsets[column]);

            if (control == null) {
                var textControl = DrawTextInColumn(column, headerText, lineWidth, headerHeight, 0, VirindiViewService.WriteTextFormats.Center, false, viewKey);

                textControl.Hit += (s, e) => {
                    Util.WriteToChat(string.Format("{0} {1}", text, extra), true);
                    var listKey = "l_" + viewKey;

                    if (statLists.ContainsKey(listKey)) {
                        for (var i = 0; i < statLists[listKey].RowCount; i++) {
                            HudList.HudListRowAccessor row = statLists[listKey][i];
                            Util.WriteToChat(string.Format("  {0} {1}", ((HudStaticText)row[0]).Text, ((HudStaticText)row[1]).Text), true);
                        }
                    }
                    else if (statBlockChildren.ContainsKey(viewKey)) {
                        foreach (var ctl in statBlockChildren[viewKey]) {
                            ((HudStaticText)ctl).MouseDown(new Point(3, 3));
                        }
                    }
                };
            }
            else {
                columnYOffsets[column] += headerHeight;
                control.Text = headerText;
            }

            if (echoToChat) {
                Util.WriteToChat(headerText, true);
            }
        }

        public HudStaticText DrawKVPair(int column, string key, string value, string extra="") {
            var viewKey = string.Format("kv_{0}_{1}", column, key);
            HudStaticText text = null;

            try {
                text = (HudStaticText)view[viewKey];
            }
            catch (Exception ex) { }

            var rowHeight = lineHeight + 2;

            if (text == null) {
                DrawTextInColumn(column, key + ":", lineWidth, rowHeight, 0, VirindiViewService.WriteTextFormats.None, true);
                text = DrawTextInColumn(column, value, lineWidth, rowHeight, 0, VirindiViewService.WriteTextFormats.Right, false, viewKey);

                text.MouseEvent += (s, e) => {
                    if (e.EventType != ControlMouseEventArgs.MouseEventType.MouseDown) return;

                    if (extra.Contains("\n")) {
                        Util.WriteToChat(string.Format("  {0}: {1}\n{2}", key, text.Text, extra), true);
                    }
                    else {
                        Util.WriteToChat(string.Format("  {0}: {1} {2}", key, text.Text, extra), true);
                    }
                };

                if (!statBlockChildren.ContainsKey(currentBlockHeaderKey)) {
                    statBlockChildren.Add(currentBlockHeaderKey, new List<HudStaticText>());
                }

                statBlockChildren[currentBlockHeaderKey].Add(text);
            }
            else {
                columnYOffsets[column] += rowHeight;
                text.Text = value;
            }

            if (echoToChat) {
                if (extra.Contains("\n")) {
                    Util.WriteToChat(string.Format("  {0}: {1}\n{2}", key, text.Text, extra), true);
                }
                else {
                    Util.WriteToChat(string.Format("  {0}: {1} {2}", key, text.Text, extra), true);
                }
            }

            return text;
        }

        private void DrawList(int column, Dictionary<string, int> stats, int height, double keyColWidth = 0.6) {
            Dictionary<string, string> data = new Dictionary<string, string>();
            foreach (var key in stats.Keys) {
                data[key] = stats[key].ToString();
            }
            DrawList(column, data, height, keyColWidth);
        }

        private void DrawList(int column, Dictionary<string, string> stats, int height, double keyColWidth = 0.6) {
            HudList list = null;
            var viewKey = string.Format("l_{0}_{1}", column, currentBlockName);

            var colWidth = (lineWidth - 15);
            keyColWidth = lineWidth * keyColWidth;
            var valueColWidth = lineWidth - keyColWidth;

            try {
                list = (HudList)view[viewKey];
            }
            catch (Exception ex) { }

            if (list == null) {
                list = new HudList();
                list.InternalName = viewKey;
                statControls.Add(list);
                Rectangle region = new Rectangle(columnXOffsets[column] - (padding / 2) + 1, columnYOffsets[column], thirdWidth - padding - 2, height - 1);

                
                list.Click += (s, r, c) => {
                    HudList.HudListRowAccessor row = list[r];
                    Util.WriteToChat(string.Format("  {0} {1}", ((HudStaticText)row[0]).Text, ((HudStaticText)row[1]).Text), true);
                };

                GetCurrentLayout().AddControl(list, region);
            }

            if (statLists.ContainsKey(viewKey)) {
                statLists[viewKey] = list;
            }
            else {
                statLists.Add(viewKey, list);
            }

            columnYOffsets[column] += height;

            if (list.ColumnCount == 0) {
                list.AddColumn(typeof(HudStaticText), (int)keyColWidth, "key");
                list.AddColumn(typeof(HudStaticText), (int)valueColWidth, "value");
            }

            var existingStatKeys = new Dictionary<string, int>();

            for (var i = 0; i < list.RowCount; i++) {
                HudList.HudListRowAccessor row = list[i];

                existingStatKeys.Add(((HudStaticText)row[0]).Text.Replace(":", ""), i);
            }

            var sortedKeys = new List<string>();

            foreach (var key in stats.Keys) {
                sortedKeys.Add(key);
            }

            sortedKeys.Sort((a, b) => {
                int av = 0;
                int bv = 0;

                Int32.TryParse(stats[a].Replace("%", "").Replace(",", "").Replace("p", ""), out av);
                Int32.TryParse(stats[b].Replace("%", "").Replace(",", "").Replace("p", ""), out bv);

                if (av == bv) return a.CompareTo(b);

                return bv.CompareTo(av);
            });

            foreach (var key in sortedKeys) {
                if (existingStatKeys.ContainsKey(key)) {
                    HudList.HudListRowAccessor row = list[existingStatKeys[key]];

                    if (row != null) {
                        ((HudStaticText)row[1]).Text = stats[key];
                    }
                }
                else {
                    HudList.HudListRowAccessor row = list.AddRow();

                    ((HudStaticText)row[0]).Text = key + ":";
                    ((HudStaticText)row[1]).Text = stats[key];
                    ((HudStaticText)row[1]).TextAlignment = WriteTextFormats.Right;
                }

                if (echoToChat) {
                    Util.WriteToChat(string.Format("  {0}: {1}", key, stats[key]), true);
                }
            }
        }

        public void StartColumnBlock(int column, string headerText, string extra="", string key="") {
            columnYOffsets[column] += padding;
            columnBlockHeights[column].Add(columnYOffsets[column]);

            DrawBlockHeader(column, headerText, extra, key);
        }

        public void StopColumnBlock(int column) {
            if (column == 0) {
                columnYOffsets[column] += 1;
            }

            var value = columnBlockHeights[column][columnBlockHeights[column].Count - 1];
            columnBlockHeights[column][columnBlockHeights[column].Count - 1] = columnYOffsets[column] - value;

        }

        public void DrawSessionStats() {
            try {
                var profit = string.Format("{0:n0}p", (long)(Globals.Stats.operatingRevenue - Globals.Stats.operatingCost));
                var timeSpent = Util.GetFriendlyTimeDifference(Globals.Stats.GetTotalTimeSpentBuffing(), true);
                var timeSpentSelf = Util.GetFriendlyTimeDifference(Globals.Stats.timeSpentSelfBuffing, true);
                var totalBuffs = Globals.Stats.GetTotalPlayerBuffsCast() + Globals.Stats.selfBuffsCasted;

                // First Col

                StartColumnBlock(0, "Bot");
                var uptimeRow = DrawKVPair(0, "Uptime", Globals.Stats.GetFriendlyUptime());
                DrawKVPair(0, "Cost", string.Format("{0:n0}p", Globals.Stats.operatingCost));
                DrawKVPair(0, "Revenue", string.Format("{0:n0}p", Globals.Stats.operatingRevenue));
                DrawKVPair(0, "Profit", profit);
                DrawKVPair(0, "Unique Users", Globals.Stats.GetTotalPlayersServed().ToString());
                StopColumnBlock(0);

                StartColumnBlock(0, "Buffs", string.Format("({0})", totalBuffs));
                DrawKVPair(0, "Buffs Other", Globals.Stats.GetTotalPlayerBuffsCast().ToString());
                DrawKVPair(0, "Buffs Self", Globals.Stats.selfBuffsCasted.ToString());
                DrawKVPair(0, "Buff Profiles", Globals.Stats.GetTotalPlayerBuffProfilesCast().ToString());
                DrawKVPair(0, "Fizzles", Globals.Stats.fizzles.ToString());
                DrawKVPair(0, "Other Time", timeSpent);
                DrawKVPair(0, "Self Time", timeSpentSelf);
                StopColumnBlock(0);

                StartColumnBlock(0, "Tinkering");
                DrawKVPair(0, "Lowest Success", Globals.Stats.lowestSuccessfulTinkerChance + "%", Globals.Stats.lowestSuccessfulTinkerChanceDescription);
                DrawKVPair(0, "Highest Failure", Globals.Stats.highestFailedTinkerChance + "%", Globals.Stats.highestFailedTinkerChanceDescription);
                DrawKVPair(0, "Highest Imbued Streak", Globals.Stats.highestPlayerImbueLandedStreak.ToString(), "by " + Globals.Stats.highestPlayerImbueLandedStreakName);
                DrawKVPair(0, "Highest Failed Streak", Globals.Stats.highestPlayerImbueFailedStreak.ToString(), "by " + Globals.Stats.highestPlayerImbueFailedStreakName);
                StopColumnBlock(0);

                // Second Col

                StartColumnBlock(1, "Commands Issued", string.Format("({0})", Globals.Stats.GetTotalCommandsIssued()));
                DrawList(1, Globals.Stats.GetCommandsIssued(), 91, 0.6);
                StopColumnBlock(1);

                StartColumnBlock(1, "Imbues", string.Format("({0}) {1}", Globals.Stats.GetTotalImbueAttempts(), Globals.Stats.GetOverallImbuePercentage().ToString() + "%"));
                DrawList(1, Globals.Stats.GetImbueTypeStatsList(), 91, 0.6);
                StopColumnBlock(1);

                StartColumnBlock(1, "Salvage Used", string.Format("({0})", Globals.Stats.GetTotalSalvageBagsUsed()));
                DrawList(1, Globals.Stats.GetSalvageBagsUsed(), 91, 0.6);
                StopColumnBlock(1);

                // Third Col
                StartColumnBlock(2, "Portals Summoned", string.Format("({0})", Globals.Stats.GetTotalPortalsSummoned()));
                DrawList(2, Globals.Stats.GetPortalsSummoned(), 91, 0.6);
                StopColumnBlock(2);

                StartColumnBlock(2, "Donations", string.Format("({0})", Globals.Stats.GetTotalDonations()));
                DrawList(2, Globals.Stats.GetDonations(), 91, 0.65);
                StopColumnBlock(2);

                StartColumnBlock(2, "Components Burned", string.Format("({0})", Globals.Stats.GetTotalBurnedComponents()));
                DrawList(2, Globals.Stats.burnedComponents, 91, 0.6);
                StopColumnBlock(2);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public void DrawGlobalStats() {
            try {
                var profit = string.Format("{0:n0}p", (long)(Globals.Stats.globalStats.operatingRevenue - Globals.Stats.globalStats.operatingCost));
                var timeSpent = Util.GetFriendlyTimeDifference(Globals.Stats.globalStats.timeSpentBuffing, true);
                var timeSpentSelf = Util.GetFriendlyTimeDifference(Globals.Stats.globalStats.timeSpentSelfBuffing, true);
                var totalBuffs = Globals.Stats.globalStats.playerBuffsCasted + Globals.Stats.globalStats.selfBuffsCasted;

                // First Col

                StartColumnBlock(0, "Bot");
                var uptimeRow = DrawKVPair(0, "Uptime", Globals.Stats.globalStats.GetFriendlyUptime());
                DrawKVPair(0, "Cost", string.Format("{0:n0}p", Globals.Stats.globalStats.operatingCost));
                DrawKVPair(0, "Revenue", string.Format("{0:n0}p", Globals.Stats.globalStats.operatingRevenue));
                DrawKVPair(0, "Profit", profit);
                DrawKVPair(0, "Unique Users", Globals.Stats.globalStats.GetTotalPlayersServed().ToString());
                StopColumnBlock(0);

                StartColumnBlock(0, "Buffs", string.Format("({0})", totalBuffs));
                DrawKVPair(0, "Buffs Other", Globals.Stats.globalStats.playerBuffsCasted.ToString());
                DrawKVPair(0, "Buffs Self", Globals.Stats.globalStats.selfBuffsCasted.ToString());
                DrawKVPair(0, "Buff Profiles", Globals.Stats.globalStats.buffProfilesCasted.ToString());
                DrawKVPair(0, "Fizzles", Globals.Stats.globalStats.fizzles.ToString());
                DrawKVPair(0, "Other Time", timeSpent);
                DrawKVPair(0, "Self Time", timeSpentSelf);
                StopColumnBlock(0);

                StartColumnBlock(0, "Tinkering");
                DrawKVPair(0, "Lowest Success", Globals.Stats.globalStats.lowestSuccessfulTinkerChance + "%", Globals.Stats.globalStats.lowestSuccessfulTinkerChanceDescription);
                DrawKVPair(0, "Highest Failure", Globals.Stats.globalStats.highestFailedTinkerChance + "%", Globals.Stats.globalStats.highestFailedTinkerChanceDescription);
                DrawKVPair(0, "Highest Imbued Streak", Globals.Stats.globalStats.highestPlayerImbueLandedStreak.ToString(), "by " + Globals.Stats.globalStats.highestPlayerImbueLandedStreakName);
                DrawKVPair(0, "Highest Failed Streak", Globals.Stats.globalStats.highestPlayerImbueFailedStreak.ToString(),  "by " + Globals.Stats.globalStats.highestPlayerImbueFailedStreakName);
                StopColumnBlock(0);

                StartColumnBlock(1, "Commands Issued", string.Format("({0})", Globals.Stats.globalStats.GetTotalCommandsIssued()));
                DrawList(1, Globals.Stats.globalStats.commandsIssued, 91, 0.6);
                StopColumnBlock(1);

                StartColumnBlock(1, "Imbues", string.Format("({0}) {1}", Globals.Stats.globalStats.GetTotalImbueAttempts(), Globals.Stats.globalStats.GetOverallImbuePercentage().ToString() + "%"));
                DrawList(1, Globals.Stats.globalStats.GetImbueTypeStatsList(), 91, 0.6);
                StopColumnBlock(1);

                StartColumnBlock(1, "Salvage Used", string.Format("({0})", Globals.Stats.globalStats.GetTotalSalvageBagsUsed()));
                DrawList(1, Globals.Stats.globalStats.salvageBagsUsed, 91, 0.6);
                StopColumnBlock(1);

                // Third Col
                StartColumnBlock(2, "Portals Summoned", string.Format("({0})", Globals.Stats.globalStats.GetTotalPortalsSummoned()));
                DrawList(2, Globals.Stats.globalStats.portalsSummoned, 91, 0.6);
                StopColumnBlock(2);

                StartColumnBlock(2, "Donations", string.Format("({0})", Globals.Stats.globalStats.GetTotalDonations()));
                DrawList(2, Globals.Stats.globalStats.donations, 91, 0.65);
                StopColumnBlock(2);

                StartColumnBlock(2, "Components Burned", string.Format("({0})", Globals.Stats.globalStats.GetTotalBurnedComponents()));
                DrawList(2, Globals.Stats.globalStats.burnedComponents, 91, 0.6);
                StopColumnBlock(2);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public void DrawCharacterStats() {
            try {
                var playerData = Globals.Stats.GetItemBundle(selectedCharacter).playerData;

                var timeSpent = Util.GetFriendlyTimeDifference(playerData.totalTimeSpentBuffing, true);

                // First Col

                StartColumnBlock(0, selectedCharacter, "", "player_name");
                DrawKVPair(0, "Balance", string.Format("{0:n0}p", playerData.balance));
                StopColumnBlock(0);

                StartColumnBlock(0, "Buffs");
                DrawKVPair(0, "Buffs", playerData.totalBuffsCast.ToString());
                DrawKVPair(0, "Profiles", playerData.totalBuffProfilesCast.ToString());
                DrawKVPair(0, "Fizzles", playerData.fizzles.ToString());
                DrawKVPair(0, "Time Spent", timeSpent);
                StopColumnBlock(0);

                StartColumnBlock(0, "Tinkering");
                DrawKVPair(0, "Lowest Success", playerData.lowestSuccessfulTinkerChance + "%", playerData.lowestSuccessfulTinkerChanceDescription);
                DrawKVPair(0, "Highest Failure", playerData.highestFailedTinkerChance + "%", playerData.highestFailedTinkerChanceDescription);
                DrawKVPair(0, "Current Imbued Streak", playerData.currentImbueLandedStreak.ToString());
                DrawKVPair(0, "Current Failed Streak", playerData.currentImbueFailedStreak.ToString());
                DrawKVPair(0, "Highest Imbued Streak", playerData.highestImbueLandedStreak.ToString());
                DrawKVPair(0, "Highest Failed Streak", playerData.highestImbueFailedStreak.ToString());
                StopColumnBlock(0);

                StartColumnBlock(1, "Commands Issued", string.Format("({0})", playerData.GetTotalCommandsIssued()));
                DrawList(1, playerData.commandsIssued, 91, 0.6);
                StopColumnBlock(1);

                StartColumnBlock(1, "Imbues", string.Format("({0}) {1}", playerData.GetTotalImbueAttempts(), playerData.GetOverallImbuePercentage().ToString() + "%"));
                DrawList(1, playerData.GetImbueTypeStatsList(), 91, 0.6);
                StopColumnBlock(1);

                StartColumnBlock(1, "Salvage Used", string.Format("({0})", playerData.GetTotalSalvageBagsUsed()));
                DrawList(1, playerData.salvageBagsUsed, 91, 0.6);
                StopColumnBlock(1);

                // Third Col
                StartColumnBlock(2, "Portals Summoned", string.Format("({0})", playerData.GetTotalPortalsSummoned()));
                DrawList(2, playerData.portalsSummoned, 91, 0.6);
                StopColumnBlock(2);

                StartColumnBlock(2, "Donations", string.Format("({0})", playerData.GetTotalDonations()));
                DrawList(2, playerData.donations, 91, 0.65);
                StopColumnBlock(2);

                StartColumnBlock(2, "Components Burned", string.Format("({0})", playerData.GetTotalBurnedComponents()));
                DrawList(2, playerData.burnedComponents, 91, 0.6);
                StopColumnBlock(2);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private ACImage GetBackground() {
            using (var bmp = new Bitmap(viewWidth, 350)) {
                using (var gfx = Graphics.FromImage(bmp)) {
                    using (var pen = new Pen(view.Theme.GetColor("ButtonText"))) {
                        foreach (var column in columnBlockHeights.Keys) {
                            var yOffset = 3;
                            foreach (var blockHeight in columnBlockHeights[column]) {
                                gfx.DrawRectangle(pen, (column * thirdWidth) + (padding/2), yOffset + (padding / 2), thirdWidth-padding-1, blockHeight);
                                yOffset += blockHeight + padding;
                            }

                            foreach (var blockHeader in columnBlockHeaders[column]) {
                                gfx.DrawRectangle(pen, (column * thirdWidth) + (padding / 2), blockHeader-1, thirdWidth - padding - 1, lineHeight);
                            }
                        }

                        if (backgroundBmp != null) backgroundBmp.Dispose();
                        backgroundBmp = (Bitmap)bmp.Clone();
                    }
                }
            }

            if (backgroundBmp != null) {
                return new ACImage(backgroundBmp);
            }

            return null;
        }

        private DateTime lastRedraw = DateTime.UtcNow;

        public void Think() {
            if (DateTime.UtcNow - lastRedraw >= TimeSpan.FromMilliseconds(1000)) {
                lastRedraw = DateTime.UtcNow;

                if (view.Visible) {
                    Redraw();
                }
            }
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
                    //Remove the view
                    if (view != null) view.Dispose();
                }

                // Indicate that the instance has been disposed.
                disposed = true;
            }
        }
    }
}
