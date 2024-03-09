using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using DoThingsBot.Lib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DoThingsBot.FSM.States {
    public class BotStock_AwaitCommandState : IBotState {
        public string Name { get => "BotStock_AwaitCommandState"; }

        public int ThinkCount { get => _thinkCounter; }
        private int _thinkCounter = 0;
        private int targetId = 0;
        private bool restocked = false;
        private bool gaveItem = false;
        Machine _machine;

        private ItemBundle itemBundle;

        public BotStock_AwaitCommandState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            _machine = machine;

            ChatManager.RaiseChatCommandEvent += new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);

            foreach (var obj in CoreManager.Current.WorldFilter.GetByName(itemBundle.GetOwner()))
            {
                if (obj.ObjectClass == ObjectClass.Player)
                {
                    targetId = obj.Id;
                }
            }

            var player = CoreManager.Current.WorldFilter[targetId];

            if (player == null || Util.GetDistanceFromPlayer(player) > 2)
            {
                ChatManager.Tell(itemBundle.GetOwner(), "You moved too far away, cancelling your buffs.");
                machine.ChangeState(new BotFinishState(machine.CurrentState.GetItemBundle()));
                return;
            }
            else
            {
                ChatManager.Tell(itemBundle.GetOwner(), "Please select from the commands above to build your supply package.");
                ChatManager.Tell(itemBundle.GetOwner(), "You may issue multiple commands during your restock transaction!");
            }
                        
        }

        public void Exit(Machine machine) {
            ChatManager.RaiseChatCommandEvent -= new EventHandler<ChatCommandEventArgs>(ChatManager_ChatCommand);

        }

        void ChatManager_ChatCommand(object sender, ChatCommandEventArgs e) {
            if (Config.Stock.RestockCommands().ContainsKey(e.Command))
            {

                foreach (var stockItem in Config.Stock.StockItems.Value)
                {
                    var parts = stockItem.Split('|');
                    if (parts.Length != 4) continue;

                    if (e.Command == parts[0])
                    {
                    
                        var itemName = parts[1];
                        var stacksize = int.Parse(parts[2]);

                        foreach(var item in CoreManager.Current.WorldFilter.GetInventory())
                        {

                            var countItemStack = Convert.ToInt32(item.Values((LongValueKey)Decal.Interop.Filters.LongValueKey.keyStackCount));

                            if (countItemStack == 0) { countItemStack = 1; }

                            if (Util.GetObjectName(item.Id) == itemName && countItemStack >= stacksize)
                            {
                                Util.WriteToChat(itemName + " found in inventory. Attempting to give " + stacksize + ".");

                                if(stacksize == countItemStack)
                                {
                                    CoreManager.Current.Actions.GiveItem(item.Id, targetId);
                                    startTime = DateTime.UtcNow;
                                    restocked = true;
                                    gaveItem = true;
                                    return;
                                }
                                else
                                {
                                    CoreManager.Current.Actions.SelectItem(item.Id);
                                    CoreManager.Current.Actions.SelectedStackCount = stacksize;
                                    CoreManager.Current.Actions.GiveItem(item.Id, targetId);
                                    startTime = DateTime.UtcNow;
                                    restocked = true;
                                    gaveItem = true;
                                    return;
                                }
                            }
                        }
                        if (!gaveItem)
                        {
                            ChatManager.Tell(itemBundle.GetOwner(), "I am currently out of that item. Please try a different selection.");
                        }
                        else
                        {
                            gaveItem = false;
                        }
                    }
                }
            }
        }

        DateTime startTime = DateTime.UtcNow;

        public void Think(Machine machine) {
            if (DateTime.UtcNow - startTime > TimeSpan.FromSeconds(10)) {
                if(restocked)
                {
                    ChatManager.Tell(itemBundle.GetOwner(), "You have completed your restock!");
                }
                else 
                {
                    ChatManager.Tell(itemBundle.GetOwner(), "Your request has timed out. Please try again.");
                }
                _machine.ChangeState(new BotFinishState(itemBundle));
            }
        }

        public ItemBundle GetItemBundle() {
            return itemBundle;
        }
    }
}
