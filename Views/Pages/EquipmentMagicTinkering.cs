using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages
{
    class EquipmentMagicTinkeringPage : IDisposable
    {
        HudButton UIEquipmentMagicTinkeringAddSelected { get; set; }
        HudButton UIEquipmentMagicTinkeringAddEquipped { get; set; }
        HudList UIEquipmentMagicTinkeringList { get; set; }

        public EquipmentMagicTinkeringPage(MainView mainView)
        {
            try
            {
                UIEquipmentMagicTinkeringAddSelected = mainView.view != null ? (HudButton)mainView.view["UIEquipmentMagicTinkeringAddSelected"] : new HudButton();
                UIEquipmentMagicTinkeringAddEquipped = mainView.view != null ? (HudButton)mainView.view["UIEquipmentMagicTinkeringAddEquipped"] : new HudButton();
                UIEquipmentMagicTinkeringList = mainView.view != null ? (HudList)mainView.view["UIEquipmentMagicTinkeringList"] : new HudList();

                Config.Equipment.MagicTinkerEquipmentIds.Changed += obj => {
                    RefreshMagicTinkeringEquipmentList();
                };

                UIEquipmentMagicTinkeringList.Click += new HudList.delClickedControl(UIEquipmentMagicTinkeringList_Click);

                UIEquipmentMagicTinkeringAddSelected.Hit += (s, e) => {
                    try
                    {
                        WorldObject selectedObject = Globals.Core.WorldFilter[Globals.Host.Actions.CurrentSelection];
                        List<int> newList = Config.Equipment.MagicTinkerEquipmentIds.Value;
                        newList.Add(selectedObject.Id);
                        Config.Equipment.MagicTinkerEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIEquipmentMagicTinkeringAddEquipped.Hit += (s, e) => {
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
                        Config.Equipment.MagicTinkerEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshMagicTinkeringEquipmentList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshMagicTinkeringEquipmentList()
        {
            try
            {
                UIEquipmentMagicTinkeringList.ClearRows();

                var tinkeringEquipment = Config.Equipment.MagicTinkerEquipmentIds.Value;

                for (int equipmentIndex = 0; equipmentIndex < tinkeringEquipment.Count; equipmentIndex++)
                {

                    if (!CoreManager.Current.Actions.IsValidObject(tinkeringEquipment[equipmentIndex]))
                    {
                        Util.WriteToChat(String.Format("Removing unknown item from tinkering equipment list: {0}", tinkeringEquipment[equipmentIndex]));
                        var newList = Config.Equipment.MagicTinkerEquipmentIds.Value;

                        if (newList.Count > equipmentIndex)
                        {
                            newList.RemoveAt(equipmentIndex);
                            Config.Equipment.MagicTinkerEquipmentIds.Value = newList;
                        }
                        continue;
                    }
                    else
                    {
                        WorldObject wo = Globals.Core.WorldFilter[tinkeringEquipment[equipmentIndex]];
                        HudList.HudListRowAccessor newRow = UIEquipmentMagicTinkeringList.AddRow();
                        ((HudPictureBox)newRow[0]).Image = wo.Icon + 0x6000000;
                        ((HudStaticText)newRow[1]).Text = Util.GetGameItemDisplayName(wo);
                        ((HudStaticText)newRow[2]).Text = tinkeringEquipment[equipmentIndex].ToString();
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIEquipmentMagicTinkeringList_Click(object sender, int row, int col)
        {
            try
            {
                var newList = Config.Equipment.MagicTinkerEquipmentIds.Value;

                if (newList.Count > row)
                {
                    newList.RemoveAt(row);
                    Config.Equipment.MagicTinkerEquipmentIds.Value = newList;
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
                        UIEquipmentMagicTinkeringList.Click -= new HudList.delClickedControl(UIEquipmentMagicTinkeringList_Click);
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}