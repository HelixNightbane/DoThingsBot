using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using System;
using System.Collections.Generic;

namespace DoThingsBot {
    public static class Spells {
        public static string[] MANA_RECOVERY_SPELLS = new string[]
        {
            "Stamina to Mana Self I",
            "Stamina to Mana Self II",
            "Stamina to Mana Self III",
            "Stamina to Mana Self IV",
            "Stamina to Mana Self V",
            "Stamina to Mana Self VI",
            "Meditative Trance",
            "Incantation of Stamina to Mana Self",
        };

        public static string[] STAMINA_RECOVERY_SPELLS = new string[]
        {
            "Revitalize Self I",
            "Revitalize Self II",
            "Revitalize Self III",
            "Revitalize Self IV",
            "Revitalize Self V",
            "Revitalize Self VI",
            "Robustification",
            "Incantation of Revitalize Self",
        };

        public enum SpellClass {
            UNKNOWN = 0,
            STRENGTH = 1,
            ENDURANCE = 3,
            QUICKNESS = 5,
            COORDINATION = 7,
            FOCUS = 9,
            WILLPOWER = 11,
            LIGHT_WEAPON_MASTERY = 17,
            MISSILE_WEAPON_MASTERY = 19,
            FINESSE_WEAPON_MASTERY = 23,
            HEAVY_WEAPON_MASTERY = 31,
            INVULNERABILITY = 37,
            IMPREGNABILITY = 39,
            MAGIC_RESISTANCE = 41,
            CREATURE_ENCHANTMENT_MASTERY = 43,
            ITEM_ENCHANTMENT_MASTERY = 45,
            LIFE_MAGIC_MASTERY = 47,
            WAR_MAGIC_MASTERY = 49,
            MANA_CONVERSION_MASTERY = 51,
            ARCANE_ENLIGHTENMENT = 53,
            ARMOR_TINKERING_EXPERTISE = 55,
            ITEM_TINKERING_EXPERTISE = 57,
            MAGIC_ITEM_TINKERING_EXPERTISE = 59,
            WEAPON_TINKERING_EXPERTISE = 61,
            MONSTER_ATTUNEMENT = 63,
            DECEPTION_MASTERY = 65,
            HEALING_MASTERY = 67,
            JUMPING_MASTERY = 69,
            LEADERSHIP_MASTERY = 71,
            LOCKPICK_MASTERY = 73,
            FEALTY = 75,
            SPRINT = 77,
            //REVITALIZE = 81,
            //MANA_BOOST = 83,
            REGENERATION = 93,
            REJUVENATION = 95,
            MANA_RENEWAL = 97,
            ACID_PROTECTION = 101,
            BLUDGEONING_PROTECTION = 103,
            COLD_PROTECTION = 105,
            LIGHTNING_PROTECTION = 107,
            FIRE_PROTECTION = 109,
            PIERCING_PROTECTION = 111,
            BLADE_PROTECTION = 113,
            ARMOR = 115,
            AURA_OF_HEART_SEEKER = 152,
            AURA_OF_BLOOD_DRINKER = 154,
            AURA_OF_DEFENDER = 156,
            AURA_OF_SWIFT_KILLER = 158,
            IMPENETRABILITY = 160,
            ACID_BANE = 162,
            BLUDGEON_BANE = 164,
            FROST_BANE = 166,
            LIGHTNING_BANE = 168,
            FLAME_BANE = 170,
            PIERCING_BANE = 172,
            BLADE_BANE = 174,
            AURA_OF_HERMETIC_LINK = 195,
            PERSON_ATTUNEMENT = 205,
            COOKING_MASTERY = 216,
            FLETCHING_MASTERY = 218,
            ALCHEMY_MASTERY = 221,
            ARCANUM_SALVAGING = 435,
            TWO_HANDED_COMBAT_MASTERY = 593,
            VOID_MAGIC_MASTERY = 645,
            DIRTY_FIGHTING_MASTERY = 665,
            DUAL_WIELD_MASTERY = 668,
            RECKLESSNESS_MASTERY = 671,
            SHIELD_MASTERY = 674,
            SNEAK_ATTACK_MASTERY = 677,
            AURA_OF_SPIRIT_DRINKER = 695,
            SUMMONING_MASTERY = 696,
        }

        public static int GetNextSpellIdToRefresh(List<string> spellNames) {
            foreach (var name in spellNames) {
                if (DoesSpellNeedRefresh(name)) {
                    return GetIdFromName(name);
                }
            }

            return 0;
        }

        public static bool DoesAnySpellNeedRefresh(List<string> spellNames) {
            foreach (var name in spellNames) {
                if (DoesSpellNeedRefresh(name)) {
                    return true;
                }
            }

            return false;
        }

        public static bool DoesAnySpellNeedRefresh(List<SpellClass> spells) {
            foreach (var spellFamily in spells) {
                var spell = Spells.GetBestKnownSpellByClass(spellFamily, true);

                if (spell == null) continue;

                if (DoesSpellNeedRefresh(spell.Name)) {
                    return true;
                }
            }

            return false;
        }

        public static bool DoesSpellNeedRefresh(string spellName) {
            try {
                int spellId = GetIdFromName(spellName);
                var enchantments = CoreManager.Current.CharacterFilter.Enchantments;
                
                if (!CoreManager.Current.CharacterFilter.SpellBook.Contains(spellId)) {
                    Util.WriteToChat(String.Format("I don't know this spell: {0} ({1})", spellName, spellId));
                    return false;
                }

                foreach (var enchantment in enchantments) {
                    string enchantmentName = Spells.GetNameFromId(enchantment.SpellId);

                    // TODO: Don't buff if equipment contains the same buff
                    if (enchantment.Duration == -1) continue;

                    if (enchantmentName == spellName) {
                        if (enchantment.Expires - DateTime.Now < TimeSpan.FromMinutes(Config.Bot.BuffRefreshTime.Value)) {
                            return true;
                        }

                        return false;
                    }
                }

                return true;
            }
            catch (Exception e) { Util.LogException(e); }

            return false;
        }

        public static Spell GetSpell(int spellId) {
            FileService fs = CoreManager.Current.Filter<FileService>();

            return fs.SpellTable.GetById(spellId);
        }

        public static bool HasComponents(Decal.Filters.Spell spell) {
            if (!HasFociFor(spell.School)) return false;
            if (Util.GetItemCount("Prismatic Taper") == 0) return false;
            if (!HasScarabsFor(spell)) return false;

            return true;
        }

        private static bool HasScarabsFor(Decal.Filters.Spell spell) {
            string scarabNeeded = "";
            var spellLevel = GetSpellLevel(spell);

            switch (spellLevel) {
                case 1:
                    scarabNeeded = "Lead Scarab";
                    break;
                case 2:
                    scarabNeeded = "Iron Scarab";
                    break;
                case 3:
                    scarabNeeded = "Copper Scarab";
                    break;
                case 4:
                    scarabNeeded = "Silver Scarab";
                    break;
                case 5:
                    scarabNeeded = "Gold Scarab";
                    break;
                case 6:
                    scarabNeeded = "Pyreal Scarab";
                    break;
                case 7:
                    scarabNeeded = "Platinum Scarab";
                    break;
                case 8:
                    scarabNeeded = "Mana Scarab";
                    break;
            }

            return Util.GetItemCount(scarabNeeded) > 0;
        }

        private static bool HasFociFor(SpellSchool school) {
            var fociName = "Unknown";

            switch (school.ToString()) {
                case "Creature Enchantment":
                    fociName = "Foci of Enchantment";
                    if (Globals.Core.CharacterFilter.GetCharProperty((int)Augmentations.InfusedCreature) == 1) return true;
                    break;

                case "Life Magic":
                    fociName = "Foci of Verdancy";
                    if (Globals.Core.CharacterFilter.GetCharProperty((int)Augmentations.InfusedLife) == 1) return true;
                    break;

                case "Item Enchantment":
                    fociName = "Foci of Artifice";
                    if (Globals.Core.CharacterFilter.GetCharProperty((int)Augmentations.InfusedItem) == 1) return true;
                    break;

                case "War Magic":
                    fociName = "Foci of Strife";
                    if (Globals.Core.CharacterFilter.GetCharProperty((int)Augmentations.InfusedWar) == 1) return true;
                    break;

                case "Void Magic":
                    fociName = "Foci of Shadow";
                    break;
            }

            return Util.HasItem(fociName);
        }

        // this seems wrong
        private static Dictionary<string, int> _spellLevels = new Dictionary<string, int>();
        public static int GetSpellLevel(Spell spell) {
            if (_spellLevels.ContainsKey(spell.Name)) return _spellLevels[spell.Name];

            var level = 7;

            if (spell.Name.EndsWith(" I")) level = 1;
            if (spell.Name.EndsWith(" II")) level = 2;
            if (spell.Name.EndsWith(" III")) level = 3;
            if (spell.Name.EndsWith(" IV")) level = 4;
            if (spell.Name.EndsWith(" V")) level = 5;
            if (spell.Name.EndsWith(" VI")) level = 6;
            if (spell.Name.EndsWith(" VII")) level = 7;
            if (spell.Name.EndsWith(" VIII")) level = 8;
            if (spell.Name.StartsWith("Incantation ")) level = 8;
            if (spell.Name.StartsWith("Aura of Incantation ")) level = 8;

            _spellLevels[spell.Name] = level;

            return _spellLevels[spell.Name];
        }

        public static bool CanCast(Decal.Filters.Spell spell) {
            // is this spell known?
            if (!CoreManager.Current.CharacterFilter.SpellBook.Contains(spell.Id)) {
                return false;
            }

            // has skill?
            if (!HasSkillToCast(spell)) return false;
            
            // has components?
            if (!HasComponents(spell)) return false;
            return true;
        }

        private static bool HasSkillToCast(Spell spell) {
            var currentSkill = 0;

            switch (spell.School.ToString()) {
                case "Creature Enchantment":
                    if (!CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.CreatureEnchantment].Known) return false;
                    currentSkill = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.CreatureEnchantment];
                    break;

                case "Life Magic":
                    if (!CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.LifeMagic].Known) return false;
                    currentSkill = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.LifeMagic];
                    break;

                case "Item Enchantment":
                    if (!CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.ItemEnchantment].Known) return false;
                    currentSkill = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.ItemEnchantment];
                    break;

                case "War Magic":
                    if (!CoreManager.Current.CharacterFilter.Skills[CharFilterSkillType.WarMagic].Known) return false;
                    currentSkill = CoreManager.Current.CharacterFilter.EffectiveSkill[CharFilterSkillType.WarMagic];
                    break;
            }

            // enough skill?
            if (currentSkill < spell.Difficulty - 10) return false;

            return true;
        }

        public static Spell GetBestStaminaRecoverySpell(bool isSelf) {
            return GetBestKnownSpellFrom(STAMINA_RECOVERY_SPELLS);
        }

        public static Spell GetBestManaRecoverySpell(bool isSelf) {
            return GetBestKnownSpellFrom(MANA_RECOVERY_SPELLS);
        }

        private static Spell GetBestKnownSpellFrom(string[] spells) {
            Spell bestSpell = null;
            try {
                FileService fs = CoreManager.Current.Filter<FileService>();

                foreach (var spellName in spells) {
                    var spell = fs.SpellTable.GetByName(spellName);

                    if (spell == null) {
                        Util.WriteToChat("Could not find spell: " + spellName);
                        continue;
                    }

                    if (CanCast(spell) && (bestSpell == null || (spell.Difficulty > bestSpell.Difficulty))) {
                        bestSpell = spell;
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }

            return bestSpell;
        }

        public static Spell GetBestKnownSpellByClass(SpellClass spellClass, bool isSelf, int levelLimit=999) {
            Spell bestSpell = null;
            FileService fs = CoreManager.Current.Filter<FileService>();

            foreach (var spellId in CoreManager.Current.CharacterFilter.SpellBook) {
                var spell = fs.SpellTable.GetById(spellId);

                if (isSelf != spell.IsUntargetted) continue;
                if (spell.IsFellowship) continue;

                var spellLevel = GetSpellLevel(spell);

                if (spell.Family == (int)spellClass && spellLevel <= levelLimit && CanCast(spell)) {
                    if (bestSpell == null) {
                        bestSpell = spell;
                    }
                    else if (spell.Difficulty > bestSpell.Difficulty) {
                        bestSpell = spell;
                    }
                }
            }

            return bestSpell;
        }

        private static Dictionary<SpellClass, Spell> exampleSpellClassCache = new Dictionary<SpellClass, Spell>();

        internal static Spell GetExampleSpellByClass(SpellClass family) {
            Spell exampleSpell = null;
            try {
                if (exampleSpellClassCache.ContainsKey(family)) {
                    return exampleSpellClassCache[family];
                }

                FileService fs = CoreManager.Current.Filter<FileService>();

                for (var i=0; i < fs.SpellTable.Length; i++) {
                    var spell = fs.SpellTable[i];

                    if (spell.Family != (int)family) continue;

                    if ((exampleSpell == null || (spell.Difficulty < exampleSpell.Difficulty))) {
                        exampleSpell = spell;
                    }
                }
            }
            catch (Exception ex) { Util.LogException(ex); }

            if (exampleSpell != null && !exampleSpellClassCache.ContainsKey(family)) {
                exampleSpellClassCache[family] = exampleSpell;
            }

            return exampleSpell;
        }

        public static string GetNameFromId(int id) {
            FileService fs = CoreManager.Current.Filter<FileService>();
            var spell = fs.SpellTable.GetById(id);

            if (spell != null) {
                return spell.Name;
            }

            return null;
        }

        public static int GetIdFromName(string name) {
            FileService fs = CoreManager.Current.Filter<FileService>();
            var spell = fs.SpellTable.GetByName(name);

            if (spell != null) {
                return spell.Id;
            }

            return 0;
        }

        public static bool EnsureEnoughStamina(bool readyToCast=true) {

            int effectiveStamina = CoreManager.Current.CharacterFilter.EffectiveVital[CharFilterVitalType.Stamina];
            int currentStamina = CoreManager.Current.CharacterFilter.Stamina;
            
            if (currentStamina < effectiveStamina * (Config.BuffBot.GetStaminaAt.Value / 100f)) {
                if (!readyToCast) return false;

                var spell = Spells.GetBestStaminaRecoverySpell(true);

                //Util.WriteToChat(String.Format("Stamina is: {0}/{1}", currentStamina, effectiveStamina));
                //Util.WriteToChat("Trying to cast: " + spell.Id + " : " + spell.Name);
                CoreManager.Current.Actions.CastSpell(spell.Id, CoreManager.Current.CharacterFilter.Id);
                return false;
            }

            return true;
        }

        public static bool EnsureEnoughMana(bool readyToCast=true) {
            int effectiveMana = CoreManager.Current.CharacterFilter.EffectiveVital[CharFilterVitalType.Mana];
            int currentMana = CoreManager.Current.CharacterFilter.Mana;

            // stam to mana
            if (currentMana < effectiveMana * (Config.BuffBot.GetManaAt.Value / 100f)) {
                if (!readyToCast) return false;

                var spell = Spells.GetBestManaRecoverySpell(true);

                //Util.WriteToChat(String.Format("Mana is: {0}/{1}", currentMana, effectiveMana));
                //Util.WriteToChat("Trying to cast: " + spell.Id + " : " + spell.Name);
                CoreManager.Current.Actions.CastSpell(spell.Id, CoreManager.Current.CharacterFilter.Id);
                return false;
            }

            return true;
        }

        public static bool CanUseRare() {
            FileService fs = CoreManager.Current.Filter<FileService>();

            for (var i = 0; i < CoreManager.Current.CharacterFilter.Enchantments.Count; i++) {
                var enchantment = CoreManager.Current.CharacterFilter.Enchantments[i];
                var spell = fs.SpellTable.GetById(enchantment.SpellId);
                if (spell.Name.Contains("Prodigal") && enchantment.Duration - enchantment.TimeRemaining < 60 * 3) {
                    return false;
                }
            }

            return true;
        }

        public static ulong GetTimeUntilCanUseRareAgain() {
            FileService fs = CoreManager.Current.Filter<FileService>();
            double shortest = double.MaxValue;

            for (var i = 0; i < CoreManager.Current.CharacterFilter.Enchantments.Count; i++) {
                var enchantment = CoreManager.Current.CharacterFilter.Enchantments[i];
                var spell = fs.SpellTable.GetById(enchantment.SpellId);
                if (spell.Name.Contains("Prodigal") && enchantment.Duration - enchantment.TimeRemaining < 60 * 3) {
                    if (enchantment.Duration - enchantment.TimeRemaining < shortest) {
                        shortest = enchantment.Duration - enchantment.TimeRemaining;
                        Util.WriteToChat($"{enchantment.Duration} {enchantment.TimeRemaining} {enchantment.Duration - enchantment.TimeRemaining}");
                    }
                }
            }

            return (60*3) - (ulong)shortest;
        }

        public static string itemEnchantmentAlreadyHadReason = "";
        public static bool HasItemEnchantmentsAlready(WorldObject wo) {
            if (wo != null) {
                FileService fs = CoreManager.Current.Filter<FileService>();

                List<int> existingEnchantments = new List<int>();

                for (var i = 0; i < CoreManager.Current.CharacterFilter.Enchantments.Count; i++) {
                    var spell = CoreManager.Current.CharacterFilter.Enchantments[i];
                    existingEnchantments.Add(spell.SpellId);
                }

                for (var i = 0; i < wo.SpellCount; i++) {
                    if (existingEnchantments.Contains(wo.Spell(i))) {
                        for (var x = 0; x < CoreManager.Current.CharacterFilter.Enchantments.Count; x++) {
                            var enchantment = CoreManager.Current.CharacterFilter.Enchantments[x];
                            if (enchantment.SpellId == wo.Spell(i)) {
                                var spell = fs.SpellTable.GetById(enchantment.SpellId);
                                var expires = Util.GetFriendlyTimeDifference(enchantment.Expires - DateTime.Now);

                                itemEnchantmentAlreadyHadReason = $"I already have {spell.Name} cast on me from a {wo.Name}, it expires in {expires}.";
                            }
                        }
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
