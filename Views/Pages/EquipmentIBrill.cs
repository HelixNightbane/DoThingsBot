using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class EquipmentBrillPage : IDisposable {
        HudButton UIEquipmentBrillAddSelected { get; set; }
        HudButton UIEquipmentBrillAddEquipped { get; set; }
        HudList UIEquipmentBrillList { get; set; }

        public EquipmentBrillPage(MainView mainView) {
            try {
                UIEquipmentBrillAddSelected = mainView.view != null ? (HudButton)mainView.view["UIEquipmentBrillAddSelected"] : new HudButton();
                UIEquipmentBrillAddEquipped = mainView.view != null ? (HudButton)mainView.view["UIEquipmentBrillAddEquipped"] : new HudButton();
                UIEquipmentBrillList = mainView.view != null ? (HudList)mainView.view["UIEquipmentBrillList"] : new HudList();
                
                Config.Equipment.BrillEquipmentIds.Changed += obj => {
                    RefreshBrillEquipmentList();
                };

                UIEquipmentBrillList.Click += new HudList.delClickedControl(UIEquipmentBrillList_Click);

                UIEquipmentBrillAddSelected.Hit += (s, e) => {
                    try {
                        WorldObject selectedObject = Globals.Core.WorldFilter[Globals.Core.Actions.CurrentSelection];
                        List<int> newList = Config.Equipment.BrillEquipmentIds.Value;
                        newList.Add(selectedObject.Id);
                        Config.Equipment.BrillEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIEquipmentBrillAddEquipped.Hit += (s, e) => {
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
                        Config.Equipment.BrillEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshBrillEquipmentList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshBrillEquipmentList() {
            try {
                UIEquipmentBrillList.ClearRows();

                var BrillEquipment = Config.Equipment.BrillEquipmentIds.Value;

                for (int equipmentIndex = 0; equipmentIndex < BrillEquipment.Count; equipmentIndex++) {
                    WorldObject wo = Globals.Core.WorldFilter[BrillEquipment[equipmentIndex]];

                    if (wo == null) {
                        Util.WriteToChat(String.Format("Removing unknown item from Brill equipment list: {0}", BrillEquipment[equipmentIndex]));
                        var newList = Config.Equipment.BrillEquipmentIds.Value;

                        if (newList.Count > equipmentIndex) {
                            newList.RemoveAt(equipmentIndex);
                            Config.Equipment.BrillEquipmentIds.Value = newList;
                        }
                        continue;
                    }
                    else {
                        HudList.HudListRowAccessor newRow = UIEquipmentBrillList.AddRow();
                        ((HudPictureBox)newRow[0]).Image = wo.Icon + 0x6000000;
                        ((HudStaticText)newRow[1]).Text = Util.GetGameItemDisplayName(wo);
                        ((HudStaticText)newRow[2]).Text = BrillEquipment[equipmentIndex].ToString();
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIEquipmentBrillList_Click(object sender, int row, int col) {
            try {
                var newList = Config.Equipment.BrillEquipmentIds.Value;

                if (newList.Count > row) {
                    newList.RemoveAt(row);
                    Config.Equipment.BrillEquipmentIds.Value = newList;
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
                        UIEquipmentBrillList.Click -= new HudList.delClickedControl(UIEquipmentBrillList_Click);
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}