using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class EquipmentTinkeringPage : IDisposable {
        HudButton UIEquipmentTinkeringAddSelected { get; set; }
        HudButton UIEquipmentTinkeringAddEquipped { get; set; }
        HudList UIEquipmentTinkeringList { get; set; }

        public EquipmentTinkeringPage(MainView mainView) {
            try {
                UIEquipmentTinkeringAddSelected = mainView.view != null ? (HudButton)mainView.view["UIEquipmentTinkeringAddSelected"] : new HudButton();
                UIEquipmentTinkeringAddEquipped = mainView.view != null ? (HudButton)mainView.view["UIEquipmentTinkeringAddEquipped"] : new HudButton();
                UIEquipmentTinkeringList = mainView.view != null ? (HudList)mainView.view["UIEquipmentTinkeringList"] : new HudList();

                Config.Equipment.TinkerEquipmentIds.Changed += obj => {
                    RefreshTinkeringEquipmentList();
                };

                UIEquipmentTinkeringList.Click += new HudList.delClickedControl(UIEquipmentTinkeringList_Click);

                UIEquipmentTinkeringAddSelected.Hit += (s, e) => {
                    try {
                        WorldObject selectedObject = Globals.Core.WorldFilter[Globals.Host.Actions.CurrentSelection];
                        List<int> newList = Config.Equipment.TinkerEquipmentIds.Value;
                        newList.Add(selectedObject.Id);
                        Config.Equipment.TinkerEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIEquipmentTinkeringAddEquipped.Hit += (s, e) => {
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
                        Config.Equipment.TinkerEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshTinkeringEquipmentList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshTinkeringEquipmentList() {
            try {
                UIEquipmentTinkeringList.ClearRows();

                var tinkeringEquipment = Config.Equipment.TinkerEquipmentIds.Value;

                for (int equipmentIndex = 0; equipmentIndex < tinkeringEquipment.Count; equipmentIndex++) {

                    if (!CoreManager.Current.Actions.IsValidObject(tinkeringEquipment[equipmentIndex])) {
                        Util.WriteToChat(String.Format("Removing unknown item from tinkering equipment list: {0}", tinkeringEquipment[equipmentIndex]));
                        var newList = Config.Equipment.TinkerEquipmentIds.Value;

                        if (newList.Count > equipmentIndex) {
                            newList.RemoveAt(equipmentIndex);
                            Config.Equipment.TinkerEquipmentIds.Value = newList;
                        }
                        continue;
                    }
                    else {
                        WorldObject wo = Globals.Core.WorldFilter[tinkeringEquipment[equipmentIndex]];
                        HudList.HudListRowAccessor newRow = UIEquipmentTinkeringList.AddRow();
                        ((HudPictureBox)newRow[0]).Image = wo.Icon + 0x6000000;
                        ((HudStaticText)newRow[1]).Text = Util.GetGameItemDisplayName(wo);
                        ((HudStaticText)newRow[2]).Text = tinkeringEquipment[equipmentIndex].ToString();
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIEquipmentTinkeringList_Click(object sender, int row, int col) {
            try {
                var newList = Config.Equipment.TinkerEquipmentIds.Value;

                if (newList.Count > row) {
                    newList.RemoveAt(row);
                    Config.Equipment.TinkerEquipmentIds.Value = newList;
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
                        UIEquipmentTinkeringList.Click -= new HudList.delClickedControl(UIEquipmentTinkeringList_Click);
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}