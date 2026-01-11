using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class EquipmentCraftingPage : IDisposable {
        HudButton UIEquipmentCraftingAddSelected { get; set; }
        HudButton UIEquipmentCraftingAddEquipped { get; set; }
        HudList UIEquipmentCraftingList { get; set; }

        public EquipmentCraftingPage(MainView mainView) {
            try {
                UIEquipmentCraftingAddSelected = mainView.view != null ? (HudButton)mainView.view["UIEquipmentCraftingAddSelected"] : new HudButton();
                UIEquipmentCraftingAddEquipped = mainView.view != null ? (HudButton)mainView.view["UIEquipmentCraftingAddEquipped"] : new HudButton();
                UIEquipmentCraftingList = mainView.view != null ? (HudList)mainView.view["UIEquipmentCraftingList"] : new HudList();

                Config.Equipment.CraftEquipmentIds.Changed += obj => {
                    RefreshCraftingEquipmentList();
                };

                UIEquipmentCraftingList.Click += new HudList.delClickedControl(UIEquipmentCraftingList_Click);

                UIEquipmentCraftingAddSelected.Hit += (s, e) => {
                    try {
                        WorldObject selectedObject = Globals.Core.WorldFilter[Globals.Core.Actions.CurrentSelection];
                        List<int> newList = Config.Equipment.CraftEquipmentIds.Value;
                        newList.Add(selectedObject.Id);
                        Config.Equipment.CraftEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIEquipmentCraftingAddEquipped.Hit += (s, e) => {
                    try {
                        List<int> newList = new List<int>();
                        var wos = CoreManager.Current.WorldFilter.GetInventory();
                        foreach (var item in wos) {
                            if (item.Values(LongValueKey.Slot, -1) == -1) {
                                newList.Add(item.Id);
                                Util.WriteToChat($"Adding: {item.Name}");
                            }
                        }
                        wos.Dispose();
                        Config.Equipment.CraftEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshCraftingEquipmentList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshCraftingEquipmentList() {
            try {
                UIEquipmentCraftingList.ClearRows();

                var craftingEquipment = Config.Equipment.CraftEquipmentIds.Value;

                for (int equipmentIndex = 0; equipmentIndex < craftingEquipment.Count; equipmentIndex++) {
                    WorldObject wo = Globals.Core.WorldFilter[craftingEquipment[equipmentIndex]];

                    if (wo == null) {
                        Util.WriteToChat(String.Format("Removing unknown item from buffing equipment list: {0}", craftingEquipment[equipmentIndex]));
                        var newList = Config.Equipment.CraftEquipmentIds.Value;

                        if (newList.Count > equipmentIndex) {
                            newList.RemoveAt(equipmentIndex);
                            Config.Equipment.CraftEquipmentIds.Value = newList;
                        }
                        continue;
                    }
                    else {
                        HudList.HudListRowAccessor newRow = UIEquipmentCraftingList.AddRow();
                        ((HudPictureBox)newRow[0]).Image = wo.Icon + 0x6000000;
                        ((HudStaticText)newRow[1]).Text = Util.GetGameItemDisplayName(wo);
                        ((HudStaticText)newRow[2]).Text = craftingEquipment[equipmentIndex].ToString();
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIEquipmentCraftingList_Click(object sender, int row, int col) {
            try {
                var newList = Config.Equipment.CraftEquipmentIds.Value;

                if (newList.Count > row) {
                    newList.RemoveAt(row);
                    Config.Equipment.CraftEquipmentIds.Value = newList;
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
                        UIEquipmentCraftingList.Click -= new HudList.delClickedControl(UIEquipmentCraftingList_Click);
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}