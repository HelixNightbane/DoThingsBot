using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class EquipmentIdlePage : IDisposable {
        HudButton UIEquipmentIdleAddSelected { get; set; }
        HudButton UIEquipmentIdleAddEquipped { get; set; }
        HudList UIEquipmentIdleList { get; set; }

        public EquipmentIdlePage(MainView mainView) {
            try {
                UIEquipmentIdleAddSelected = mainView.view != null ? (HudButton)mainView.view["UIEquipmentIdleAddSelected"] : new HudButton();
                UIEquipmentIdleAddEquipped = mainView.view != null ? (HudButton)mainView.view["UIEquipmentIdleAddEquipped"] : new HudButton();
                UIEquipmentIdleList = mainView.view != null ? (HudList)mainView.view["UIEquipmentIdleList"] : new HudList();
                
                Config.Equipment.IdleEquipmentIds.Changed += obj => {
                    RefreshIdleEquipmentList();
                };

                UIEquipmentIdleList.Click += new HudList.delClickedControl(UIEquipmentIdleList_Click);

                UIEquipmentIdleAddSelected.Hit += (s, e) => {
                    try {
                        WorldObject selectedObject = Globals.Core.WorldFilter[Globals.Host.Actions.CurrentSelection];
                        List<int> newList = Config.Equipment.IdleEquipmentIds.Value;
                        newList.Add(selectedObject.Id);
                        Config.Equipment.IdleEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIEquipmentIdleAddEquipped.Hit += (s, e) => {
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
                        Config.Equipment.IdleEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshIdleEquipmentList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshIdleEquipmentList() {
            try {
                UIEquipmentIdleList.ClearRows();

                var idleEquipment = Config.Equipment.IdleEquipmentIds.Value;

                for (int equipmentIndex = 0; equipmentIndex < idleEquipment.Count; equipmentIndex++) {
                    WorldObject wo = Globals.Core.WorldFilter[idleEquipment[equipmentIndex]];

                    if (wo == null) {
                        Util.WriteToChat(String.Format("Removing unknown item from idle equipment list: {0}", idleEquipment[equipmentIndex]));
                        var newList = Config.Equipment.IdleEquipmentIds.Value;

                        if (newList.Count > equipmentIndex) {
                            newList.RemoveAt(equipmentIndex);
                            Config.Equipment.IdleEquipmentIds.Value = newList;
                        }
                        continue;
                    }
                    else {
                        HudList.HudListRowAccessor newRow = UIEquipmentIdleList.AddRow();
                        ((HudPictureBox)newRow[0]).Image = wo.Icon + 0x6000000;
                        ((HudStaticText)newRow[1]).Text = Util.GetGameItemDisplayName(wo);
                        ((HudStaticText)newRow[2]).Text = idleEquipment[equipmentIndex].ToString();
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIEquipmentIdleList_Click(object sender, int row, int col) {
            try {
                var newList = Config.Equipment.IdleEquipmentIds.Value;

                if (newList.Count > row) {
                    newList.RemoveAt(row);
                    Config.Equipment.IdleEquipmentIds.Value = newList;
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
                        UIEquipmentIdleList.Click -= new HudList.delClickedControl(UIEquipmentIdleList_Click);
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}