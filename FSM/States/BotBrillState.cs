using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using DoThingsBot.Buffs;
using System;
using System.Collections.Generic;

namespace DoThingsBot.FSM.States
{

    class BotBrillState : IBotState
    {
        public string Name { get => "BotBrillState"; }

        private int targetId = 0;
        private bool doneCasting = false;


        private DateTime lastThought = DateTime.UtcNow;
        private DateTime firstThought = DateTime.UtcNow;
        private DateTime lastCasted = DateTime.UtcNow;

        private ItemBundle itemBundle;
        private bool readyToCast = true;
        private int profileCount = 0;
        private bool fstest = Config.Equipment.BrillEquipmentIds.Value.Contains(-1854979780);

        public BotBrillState(ItemBundle items)
        {
            itemBundle = items;
            itemBundle.playerData.jobType = "brilliance";
        }

        public void Enter(Machine machine)
        {
            foreach (var obj in CoreManager.Current.WorldFilter.GetByName(itemBundle.GetOwner()))
            {
                if (obj.ObjectClass == ObjectClass.Player)
                {
                    targetId = obj.Id;
                }
            }

            var player = CoreManager.Current.WorldFilter[targetId];

            if (player == null)
            {
                machine.ChangeState(new BotFinishState(machine.CurrentState.GetItemBundle()));
                return;
            }

            itemBundle.SetOwner(player.Name);

            if (itemBundle.WasPaused)
            {
                ChatManager.Tell(itemBundle.GetOwner(), $"I am resuming your Brilliance buff.");
            }
            else
            {
                return;


            }

            CoreManager.Current.ChatBoxMessage += new EventHandler<ChatTextInterceptEventArgs>(Current_ChatBoxMessage);
        }

        public void Exit(Machine machine)
        {
            CoreManager.Current.ChatBoxMessage -= new EventHandler<ChatTextInterceptEventArgs>(Current_ChatBoxMessage);
            CoreManager.Current.Actions.FaceHeading(Config.Bot.DefaultHeading.Value, true);
        }

        void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            try
            {
                if (Util.IsChat(e.Text, Util.ChatFlags.PlayerTellsYou))
                {
                    string playerName = Util.GetSourceOfChat(e.Text);
                    string command = Util.GetMessageFromChat(e.Text);
                    if (playerName == itemBundle.GetOwner() && command == "cancel")
                    {
                        ChatManager.Tell(playerName, "Ok, cancelling your current Brilliance request.");
                        doneCasting = true;
                        return;
                    }
                }
            }

            catch (Exception ex) { Util.LogException(ex); }
        }

        public void Think(Machine machine)
        {
            if (doneCasting == false)
            {
                // enter magic combat state before casting buffs
                
                if (!CoreManager.Current.Actions.IsValidObject(targetId))
                {
                    ChatManager.Tell(itemBundle.GetOwner(), "You moved too far away, cancelling your Brilliance request.");
                    doneCasting = true;
                    return;
                }

                var player = CoreManager.Current.WorldFilter[targetId];

                if (player == null || Util.GetDistanceFromPlayer(player) > 30)
                {
                    ChatManager.Tell(itemBundle.GetOwner(), "You moved too far away, cancelling your Brilliance request.");
                    doneCasting = true;
                    return;
                }
            }

            if(!fstest && doneCasting == false) //Focusing Stone
            {
                ChatManager.Tell(itemBundle.GetOwner(), "I am unable to equip the Focusing Stone at this time.");
                doneCasting = true;
            }

            if (doneCasting == false)
            {

                if (!Util.EnsureCombatState(CombatState.Magic))
                {
                    lastCasted = DateTime.UtcNow;
                    return;
                }
            }

            if (doneCasting == false)
            {
                // cast Brilliance
                if (DateTime.UtcNow - lastCasted > TimeSpan.FromMilliseconds(3000))
                {
                    
                    ChatManager.Tell(itemBundle.GetOwner(), "Preparing to cast Brilliance on you.");
                    CoreManager.Current.Actions.SelectItem(targetId);
                    CoreManager.Current.Actions.UseItem(-1854979780, 1, targetId);
                    lastCasted = DateTime.UtcNow;
                    doneCasting = true;

                }
                return;
            }
            if (DateTime.UtcNow - lastCasted > TimeSpan.FromMilliseconds(5000) && doneCasting)
            {
                if (!Util.EnsureCombatState(CombatState.Peace)) return;
                itemBundle.SetEquipMode(EquipMode.Idle);
                machine.ChangeState(new BotFinishState(itemBundle));
            }
            return;


        }

        public ItemBundle GetItemBundle()
        {
            return itemBundle;
        }
    }
}
