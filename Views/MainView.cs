using System;
using System.Globalization;
using DoThingsBot.FSM.States;
using DoThingsBot.Views.Pages;

using VirindiViewService.Controls;

namespace DoThingsBot.Views {
    internal class MainView : IDisposable {
        public readonly VirindiViewService.ViewProperties properties;
        public readonly VirindiViewService.ControlGroup controls;
        public readonly VirindiViewService.HudView view;

        public MainPage mainPage;
        public LogsLostItemsPage logsLostItemsPage;
        public LogsMessagesPage logsMessagesPage;
        public LogsGiftsPage logsGiftsPage;
        public EquipmentIdlePage equipmentIdlePage;
        public EquipmentBuffingPage equipmentBuffingPage;
        public EquipmentCraftingPage equipmentCraftingPage;
        public EquipmentTinkeringPage equipmentTinkeringPage;
        public EquipmentBrillPage equipmentBrillPage;
        public AnnouncementsPage announcementsPage;
        public PortalsPage portalsPage;
        public ConfigPage configPage;
        public BuffBotPage buffBotPage;
        public CraftBotPage craftBotPage;
        public TinkerBotPage tinkerBotPage;
        public InfinitesPage infinitesPage;
        public StockPage stockPage;
        public NavPage navPage;

        public MainView() {
            try {
                // Create the view
                VirindiViewService.XMLParsers.Decal3XMLParser parser = new VirindiViewService.XMLParsers.Decal3XMLParser();
                parser.ParseFromResource("DoThingsBot.Views.mainView.xml", out properties, out controls);

                // Display the view
                view = new VirindiViewService.HudView(properties, controls);
                
                mainPage = new MainPage(this);
                logsLostItemsPage = new LogsLostItemsPage(this);
                logsMessagesPage = new LogsMessagesPage(this);
                logsGiftsPage = new LogsGiftsPage(this);
                equipmentIdlePage = new EquipmentIdlePage(this);
                equipmentBuffingPage = new EquipmentBuffingPage(this);
                equipmentCraftingPage = new EquipmentCraftingPage(this);
                equipmentTinkeringPage = new EquipmentTinkeringPage(this);
                equipmentBrillPage = new EquipmentBrillPage(this);
                announcementsPage = new AnnouncementsPage(this);
                portalsPage = new PortalsPage(this);
                configPage = new ConfigPage(this);
                buffBotPage = new BuffBotPage(this);
                craftBotPage = new CraftBotPage(this);
                tinkerBotPage = new TinkerBotPage(this);
                infinitesPage = new InfinitesPage(this);
                stockPage = new StockPage(this);
                navPage = new NavPage(this);
            }
            catch (Exception ex) { Util.LogException(ex); }
        }

        private bool disposed;

        public void Dispose() {
            Dispose(true);

            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            // If you need thread safety, use a lock around these 
            // operations, as well as in your methods that use the resource.
            if (!disposed) {
                if (disposing) {
                    // children
                    if (navPage != null) navPage.Dispose();
                    if (portalsPage != null) portalsPage.Dispose();
                    if (logsGiftsPage != null) logsGiftsPage.Dispose();
                    if (logsMessagesPage != null) logsMessagesPage.Dispose();
                    if (logsLostItemsPage != null) logsLostItemsPage.Dispose();
                    if (equipmentIdlePage != null) equipmentIdlePage.Dispose();
                    if (equipmentBuffingPage != null) equipmentBuffingPage.Dispose();
                    if (equipmentCraftingPage != null) equipmentCraftingPage.Dispose();
                    if (equipmentTinkeringPage != null) equipmentTinkeringPage.Dispose();
                    if (equipmentBrillPage != null) equipmentBrillPage.Dispose();
                    if (announcementsPage != null) announcementsPage.Dispose();
                    if (configPage != null) configPage.Dispose();
                    if (buffBotPage != null) buffBotPage.Dispose();
                    if (craftBotPage != null) craftBotPage.Dispose();
                    if (tinkerBotPage != null) tinkerBotPage.Dispose();
                    if (stockPage != null) stockPage.Dispose();
                    if (mainPage != null) mainPage.Dispose();

                    //Remove the view
                    if (view != null) view.Dispose();
                }

                // Indicate that the instance has been disposed.
                disposed = true;
            }
        }
    }
}
