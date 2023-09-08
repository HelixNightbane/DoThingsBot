using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Interop.Filters;
using DoThingsBot.Chat;
using DoThingsBot.Lib;
using DoThingsBot.Lib.Recipes;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Forms;

namespace DoThingsBot.FSM.States {
    class BotStockState : IBotState {
        public string Name { get => "BotTrading_ReturnItemsState"; }

        public int ThinkCount { get => _thinkCounter; }
        private int _thinkCounter = 0;
        private int toolId = 0;

        private ItemBundle itemBundle;
        private Machine _machine;

        private bool giveAllegOnly = Config.Stock.UIStockAllegianceOnly.Value;
        
        public BotStockState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            _machine = machine;

            Decal.Adapter.Wrappers.WorldObject me = Util.FindPlayerWorldObjectByName(CoreManager.Current.CharacterFilter.Name);
            Decal.Adapter.Wrappers.WorldObject player = Util.FindPlayerWorldObjectByName(itemBundle.GetOwner());
            
            if (Config.Stock.UIStockAllegianceOnly.Value)
            {
                var myMonarch = me.Values(Decal.Adapter.Wrappers.LongValueKey.Monarch);
                var playerMonarch = player.Values(Decal.Adapter.Wrappers.LongValueKey.Monarch);

                if (myMonarch != playerMonarch)
                {
                    ChatManager.Tell(itemBundle.GetOwner(), "You are not in my allegiance, stock not available. Please try a different command.");

                    _machine.ChangeState(new BotFinishState(itemBundle));
                    return;
                }
            }
            
            if (player == null || Util.GetDistanceFromPlayer(player) > 2) {
                ChatManager.Tell(itemBundle.GetOwner(), "Please stand closer to me and try again.");

                _machine.ChangeState(new BotFinishState(itemBundle));
                return;
            }

            //var tool = Util.GetInventoryItemByName("Cooking Pot");
            //if (tool == null) {
            //    ChatManager.Tell(itemBundle.GetOwner(), $"I'm missing a Cooking Pot.  It {Recipes.GetToolLocation("Cooking Pot")}");
            //    _machine.ChangeState(new BotFinishState(itemBundle));
            //    return;
            //}
            //toolId = tool.Id;

            //ChatManager.Tell(itemBundle.GetOwner(), "Ok, Making you some rations.");
        }

        public void Exit(Machine machine) {

        }

        private DateTime lastThought = DateTime.UtcNow;
        private DateTime startTime = DateTime.UtcNow;

        public void Think(Machine machine) {
            try
            {
                if (DateTime.UtcNow - lastThought > TimeSpan.FromMilliseconds(500))
                {
                    lastThought = DateTime.UtcNow;

                    if (DateTime.UtcNow - startTime > TimeSpan.FromSeconds(20))
                    {
                        ChatManager.Tell(itemBundle.GetOwner(), "Timed out. I was unable to restock your items.");
                        machine.ChangeState(new BotTrading_TradeCancelledState(itemBundle));
                        return;
                    }

                    var message = $"I have the following items available in my stock: ";

                    var names = new List<string>();

                    foreach (var item in Config.Stock.StockItems.Value)
                    {
                        var parts = item.Split('|');
                        if (parts.Length != 4) continue;

                        names.Add(parts[0].ToLower() + ": " + parts[1].ToLower());
                    }

                    names.Sort();

                    foreach (var name in names)
                    {
                        if (message.Length + name.Length + 2 > 230)
                        {
                            ChatManager.Tell(itemBundle.GetOwner(), message);
                            message = "";
                        }

                        message += $"{name}, ";
                    }

                    if (message.Length > 0) ChatManager.Tell(itemBundle.GetOwner(), message);

                    _machine.ChangeState(new BotStock_AwaitCommandState(itemBundle));
                    return;


                }
            }

            catch (Exception e) { Util.LogException(e); }
        }
        public ItemBundle GetItemBundle() {
            return null;
        }
    }
}
