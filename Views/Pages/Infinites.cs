using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class InfinitesPage : IDisposable {
        HudButton UIInfinitesAddSelected { get; set; }
        HudList UIInfinitesList { get; set; }

        readonly public static List<string> ValidItems = new List<string>() {
            "Infinite Leather",
            "Infinite Elaborate Dried Rations",
            "Infinite Simple Dried Rations",
            "Perennial Argenory Dye",
            "Perennial Berimphur Dye",
            "Perennial Botched Dye",
            "Perennial Colban Dye",
            "Perennial Hennacin Dye",
            "Perennial Lapyan Dye",
            "Perennial Minalim Dye",
            "Perennial Relanim Dye",
            "Perennial Thananim Dye",
            "Perennial Verdalim Dye"
        };

        public InfinitesPage(MainView mainView) {
            try {
                UIInfinitesAddSelected = (HudButton)mainView.view["UIInfinitesAddSelected"];
                UIInfinitesList = (HudList)mainView.view["UIInfinitesList"];

                Config.Bot.InfiniteItemIds.Changed += obj => {
                    RefreshIdleEquipmentList();
                };

                UIInfinitesList.Click += new HudList.delClickedControl(UIEquipmentIdleList_Click);

                UIInfinitesAddSelected.Hit += (s, e) => {
                    try {
                        WorldObject selectedObject = Globals.Core.WorldFilter[Globals.Host.Actions.CurrentSelection];
                        if (!ValidItems.Contains(selectedObject.Name)) {
                            Util.WriteToChat("Error: Invalid item. Supported items are: " + string.Join(", ", ValidItems.ToArray()));
                            return;
                        }
                        List<int> newList = Config.Bot.InfiniteItemIds.Value;
                        newList.Add(selectedObject.Id);
                        Config.Bot.InfiniteItemIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshIdleEquipmentList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshIdleEquipmentList() {
            try {
                UIInfinitesList.ClearRows();

                var itemIds = Config.Bot.InfiniteItemIds.Value;

                for (int equipmentIndex = 0; equipmentIndex < itemIds.Count; equipmentIndex++) {
                    WorldObject wo = Globals.Core.WorldFilter[itemIds[equipmentIndex]];

                    if (wo == null) {
                        Util.WriteToChat(String.Format("Removing unknown item from idle equipment list: {0}", itemIds[equipmentIndex]));
                        var newList = Config.Bot.InfiniteItemIds.Value;

                        if (newList.Count > equipmentIndex) {
                            newList.RemoveAt(equipmentIndex);
                            Config.Bot.InfiniteItemIds.Value = newList;
                        }
                        continue;
                    }
                    else {
                        HudList.HudListRowAccessor newRow = UIInfinitesList.AddRow();
                        ((HudPictureBox)newRow[0]).Image = wo.Icon + 0x6000000;
                        ((HudStaticText)newRow[1]).Text = Util.GetGameItemDisplayName(wo);
                        ((HudStaticText)newRow[2]).Text = itemIds[equipmentIndex].ToString();
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIEquipmentIdleList_Click(object sender, int row, int col) {
            try {
                var newList = Config.Bot.InfiniteItemIds.Value;

                if (newList.Count > row) {
                    newList.RemoveAt(row);
                    Config.Bot.InfiniteItemIds.Value = newList;
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
                        UIInfinitesList.Click -= new HudList.delClickedControl(UIEquipmentIdleList_Click);
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}
