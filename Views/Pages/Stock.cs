using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Interop.Filters;
using System;
using System.CodeDom;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class StockPage : IDisposable {
        HudCheckBox UIBotStockEnabled { get; set; }
        HudTextBox UIStockAddCommandText { get; set; }
        HudTextBox UIStockAddStackSizeText { get; set; }
        HudButton UIStockAddSelected { get; set; }
        HudList UIStockCommands { get; set; }
        HudTextBox UIStockLowCount { get; set; }

        HudCheckBox UIStockAllegianceOnly { get; set; }


        public StockPage(MainView mainView) {
            try {
                UIBotStockEnabled = mainView.view != null ? (HudCheckBox)mainView.view["UIBotStockEnabled"] : new HudCheckBox();
                UIStockAddCommandText = (HudTextBox)mainView.view["UIStockAddCommandText"];
                UIStockAddStackSizeText = (HudTextBox)mainView.view["UIStockAddStackSizeText"];
                UIStockAddSelected = (HudButton)mainView.view["UIStockAddSelected"];
                UIStockCommands = (HudList)mainView.view["UIStockCommands"];

                UIStockLowCount = (HudTextBox)mainView.view["UIStockLowCount"];

                UIStockAllegianceOnly = (HudCheckBox)mainView.view["UIStockAllegianceOnly"];

                UIBotStockEnabled.Checked = Config.Stock.Enabled.Value;
                Config.Stock.Enabled.Changed += obj => { UIStockAllegianceOnly.Checked = obj.Value; };
                UIBotStockEnabled.Change += (s, e) =>
                {
                    try
                    {
                        Config.Stock.Enabled.Value = ((HudCheckBox)s).Checked;
                    }
                    catch (Exception ex)
                    {
                        Util.LogException(ex);
                    };
                };

                UIStockLowCount.Text = Config.Stock.StockLowCount.Value.ToString();

                Config.Stock.StockLowCount.Changed += obj => { UIStockLowCount.Text = obj.Value.ToString(); };

                UIStockAllegianceOnly.Checked = Config.Stock.UIStockAllegianceOnly.Value;
                Config.Stock.UIStockAllegianceOnly.Changed += obj => { UIStockAllegianceOnly.Checked = obj.Value; };
                UIStockAllegianceOnly.Change += (s, e) =>
                {
                    try
                    {
                        Config.Stock.UIStockAllegianceOnly.Value = ((HudCheckBox)s).Checked;
                    }
                    catch (Exception ex)
                    {
                        Util.LogException(ex);
                    };
                };
                UIStockAddSelected.Hit += UIStockAddSelected_Hit;

                UIStockCommands.Click += UIStockCommands_Click;

                UIStockLowCount.LostFocus += (s, e) => {
                    try {
                        if (!int.TryParse(UIStockLowCount.Text, out int value))
                            value = Config.Stock.StockLowCount.Value;
                        Config.Stock.StockLowCount.Value = value;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshStocksList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIStockCommands_Click(object sender, int row, int col) {
            if (col != 4) return;
            if (row > Config.Stock.StockItems.Value.Count) return;

            var newList = Config.Stock.StockItems.Value;

            UIStockCommands.RemoveRow(row);
            newList.RemoveAt(row);

            Config.Stock.StockItems.Value = newList;
        }

        private void UIStockAddSelected_Hit(object sender, EventArgs e) {
            try {
                var command = UIStockAddCommandText.Text.Trim().Replace("|", "");
                int StackSize = 0;
                int maxStackSize;

                if (string.IsNullOrEmpty(command)) {
                    Util.WriteToChat("You must enter something in the command textbox. This is what the user needs to tell you in order for you to use the gem you want to add.");
                    return;
                }

                if (command.Contains(" ")) {
                    Util.WriteToChat("Commands must not contain spaces");
                    return;
                }

                if (!Globals.Core.Actions.IsValidObject(Globals.Core.Actions.CurrentSelection)) {
                    Util.WriteToChat("No item is currently selected.");
                    return;
                }

                var wo = Globals.Core.WorldFilter[Globals.Core.Actions.CurrentSelection];
                
                if (wo == null) {
                    Util.WriteToChat("Something went wrong, couldn't find selected item.");
                    return;
                }
                maxStackSize = Convert.ToInt32(wo.Values((Decal.Adapter.Wrappers.LongValueKey)Decal.Interop.Filters.LongValueKey.keyStackMax));

                if (!Int32.TryParse(UIStockAddStackSizeText.Text.Trim(), out StackSize) || StackSize < 1 || StackSize > Math.Max((maxStackSize),1))
                {
                    Util.WriteToChat("Invalid StackSize, must be between 1 and " + Math.Max((maxStackSize), 1));
                    return;
                }
                var newList = Config.Stock.StockItems.Value;
                newList.Add(string.Format("{0}|{1}|{2}|{3}", command, Util.GetObjectName(wo.Id), StackSize, wo.Icon));
                Config.Stock.StockItems.Value = newList;

                UIStockAddCommandText.Text = "";
                UIStockAddStackSizeText.Text = "1";

                RefreshStocksList();
                UIStockCommands.ScrollPosition = UIStockCommands.MaxScroll;
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshStocksList() {
            try {
                UIStockCommands.ClearRows();

                var commands = Config.Stock.RestockCommands();

                foreach (var cmd in commands) {
                    HudList.HudListRowAccessor newRow = UIStockCommands.AddRow();

                    ((HudStaticText)newRow[0]).Text = cmd.Key;
                    ((HudPictureBox)newRow[1]).Image = cmd.Value.Icon + 0x6000000;
                    ((HudStaticText)newRow[2]).Text = cmd.Value.Name;
                    ((HudStaticText)newRow[3]).Text = cmd.Value.StackSize.ToString();
                    ((HudPictureBox)newRow[4]).Image = 4600 + 0x6000000;
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

                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}