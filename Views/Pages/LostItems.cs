using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using DoThingsBot.Lib;
using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using VirindiViewService.Controls;

namespace DoThingsBot.Views.Pages {
    class LogsLostItemsPage : IDisposable {
        public HudList UILogsLostItemsList { get; set; }
        HudButton UILostItemsScan { get; set; }

        public LogsLostItemsPage(MainView mainView) {
            try {
                UILogsLostItemsList = mainView.view != null ? (HudList)mainView.view["UILogsLostItemsList"] : new HudList();
                UILostItemsScan = mainView.view != null ? (HudButton)mainView.view["UILostItemsScan"] : new HudButton();

                UILostItemsScan.Hit += (s, e) => {
                    try {
                        LostItems.ScanAll();
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                };
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
                    }

                    disposed = true;
                }
            }
            catch (Exception ex) { Util.LogException(ex); }
        }
    }
}