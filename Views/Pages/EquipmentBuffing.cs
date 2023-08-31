using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class EquipmentBuffingPage : IDisposable {
        HudButton UIEquipmentBuffingAddSelected { get; set; }
        HudButton UIEquipmentBuffingAddEquipped { get; set; }
        HudList UIEquipmentBuffingList { get; set; }

        public EquipmentBuffingPage(MainView mainView) {
            try {
                UIEquipmentBuffingAddSelected = mainView.view != null ? (HudButton)mainView.view["UIEquipmentBuffingAddSelected"] : new HudButton();
                UIEquipmentBuffingAddEquipped = mainView.view != null ? (HudButton)mainView.view["UIEquipmentBuffingAddEquipped"] : new HudButton();
                UIEquipmentBuffingList = mainView.view != null ? (HudList)mainView.view["UIEquipmentBuffingList"] : new HudList();
                
                Config.Equipment.BuffEquipmentIds.Changed += obj => {
                    RefreshBuffingEquipmentList();
                };

                UIEquipmentBuffingList.Click += new HudList.delClickedControl(UIEquipmentBuffingList_Click);

                UIEquipmentBuffingAddSelected.Hit += (s, e) => {
                    try {
                        WorldObject selectedObject = Globals.Core.WorldFilter[Globals.Host.Actions.CurrentSelection];
                        List<int> newList = Config.Equipment.BuffEquipmentIds.Value;
                        newList.Add(selectedObject.Id);
                        Config.Equipment.BuffEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIEquipmentBuffingAddEquipped.Hit += (s, e) => {
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
                        Config.Equipment.BuffEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshBuffingEquipmentList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshBuffingEquipmentList() {
            try {
                UIEquipmentBuffingList.ClearRows();

                var buffingEquipment = Config.Equipment.BuffEquipmentIds.Value;

                for (int equipmentIndex = 0; equipmentIndex < buffingEquipment.Count; equipmentIndex++) {
                    WorldObject wo = Globals.Core.WorldFilter[buffingEquipment[equipmentIndex]];

                    if (wo == null) {
                        Util.WriteToChat(String.Format("Removing unknown item from buffing equipment list: {0}", buffingEquipment[equipmentIndex]));
                        var newList = Config.Equipment.BuffEquipmentIds.Value;

                        if (newList.Count > equipmentIndex) {
                            newList.RemoveAt(equipmentIndex);
                            Config.Equipment.BuffEquipmentIds.Value = newList;
                        }
                        continue;
                    }
                    else {
                        HudList.HudListRowAccessor newRow = UIEquipmentBuffingList.AddRow();
                        ((HudPictureBox)newRow[0]).Image = wo.Icon + 0x6000000;
                        ((HudStaticText)newRow[1]).Text = Util.GetGameItemDisplayName(wo);
                        ((HudStaticText)newRow[2]).Text = buffingEquipment[equipmentIndex].ToString();
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIEquipmentBuffingList_Click(object sender, int row, int col) {
            try {
                var newList = Config.Equipment.BuffEquipmentIds.Value;

                if (newList.Count > row) {
                    newList.RemoveAt(row);
                    Config.Equipment.BuffEquipmentIds.Value = newList;
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
                        UIEquipmentBuffingList.Click -= new HudList.delClickedControl(UIEquipmentBuffingList_Click);
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}