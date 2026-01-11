using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages
{
    class EquipmentItemTinkeringPage : IDisposable
    {
        HudButton UIEquipmentItemTinkeringAddSelected { get; set; }
        HudButton UIEquipmentItemTinkeringAddEquipped { get; set; }
        HudList UIEquipmentItemTinkeringList { get; set; }

        public EquipmentItemTinkeringPage(MainView mainView)
        {
            try
            {
                UIEquipmentItemTinkeringAddSelected = mainView.view != null ? (HudButton)mainView.view["UIEquipmentItemTinkeringAddSelected"] : new HudButton();
                UIEquipmentItemTinkeringAddEquipped = mainView.view != null ? (HudButton)mainView.view["UIEquipmentItemTinkeringAddEquipped"] : new HudButton();
                UIEquipmentItemTinkeringList = mainView.view != null ? (HudList)mainView.view["UIEquipmentItemTinkeringList"] : new HudList();

                Config.Equipment.ItemTinkerEquipmentIds.Changed += obj => {
                    RefreshItemTinkeringEquipmentList();
                };

                UIEquipmentItemTinkeringList.Click += new HudList.delClickedControl(UIEquipmentItemTinkeringList_Click);

                UIEquipmentItemTinkeringAddSelected.Hit += (s, e) => {
                    try
                    {
                        WorldObject selectedObject = Globals.Core.WorldFilter[Globals.Core.Actions.CurrentSelection];
                        List<int> newList = Config.Equipment.ItemTinkerEquipmentIds.Value;
                        newList.Add(selectedObject.Id);
                        Config.Equipment.ItemTinkerEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIEquipmentItemTinkeringAddEquipped.Hit += (s, e) => {
                    try
                    {
                        List<int> newList = new List<int>();
                        var wos = CoreManager.Current.WorldFilter.GetInventory();
                        foreach (var item in wos)
                        {
                            if (item.Values(LongValueKey.Slot, -1) == -1)
                            {
                                newList.Add(item.Id);
                                Util.WriteToChat($"Adding: {item.Name}");
                            }
                        }
                        wos.Dispose();
                        Config.Equipment.ItemTinkerEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshItemTinkeringEquipmentList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshItemTinkeringEquipmentList()
        {
            try
            {
                UIEquipmentItemTinkeringList.ClearRows();

                var tinkeringEquipment = Config.Equipment.ItemTinkerEquipmentIds.Value;

                for (int equipmentIndex = 0; equipmentIndex < tinkeringEquipment.Count; equipmentIndex++)
                {

                    if (!CoreManager.Current.Actions.IsValidObject(tinkeringEquipment[equipmentIndex]))
                    {
                        Util.WriteToChat(String.Format("Removing unknown item from tinkering equipment list: {0}", tinkeringEquipment[equipmentIndex]));
                        var newList = Config.Equipment.ItemTinkerEquipmentIds.Value;

                        if (newList.Count > equipmentIndex)
                        {
                            newList.RemoveAt(equipmentIndex);
                            Config.Equipment.ItemTinkerEquipmentIds.Value = newList;
                        }
                        continue;
                    }
                    else
                    {
                        WorldObject wo = Globals.Core.WorldFilter[tinkeringEquipment[equipmentIndex]];
                        HudList.HudListRowAccessor newRow = UIEquipmentItemTinkeringList.AddRow();
                        ((HudPictureBox)newRow[0]).Image = wo.Icon + 0x6000000;
                        ((HudStaticText)newRow[1]).Text = Util.GetGameItemDisplayName(wo);
                        ((HudStaticText)newRow[2]).Text = tinkeringEquipment[equipmentIndex].ToString();
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIEquipmentItemTinkeringList_Click(object sender, int row, int col)
        {
            try
            {
                var newList = Config.Equipment.ItemTinkerEquipmentIds.Value;

                if (newList.Count > row)
                {
                    newList.RemoveAt(row);
                    Config.Equipment.ItemTinkerEquipmentIds.Value = newList;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private bool disposed;

        public void Dispose()
        {
            try
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        UIEquipmentItemTinkeringList.Click -= new HudList.delClickedControl(UIEquipmentItemTinkeringList_Click);
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}