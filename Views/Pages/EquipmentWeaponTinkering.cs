using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages
{
    class EquipmentWeaponTinkeringPage : IDisposable
    {
        HudButton UIEquipmentWeaponTinkeringAddSelected { get; set; }
        HudButton UIEquipmentWeaponTinkeringAddEquipped { get; set; }
        HudList UIEquipmentWeaponTinkeringList { get; set; }

        public EquipmentWeaponTinkeringPage(MainView mainView)
        {
            try
            {
                UIEquipmentWeaponTinkeringAddSelected = mainView.view != null ? (HudButton)mainView.view["UIEquipmentWeaponTinkeringAddSelected"] : new HudButton();
                UIEquipmentWeaponTinkeringAddEquipped = mainView.view != null ? (HudButton)mainView.view["UIEquipmentWeaponTinkeringAddEquipped"] : new HudButton();
                UIEquipmentWeaponTinkeringList = mainView.view != null ? (HudList)mainView.view["UIEquipmentWeaponTinkeringList"] : new HudList();

                Config.Equipment.WeaponTinkerEquipmentIds.Changed += obj => {
                    RefreshWeaponTinkeringEquipmentList();
                };

                UIEquipmentWeaponTinkeringList.Click += new HudList.delClickedControl(UIEquipmentWeaponTinkeringList_Click);

                UIEquipmentWeaponTinkeringAddSelected.Hit += (s, e) => {
                    try
                    {
                        WorldObject selectedObject = Globals.Core.WorldFilter[Globals.Core.Actions.CurrentSelection];
                        List<int> newList = Config.Equipment.WeaponTinkerEquipmentIds.Value;
                        newList.Add(selectedObject.Id);
                        Config.Equipment.WeaponTinkerEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIEquipmentWeaponTinkeringAddEquipped.Hit += (s, e) => {
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
                        Config.Equipment.WeaponTinkerEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshWeaponTinkeringEquipmentList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshWeaponTinkeringEquipmentList()
        {
            try
            {
                UIEquipmentWeaponTinkeringList.ClearRows();

                var tinkeringEquipment = Config.Equipment.WeaponTinkerEquipmentIds.Value;

                for (int equipmentIndex = 0; equipmentIndex < tinkeringEquipment.Count; equipmentIndex++)
                {

                    if (!CoreManager.Current.Actions.IsValidObject(tinkeringEquipment[equipmentIndex]))
                    {
                        Util.WriteToChat(String.Format("Removing unknown item from tinkering equipment list: {0}", tinkeringEquipment[equipmentIndex]));
                        var newList = Config.Equipment.WeaponTinkerEquipmentIds.Value;

                        if (newList.Count > equipmentIndex)
                        {
                            newList.RemoveAt(equipmentIndex);
                            Config.Equipment.WeaponTinkerEquipmentIds.Value = newList;
                        }
                        continue;
                    }
                    else
                    {
                        WorldObject wo = Globals.Core.WorldFilter[tinkeringEquipment[equipmentIndex]];
                        HudList.HudListRowAccessor newRow = UIEquipmentWeaponTinkeringList.AddRow();
                        ((HudPictureBox)newRow[0]).Image = wo.Icon + 0x6000000;
                        ((HudStaticText)newRow[1]).Text = Util.GetGameItemDisplayName(wo);
                        ((HudStaticText)newRow[2]).Text = tinkeringEquipment[equipmentIndex].ToString();
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIEquipmentWeaponTinkeringList_Click(object sender, int row, int col)
        {
            try
            {
                var newList = Config.Equipment.WeaponTinkerEquipmentIds.Value;

                if (newList.Count > row)
                {
                    newList.RemoveAt(row);
                    Config.Equipment.WeaponTinkerEquipmentIds.Value = newList;
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
                        UIEquipmentWeaponTinkeringList.Click -= new HudList.delClickedControl(UIEquipmentWeaponTinkeringList_Click);
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}