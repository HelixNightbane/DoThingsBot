using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DoThingsBot.Views.Pages;

using VirindiViewService.Controls;

namespace DoThingsBot.Views {
    public enum ProfileEditMode {
        BUFF_PROFILES,
        BOT_PROFILES
    };

    public class ProfileManagerView : IDisposable {
        public readonly VirindiViewService.ViewProperties properties;
        public readonly VirindiViewService.ControlGroup controls;
        public readonly VirindiViewService.HudView view;

        private readonly HudList UIBuffProfileManagerAvailableSpells;
        private readonly HudList UIBuffProfileManagerProfileSpells;
        private readonly HudCombo UIBuffProfileManagerEdit;
        private readonly HudButton UIBuffProfileManagerMoveSpell;
        private readonly HudTextBox UIBuffProfileManagerAliases;
        private readonly HudCheckBox UIBuffProfileManagerShowProfiles;
        private readonly HudCheckBox UIBuffProfileManagerShowItem;
        private readonly HudCheckBox UIBuffProfileManagerShowCreature;
        private readonly HudCheckBox UIBuffProfileManagerShowLife;
        private readonly HudButton UIBuffProfileManagerSaveProfile;
        private readonly HudStaticText UIBuffProfileManagerAliasesLabel;
        private readonly HudButton UIBuffProfileManagerAddNew;
        private readonly HudStaticText UIBuffProfileManagerAddLabel;
        private readonly HudTextBox UIBuffProfileManagerNewName;

        private int selectedProfileRow = -1;
        private int selectedAvailableRow = 0;
        private List<Spells.SpellClass> profileFamilyIds = new List<Spells.SpellClass>();
        private List<string> profileIncludes = new List<string>();
        private Buffs.BuffProfile profile;
        private ProfileEditMode editMode = ProfileEditMode.BUFF_PROFILES; 

        public ProfileManagerView() {
            try {
                // Create the view
                VirindiViewService.XMLParsers.Decal3XMLParser parser = new VirindiViewService.XMLParsers.Decal3XMLParser();
                parser.ParseFromResource("DoThingsBot.Views.profileManagerView.xml", out properties, out controls);
                
                view = new VirindiViewService.HudView(properties, controls);
                view.ShowInBar = false;

                UIBuffProfileManagerEdit = (HudCombo)view["BuffProfileManagerEdit"];
                UIBuffProfileManagerAvailableSpells = (HudList)view["BuffProfileManagerAvailableSpells"];
                UIBuffProfileManagerProfileSpells = (HudList)view["BuffProfileManagerProfileSpells"];
                UIBuffProfileManagerMoveSpell = (HudButton)view["BuffProfileManagerMoveSpell"];
                UIBuffProfileManagerAliases = (HudTextBox)view["BuffProfileManagerAliases"];
                UIBuffProfileManagerShowProfiles = (HudCheckBox)view["BuffProfileManagerShowProfiles"];
                UIBuffProfileManagerShowItem = (HudCheckBox)view["BuffProfileManagerShowItem"];
                UIBuffProfileManagerShowCreature = (HudCheckBox)view["BuffProfileManagerShowCreature"];
                UIBuffProfileManagerShowLife = (HudCheckBox)view["BuffProfileManagerShowLife"];
                UIBuffProfileManagerSaveProfile = (HudButton)view["BuffProfileManagerSaveProfile"];
                UIBuffProfileManagerAliasesLabel = (HudStaticText)view["BuffProfileManagerAliasesLabel"];
                UIBuffProfileManagerAddNew = (HudButton)view["BuffProfileManagerAddNew"];
                UIBuffProfileManagerAddLabel = (HudStaticText)view["BuffProfileManagerAddLabel"];
                UIBuffProfileManagerNewName = (HudTextBox)view["BuffProfileManagerNewName"];

                UIBuffProfileManagerShowProfiles.Checked = true;
                UIBuffProfileManagerShowItem.Checked = true;
                UIBuffProfileManagerShowCreature.Checked = true;
                UIBuffProfileManagerShowLife.Checked = true;

                UIBuffProfileManagerShowProfiles.Change += FiltersChanged;
                UIBuffProfileManagerShowItem.Change += FiltersChanged;
                UIBuffProfileManagerShowCreature.Change += FiltersChanged;
                UIBuffProfileManagerShowLife.Change += FiltersChanged;

                view.VisibleChanged += View_VisibleChanged;
                //view.Visible = true;

                UIBuffProfileManagerEdit.Change += UIBuffProfileManagerEdit_Change;
                UIBuffProfileManagerProfileSpells.Click += UIBuffProfileManagerProfileSpells_Click;
                UIBuffProfileManagerAvailableSpells.Click += UIBuffProfileManagerAvailableSpells_Click;

                UIBuffProfileManagerMoveSpell.Hit += UIBuffProfileManagerMoveSpell_Hit;
                UIBuffProfileManagerSaveProfile.Hit += UIBuffProfileManagerSaveProfile_Hit;

                UIBuffProfileManagerAddNew.Hit += UIBuffProfileManagerAddNew_Hit;
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIBuffProfileManagerAddNew_Hit(object sender, EventArgs e) {
            try {
                var newName = UIBuffProfileManagerNewName.Text;

                if (string.IsNullOrEmpty(newName)) {
                    Util.WriteToChat("Profile name cannot be blank.");
                    return;
                }

                if (newName.Contains(" ")) {
                    Util.WriteToChat("Profile name cannot contain spaces.");
                    return;
                }

                UIBuffProfileManagerEdit.AddItem(newName, newName);
                UIBuffProfileManagerEdit.Current = UIBuffProfileManagerEdit.Count - 1;

                Util.WriteToChat("Creating new profile: " + newName);

                LoadProfile(newName, true);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        public void EditBuffProfiles() {
            view.Visible = true;
            editMode = ProfileEditMode.BUFF_PROFILES;
            ReloadProfiles();

            UIBuffProfileManagerShowProfiles.Visible = true;
            UIBuffProfileManagerAliases.Visible = true;
            UIBuffProfileManagerAliasesLabel.Visible = true;
            UIBuffProfileManagerAddNew.Visible = true;
            UIBuffProfileManagerAddLabel.Visible = true;
            UIBuffProfileManagerNewName.Visible = true;
        }

        public void EditBotProfiles() {
            view.Visible = true;
            editMode = ProfileEditMode.BOT_PROFILES;
            ReloadProfiles();

            UIBuffProfileManagerShowProfiles.Visible = false;
            UIBuffProfileManagerAliases.Visible = false;
            UIBuffProfileManagerAliasesLabel.Visible = false;
            UIBuffProfileManagerAddNew.Visible = false;
            UIBuffProfileManagerAddLabel.Visible = false;
            UIBuffProfileManagerNewName.Visible = false;
        }

        private void UIBuffProfileManagerSaveProfile_Hit(object sender, EventArgs e) {
            var alreadyListed = new List<string>();

            var aliases = UIBuffProfileManagerAliases.Text;
            var profileName = ((HudStaticText)(UIBuffProfileManagerEdit[UIBuffProfileManagerEdit.Current])).Text;
            var fileContents = string.Format("<profile name=\"{0}\" aliases=\"{1}\">\n", profileName, aliases);

            for (var i = 0; i < UIBuffProfileManagerProfileSpells.RowCount; i++) {
                var r = (HudList.HudListRowAccessor)UIBuffProfileManagerProfileSpells[i];
                var name = ((HudStaticText)r[1]).Text.Replace("> ", "");
                if (((HudStaticText)r[4]).Text == "-1") {
                    var profile = Buffs.Buffs.profiles[name];

                    if (profile != null) {
                        fileContents += string.Format(" <spell profile=\"{0}\" />\n", profile.name);
                    }
                }
                else {
                    if (!alreadyListed.Contains(name)) {
                        alreadyListed.Add(name);
                        fileContents += string.Format(" <spell family=\"{0}\" />\n", UnFriendlyName(name));
                    }
                }
            }

            fileContents += "</profile>";

            if (editMode == ProfileEditMode.BUFF_PROFILES) {
                var path = Buffs.Buffs.GetProfilePath(profileName);
                File.WriteAllText(path, fileContents);
            }
            else if (editMode == ProfileEditMode.BOT_PROFILES) {
                var path = Buffs.Buffs.GetBotProfilePath(profileName);
                File.WriteAllText(path, fileContents);
            }

            Util.WriteToChat("Saved profile: " + profileName);

            Buffs.Buffs.ReloadProfiles();
        }

        private object UnFriendlyName(string name) {
            return name.Replace(" ", "_").ToUpper();
        }

        private void FiltersChanged(object sender, EventArgs e) {
            RedrawAvailableList();
        }

        private void UIBuffProfileManagerMoveSpell_Hit(object sender, EventArgs e) {
            if (selectedProfileRow >= 0) {
                selectedAvailableRow = 0;
                int familyId = 0;
                var row = (HudList.HudListRowAccessor)UIBuffProfileManagerProfileSpells[selectedProfileRow];

                if (!Int32.TryParse(((HudStaticText)row[4]).Text, out familyId)) return;

                var added = false;
                if (familyId == -1) {
                    var profileName = ((HudStaticText)row[1]).Text.Replace("> ", "");
                    profileIncludes.Remove(profileName);
                    for (var i = 0; i < UIBuffProfileManagerAvailableSpells.RowCount; i++) {
                        var r = (HudList.HudListRowAccessor)UIBuffProfileManagerAvailableSpells[i];

                        if (profileName.ToLower().CompareTo(((HudStaticText)r[1]).Text.Replace("> ", "").ToLower()) == -1) {
                            HudList.HudListRowAccessor newRow = (HudList.HudListRowAccessor)UIBuffProfileManagerAvailableSpells.InsertRow(i);

                            ((HudPictureBox)newRow[0]).Image = 0x060016CB;
                            ((HudStaticText)newRow[1]).Text = profileName;
                            ((HudStaticText)newRow[2]).Text = "-1";

                            added = true;
                            selectedAvailableRow = i;
                            break;
                        }
                    }

                    if (!added) {
                        HudList.HudListRowAccessor newRow = (HudList.HudListRowAccessor)UIBuffProfileManagerAvailableSpells.AddRow();
                        ((HudPictureBox)newRow[0]).Image = 0x060016CB;
                        ((HudStaticText)newRow[1]).Text = profileName;
                        ((HudStaticText)newRow[2]).Text = "-1";
                        selectedAvailableRow = UIBuffProfileManagerAvailableSpells.RowCount-1;
                    }
                    UIBuffProfileManagerProfileSpells.RemoveRow(selectedProfileRow);
                    selectedProfileRow = -1;
                    RedrawAvailableList();
                    FixListDisplay();
                    return;
                }
                else {
                    profileFamilyIds.Remove((Spells.SpellClass)familyId);
                }

                UIBuffProfileManagerProfileSpells.RemoveRow(selectedProfileRow);

                selectedAvailableRow = 0;
                selectedProfileRow = -1;

                for (var i = 0; i < UIBuffProfileManagerAvailableSpells.RowCount; i++) {
                    var r = (HudList.HudListRowAccessor)UIBuffProfileManagerAvailableSpells[i];
                    int fid = 0;
                    Int32.TryParse(((HudStaticText)r[2]).Text, out fid);
                    if (FriendlyName((Spells.SpellClass)familyId).ToLower().CompareTo(FriendlyName((Spells.SpellClass)fid).ToLower()) == -1) {
                        HudList.HudListRowAccessor newRow;
                        if (i >= UIBuffProfileManagerAvailableSpells.RowCount) {
                            newRow = (HudList.HudListRowAccessor)UIBuffProfileManagerAvailableSpells.AddRow();
                        }
                        else {
                            newRow = (HudList.HudListRowAccessor)UIBuffProfileManagerAvailableSpells.InsertRow(i);
                        }
                        var spell = Spells.GetExampleSpellByClass((Spells.SpellClass)familyId);
                        var friendlyName = FriendlyName((Spells.SpellClass)familyId);

                        ((HudPictureBox)newRow[0]).Image = spell.IconId;
                        ((HudStaticText)newRow[1]).Text = friendlyName;
                        ((HudStaticText)newRow[2]).Text = ((int)familyId).ToString();

                        selectedAvailableRow = i;
                        break;
                    }
                }

                FixListDisplay();
            }
            else {
                int familyId = 0;
                var row = (HudList.HudListRowAccessor)UIBuffProfileManagerAvailableSpells[selectedAvailableRow];

                Int32.TryParse(((HudStaticText)row[2]).Text, out familyId);

                if (familyId == 0) return;
                
                if (familyId == -1) {
                    var profileName = ((HudStaticText)row[1]).Text.Replace("> ", "");
                    profileIncludes.Add(profileName);
                    HudList.HudListRowAccessor newProfileRow = (HudList.HudListRowAccessor)UIBuffProfileManagerProfileSpells.AddRow();
                            
                    ((HudPictureBox)newProfileRow[0]).Image = 0x060016CB;
                    ((HudStaticText)newProfileRow[1]).Text = profileName;
                    ((HudStaticText)newProfileRow[4]).Text = "-1";

                    UIBuffProfileManagerAvailableSpells.RemoveRow(selectedAvailableRow);
                    selectedAvailableRow = -1;
                    selectedProfileRow = UIBuffProfileManagerProfileSpells.RowCount - 1;
                    RedrawAvailableList();
                    FixListDisplay();
                    return;
                }
                else {
                    profileFamilyIds.Add((Spells.SpellClass)familyId);
                }

                var newRow = (HudList.HudListRowAccessor)UIBuffProfileManagerProfileSpells.AddRow();
                var spell = Spells.GetExampleSpellByClass((Spells.SpellClass)familyId);
                var friendlyName = FriendlyName((Spells.SpellClass)familyId);

                ((HudPictureBox)newRow[0]).Image = spell.IconId;
                ((HudStaticText)newRow[1]).Text = friendlyName;
                ((HudPictureBox)newRow[2]).Image = 100673788; // up arrow
                ((HudPictureBox)newRow[3]).Image = 100673789; // down arrow
                ((HudStaticText)newRow[4]).Text = ((int)familyId).ToString();

                UIBuffProfileManagerAvailableSpells.RemoveRow(selectedAvailableRow);

                selectedAvailableRow = -1;
                selectedProfileRow = UIBuffProfileManagerProfileSpells.RowCount - 1;

                UIBuffProfileManagerProfileSpells.ScrollPosition = UIBuffProfileManagerProfileSpells.MaxScroll;

                //RedrawAvailableList();
                FixListDisplay();
            }
        }

        private void RefreshAvailableList() {
            var values = new List<Spells.SpellClass>();
            var alreadyListed = new List<string>();
            var index = 0;

            var vs = Enum.GetValues(typeof(Spells.SpellClass));
            foreach (var v in vs) {
                values.Add((Spells.SpellClass)v);
            }

            for (var i = 0; i < UIBuffProfileManagerAvailableSpells.RowCount; i++) {
                var r = (HudList.HudListRowAccessor)UIBuffProfileManagerAvailableSpells[i];
                alreadyListed.Add(((HudStaticText)r[1]).Text.Replace("> ", ""));
            }

            for (var i = 0; i < UIBuffProfileManagerProfileSpells.RowCount; i++) {
                var r = (HudList.HudListRowAccessor)UIBuffProfileManagerProfileSpells[i];
                alreadyListed.Add(((HudStaticText)r[1]).Text.Replace("> ", ""));
            }

            foreach (Spells.SpellClass family in values) {
                if (family != Spells.SpellClass.UNKNOWN && !alreadyListed.Contains(FriendlyName(family))) {
                    var spell = Spells.GetExampleSpellByClass(family);
                    var friendlyName = FriendlyName(family);

                    if (spell == null) continue;

                    var added = false;

                    for (var i = 0; i < UIBuffProfileManagerAvailableSpells.RowCount; i++) {
                        var r = (HudList.HudListRowAccessor)UIBuffProfileManagerAvailableSpells[i];
                        if (friendlyName.ToLower().CompareTo(((HudStaticText)r[1]).Text.ToLower()) == -1) {
                            alreadyListed.Add(friendlyName);

                            HudList.HudListRowAccessor newRow = UIBuffProfileManagerAvailableSpells.InsertRow(i);
                            ((HudPictureBox)newRow[0]).Image = spell.IconId;
                            ((HudStaticText)newRow[1]).Text = index == selectedAvailableRow ? "> " + friendlyName : friendlyName;
                            ((HudStaticText)newRow[2]).Text = ((int)family).ToString();
                            added = true;
                        }
                    }

                    if (!added) {
                        alreadyListed.Add(friendlyName);

                        HudList.HudListRowAccessor newRow = UIBuffProfileManagerAvailableSpells.AddRow();
                        ((HudPictureBox)newRow[0]).Image = spell.IconId;
                        ((HudStaticText)newRow[1]).Text = index == selectedAvailableRow ? "> " + friendlyName : friendlyName;
                        ((HudStaticText)newRow[2]).Text = ((int)family).ToString();
                    }

                    index++;
                }
            }
        }

        private void RedrawAvailableList() {
            var items = new Dictionary<string, int>();
            var alreadyListed = new List<string>();
            var index = 0;

            // add spells
            var vs = Enum.GetValues(typeof(Spells.SpellClass));
            foreach (var v in vs) {
                items.Add(((Spells.SpellClass)v).ToString(), (int)v);
            }

            // add profiles
            if (editMode == ProfileEditMode.BUFF_PROFILES) {
                foreach (var v in Buffs.Buffs.profiles.Keys) {
                    if (Buffs.Buffs.profiles[v].IsAutoGenerated()) continue;
                    items.Add(v, -1);
                }
            }

            UIBuffProfileManagerAvailableSpells.ClearRows();

            var keys = new List<string>(items.Keys);

            keys.Sort(delegate (string spell1, string spell2) {
                return (spell1.ToLower()).CompareTo((spell2.ToLower()));
            });

            for (var i = 0; i < UIBuffProfileManagerProfileSpells.RowCount; i++) {
                var r = (HudList.HudListRowAccessor)UIBuffProfileManagerProfileSpells[i];
                var rowName = ((HudStaticText)r[1]).Text.Replace("> ", "");
                alreadyListed.Add(rowName);

                if (((HudStaticText)r[4]).Text == "-1") {
                    var profile = Buffs.Buffs.GetProfile(rowName);
                    if (profile != null) {
                        foreach (var fam in profile.familyIds) {
                            alreadyListed.Add(FriendlyName(fam));
                        }
                    }
                }
            }

            foreach (string key in keys) {
                var name = "";
                int icon = 0x060016CB;

                if (items[key] == -1) {
                    if (UIBuffProfileManagerShowProfiles.Checked == false) continue;
                    name = key;
                }
                else {
                    var spell = Spells.GetExampleSpellByClass((Spells.SpellClass)items[key]);
                    if (spell == null) {
                        continue;
                    }

                    if (spell.School.ToString() == "Creature Enchantment" && !UIBuffProfileManagerShowCreature.Checked) continue;
                    if (spell.School.ToString() == "Item Enchantment" && !UIBuffProfileManagerShowItem.Checked) continue;
                    if (spell.School.ToString() == "Life Magic" && !UIBuffProfileManagerShowLife.Checked) continue;

                    name = FriendlyName((Spells.SpellClass)items[key]);
                    icon = spell.IconId;
                }

                if (!alreadyListed.Contains(name)) {
                    alreadyListed.Add(name);

                    HudList.HudListRowAccessor newRow = UIBuffProfileManagerAvailableSpells.AddRow();
                    ((HudPictureBox)newRow[0]).Image = icon;
                    ((HudStaticText)newRow[1]).Text = index == selectedAvailableRow ? "> " + name : name;
                    ((HudStaticText)newRow[2]).Text = items[key].ToString();
                    index++;
                }
            }
        }

        private void UIBuffProfileManagerAvailableSpells_Click(object sender, int row, int col) {
            HudList.HudListRowAccessor currentSelectedRow;
            HudList.HudListRowAccessor newSelectedRow = (HudList.HudListRowAccessor)UIBuffProfileManagerAvailableSpells[row];

            if (selectedAvailableRow >= 0 && selectedAvailableRow < UIBuffProfileManagerAvailableSpells.RowCount) {
                currentSelectedRow = (HudList.HudListRowAccessor)UIBuffProfileManagerAvailableSpells[selectedAvailableRow];

                if (currentSelectedRow != null) {
                    ((HudStaticText)currentSelectedRow[1]).Text = ((HudStaticText)currentSelectedRow[1]).Text.Replace("> ", "");
                }
            }

            if (!((HudStaticText)newSelectedRow[1]).Text.StartsWith("> ")) {
                ((HudStaticText)newSelectedRow[1]).Text = "> " + ((HudStaticText)newSelectedRow[1]).Text;
            }

            selectedAvailableRow = row;
            selectedProfileRow = -1;

            UIBuffProfileManagerMoveSpell.Text = ">>>";

            FixListDisplay();
        }

        private void UIBuffProfileManagerProfileSpells_Click(object sender, int row, int col) {
            HudList.HudListRowAccessor currentSelectedRow;
            HudList.HudListRowAccessor newSelectedRow = (HudList.HudListRowAccessor)UIBuffProfileManagerProfileSpells[row];

            if (selectedProfileRow >= 0 && selectedProfileRow < UIBuffProfileManagerProfileSpells.RowCount) {
                currentSelectedRow = (HudList.HudListRowAccessor)UIBuffProfileManagerProfileSpells[selectedProfileRow];

                if (currentSelectedRow != null) {
                    ((HudStaticText)currentSelectedRow[1]).Text = ((HudStaticText)currentSelectedRow[1]).Text.Replace("> ", "");
                }
            }

            if (!((HudStaticText)newSelectedRow[1]).Text.StartsWith("> ")) {
                ((HudStaticText)newSelectedRow[1]).Text = "> " + ((HudStaticText)newSelectedRow[1]).Text;
            }
            selectedProfileRow = row;

            var rowCount = UIBuffProfileManagerProfileSpells.RowCount;

            // up arrow
            if (col == 2 && row > 0) {
                HudList.HudListRowAccessor newRow = UIBuffProfileManagerProfileSpells.InsertRow(row - 1);
                HudList.HudListRowAccessor oldRow = (HudList.HudListRowAccessor)UIBuffProfileManagerProfileSpells[row + 1];
                ((HudPictureBox)newRow[0]).Image = ((HudPictureBox)oldRow[0]).Image;
                ((HudStaticText)newRow[1]).Text = ((HudStaticText)oldRow[1]).Text;
                ((HudStaticText)newRow[4]).Text = ((HudStaticText)oldRow[4]).Text;
                UIBuffProfileManagerProfileSpells.RemoveRow(row + 1);

                selectedProfileRow = row - 1;
            }

            // down arrow
            if (col == 3 && row != rowCount-1) {
                HudList.HudListRowAccessor newRow = UIBuffProfileManagerProfileSpells.InsertRow(row + 2);
                HudList.HudListRowAccessor oldRow = (HudList.HudListRowAccessor)UIBuffProfileManagerProfileSpells[row];
                ((HudPictureBox)newRow[0]).Image = ((HudPictureBox)oldRow[0]).Image;
                ((HudStaticText)newRow[1]).Text = ((HudStaticText)oldRow[1]).Text;
                ((HudStaticText)newRow[4]).Text = ((HudStaticText)oldRow[4]).Text;
                UIBuffProfileManagerProfileSpells.RemoveRow(row);

                selectedProfileRow = row + 1;
            }

            selectedAvailableRow = -1;

            UIBuffProfileManagerMoveSpell.Text = "<<<";

            FixListDisplay();
        }

        private string FriendlyName(Spells.SpellClass spellClass) {
            var parts = spellClass.ToString().Split('_');
            List<string> endParts = new List<string>();

            foreach (var part in parts) {
                endParts.Add(part[0] + part.Substring(1).ToLower());
            }

            return string.Join(" ", endParts.ToArray());
        }

        private void FixListDisplay() {
            var rowCount = UIBuffProfileManagerProfileSpells.RowCount;

            if (selectedProfileRow >= 0 && selectedAvailableRow >= 0) {
                selectedProfileRow = -1;
            }

            if (selectedAvailableRow >= 0) {
                UIBuffProfileManagerMoveSpell.Text = ">>>";
            }
            else {
                UIBuffProfileManagerMoveSpell.Text = "<<<";
            }

            for (var i = 0; i < rowCount; i++) {
                HudList.HudListRowAccessor row = (HudList.HudListRowAccessor)UIBuffProfileManagerProfileSpells[i];
                var text = ((HudStaticText)row[1]).Text.Replace("> ", "");
                ((HudStaticText)row[1]).Text = selectedProfileRow == i ? "> " + text : text;
                ((HudPictureBox)row[2]).Image = i == 0 ? 100677592 : 100673788; // up arrow
                ((HudPictureBox)row[3]).Image = i == rowCount - 1 ? 100677592 : 100673789; // down arrow
            }

            rowCount = UIBuffProfileManagerAvailableSpells.RowCount;

            for (var i = 0; i < rowCount; i++) {
                HudList.HudListRowAccessor row = (HudList.HudListRowAccessor)UIBuffProfileManagerAvailableSpells[i];
                var text = ((HudStaticText)row[1]).Text.Replace("> ", "");
                ((HudStaticText)row[1]).Text = selectedAvailableRow == i ? "> " + text : text;
            }
        }

        private void UIBuffProfileManagerEdit_Change(object sender, EventArgs e) {
            HudStaticText c = (HudStaticText)(UIBuffProfileManagerEdit[UIBuffProfileManagerEdit.Current]);
            LoadProfile(c.Text);
        }

        private void LoadProfile(string name, bool isNew = false) {
            try {
                if (string.IsNullOrEmpty(name)) {
                    ReloadProfiles();
                    return;
                }

                UIBuffProfileManagerProfileSpells.ClearRows();
                UIBuffProfileManagerAvailableSpells.ClearRows();

                if (selectedAvailableRow != -1) selectedAvailableRow = 0;
                if (selectedProfileRow != -1) selectedProfileRow = 0;

                if (!isNew) {
                    if (editMode == ProfileEditMode.BUFF_PROFILES) {
                        profile = Buffs.Buffs.GetProfile(name);
                    }
                    else if (editMode == ProfileEditMode.BOT_PROFILES) {
                        profile = Buffs.Buffs.GetBotProfile(name);
                    }
                }

                var index = 0;

                profileFamilyIds.Clear();
                profileIncludes.Clear();

                if (!isNew) {
                    profileIncludes.AddRange(profile.includedProfiles.Values);
                    profileFamilyIds.AddRange(profile.directFamilyIds);

                    foreach (var family in profileFamilyIds) {
                        while (profile.includedProfiles.ContainsKey(index)) {
                            HudList.HudListRowAccessor profileRow = UIBuffProfileManagerProfileSpells.AddRow();
                            ((HudPictureBox)profileRow[0]).Image = 0x060016CB;
                            ((HudStaticText)profileRow[1]).Text = profile.includedProfiles[index];
                            ((HudStaticText)profileRow[4]).Text = "-1";
                            index++;
                        }

                        var spell = Spells.GetExampleSpellByClass(family);
                        var friendlyName = FriendlyName(family);
                        HudList.HudListRowAccessor newRow = UIBuffProfileManagerProfileSpells.AddRow();
                        ((HudPictureBox)newRow[0]).Image = spell.IconId;
                        ((HudStaticText)newRow[1]).Text = index == selectedProfileRow ? "> " + friendlyName : friendlyName;
                        ((HudStaticText)newRow[4]).Text = ((int)family).ToString();

                        index++;
                    }

                    while (profile.includedProfiles.ContainsKey(index)) {
                        HudList.HudListRowAccessor profileRow = UIBuffProfileManagerProfileSpells.AddRow();
                        ((HudPictureBox)profileRow[0]).Image = 0x060016CB;
                        ((HudStaticText)profileRow[1]).Text = profile.includedProfiles[index];
                        ((HudStaticText)profileRow[4]).Text = "-1";

                        index++;
                    }

                    UIBuffProfileManagerAliases.Text = string.Join(" ", profile.aliases.ToArray());
                }
                else {
                    UIBuffProfileManagerAliases.Text = "";
                }

                RedrawAvailableList();

                FixListDisplay();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void View_VisibleChanged(object sender, EventArgs e) {
            if (view.Visible) {
                ReloadProfiles();
            }
        }

        public void ReloadProfiles() {
            UIBuffProfileManagerProfileSpells.ClearRows();
            UIBuffProfileManagerAvailableSpells.ClearRows();
            UIBuffProfileManagerEdit.Clear();

            if (selectedAvailableRow != -1) selectedAvailableRow = 0;
            if (selectedProfileRow != -1) selectedProfileRow = 0;

            UIBuffProfileManagerAliases.Text = "";
            
            LoadProfileList();
            FixListDisplay();
        }

        private void LoadProfileList() {
            var first = "";
            if (editMode == ProfileEditMode.BUFF_PROFILES) {
                foreach (var profileName in Buffs.Buffs.profiles.Keys) {
                    if (Buffs.Buffs.profiles[profileName].IsAutoGenerated()) continue;
                    UIBuffProfileManagerEdit.AddItem(profileName, profileName);
                    if (string.IsNullOrEmpty(first)) first = profileName;
                }
            }
            else {
                UIBuffProfileManagerEdit.AddItem("buff", "buff");
                UIBuffProfileManagerEdit.AddItem("idle", "idle");
                UIBuffProfileManagerEdit.AddItem("tinker", "tinker");
                first = "buff";
            }

            LoadProfile(first);
        }

        private bool disposed;

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposed) {
                if (disposing) {
                    view.VisibleChanged -= View_VisibleChanged;
                    UIBuffProfileManagerEdit.Change -= UIBuffProfileManagerEdit_Change;
                    UIBuffProfileManagerProfileSpells.Click -= UIBuffProfileManagerProfileSpells_Click;
                    //Remove the view
                    if (view != null) view.Dispose();
                }
                
                disposed = true;
            }
        }
    }
}
