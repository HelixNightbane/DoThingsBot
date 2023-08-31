using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Chat;
using System;
using System.Collections.Generic;
using System.Text;

namespace DoThingsBot.FSM.States {
    class BotCheckSkillsState : IBotState {
        public string Name { get => "BotCheckSkillsState"; }
        public ItemBundle itemBundle;

        public BotCheckSkillsState(ItemBundle items) {
            itemBundle = items;
        }

        public void Enter(Machine machine) {
            PrintSkillsMessage();
            itemBundle.SetCraftMode(CraftMode.None);
            itemBundle.SetEquipMode(EquipMode.Idle);
            machine.ChangeState(new BotEquipItemsState(GetItemBundle()));
        }

        public void Exit(Machine machine) {

        }

        void PrintSkillsMessage() {
            int weaponTinkeringSkill = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.WeaponTinkering];
            int magicItemTinkeringSkill = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.MagicItemTinkering];
            int armorTinkeringSkill = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.ArmorTinkering];
            int itemTinkeringSkill = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.ItemTinkering];

            ChatManager.Tell(itemBundle.GetOwner(), String.Format("My current (buffed) tinkering skills: Weapon: {0}, MagicItem: {1}, Armor: {2}, Item: {3}",
                weaponTinkeringSkill,
                magicItemTinkeringSkill,
                armorTinkeringSkill,
                itemTinkeringSkill
            ));
        }

        public void Think(Machine machine) {

        }

        public ItemBundle GetItemBundle() {
            return itemBundle;
        }
    }
}
