using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages
{
    class EquipmentArmorTinkeringPage : IDisposable
    {
        HudButton UIEquipmentArmorTinkeringAddSelected { get; set; }
        HudButton UIEquipmentArmorTinkeringAddEquipped { get; set; }
        HudList UIEquipmentArmorTinkeringList { get; set; }

        public EquipmentArmorTinkeringPage(MainView mainView)
        {
            try
            {
                UIEquipmentArmorTinkeringAddSelected = mainView.view != null ? (HudButton)mainView.view["UIEquipmentArmorTinkeringAddSelected"] : new HudButton();
                UIEquipmentArmorTinkeringAddEquipped = mainView.view != null ? (HudButton)mainView.view["UIEquipmentArmorTinkeringAddEquipped"] : new HudButton();
                UIEquipmentArmorTinkeringList = mainView.view != null ? (HudList)mainView.view["UIEquipmentArmorTinkeringList"] : new HudList();

                Config.Equipment.ArmorTinkerEquipmentIds.Changed += obj => {
                    RefreshArmorTinkeringEquipmentList();
                };

                UIEquipmentArmorTinkeringList.Click += new HudList.delClickedControl(UIEquipmentArmorTinkeringList_Click);

                UIEquipmentArmorTinkeringAddSelected.Hit += (s, e) => {
                    try
                    {
                        WorldObject selectedObject = Globals.Core.WorldFilter[Globals.Core.Actions.CurrentSelection];
                        List<int> newList = Config.Equipment.ArmorTinkerEquipmentIds.Value;
                        newList.Add(selectedObject.Id);
                        Config.Equipment.ArmorTinkerEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                UIEquipmentArmorTinkeringAddEquipped.Hit += (s, e) => {
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
                        Config.Equipment.ArmorTinkerEquipmentIds.Value = newList;
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };

                RefreshArmorTinkeringEquipmentList();
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void RefreshArmorTinkeringEquipmentList()
        {
            try
            {
                UIEquipmentArmorTinkeringList.ClearRows();

                var tinkeringEquipment = Config.Equipment.ArmorTinkerEquipmentIds.Value;

                for (int equipmentIndex = 0; equipmentIndex < tinkeringEquipment.Count; equipmentIndex++)
                {

                    if (!CoreManager.Current.Actions.IsValidObject(tinkeringEquipment[equipmentIndex]))
                    {
                        Util.WriteToChat(String.Format("Removing unknown item from tinkering equipment list: {0}", tinkeringEquipment[equipmentIndex]));
                        var newList = Config.Equipment.ArmorTinkerEquipmentIds.Value;

                        if (newList.Count > equipmentIndex)
                        {
                            newList.RemoveAt(equipmentIndex);
                            Config.Equipment.ArmorTinkerEquipmentIds.Value = newList;
                        }
                        continue;
                    }
                    else
                    {
                        WorldObject wo = Globals.Core.WorldFilter[tinkeringEquipment[equipmentIndex]];
                        HudList.HudListRowAccessor newRow = UIEquipmentArmorTinkeringList.AddRow();
                        ((HudPictureBox)newRow[0]).Image = wo.Icon + 0x6000000;
                        ((HudStaticText)newRow[1]).Text = Util.GetGameItemDisplayName(wo);
                        ((HudStaticText)newRow[2]).Text = tinkeringEquipment[equipmentIndex].ToString();
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private void UIEquipmentArmorTinkeringList_Click(object sender, int row, int col)
        {
            try
            {
                var newList = Config.Equipment.ArmorTinkerEquipmentIds.Value;

                if (newList.Count > row)
                {
                    newList.RemoveAt(row);
                    Config.Equipment.ArmorTinkerEquipmentIds.Value = newList;
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
                        UIEquipmentArmorTinkeringList.Click -= new HudList.delClickedControl(UIEquipmentArmorTinkeringList_Click);
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}