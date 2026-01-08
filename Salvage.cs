using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DoThingsBot {
    public enum TinkerType {
        Unknown = 0,
        Weapon = 1,
    }

    public enum Material {
        CERAMIC = 1,
        PORCELAIN = 2,
        CLOTH = 3,
        LINEN = 4,
        SATIN = 5,
        SILK = 6,
        VELVET = 7,
        WOOL = 8,
        GEM = 9,
        AGATE = 10,
        AMBER = 11,
        AMETHYST = 12,
        AQUAMARINE = 13,
        AZURITE = 14,
        BLACK_GARNET = 15,
        BLACK_OPAL = 16,
        BLOODSTONE = 17,
        CARNELIAN = 18,
        CITRINE = 19,
        DIAMOND = 20,
        EMERALD = 21,
        FIRE_OPAL = 22,
        GREEN_GARNET = 23,
        GREEN_JADE = 24,
        HEMATITE = 25,
        IMPERIAL_TOPAZ = 26,
        JET = 27,
        LAPIS_LAZULI = 28,
        LAVENDER_JADE = 29,
        MALACHITE = 30,
        MOONSTONE = 31,
        ONYX = 32,
        OPAL = 33,
        PERIDOT = 34,
        RED_GARNET = 35,
        RED_JADE = 36,
        ROSE_QUARTZ = 37,
        RUBY = 38,
        SAPPHIRE = 39,
        SMOKY_QUARTZ = 40,
        SUNSTONE = 41,
        TIGER_EYE = 42,
        TOURMALINE = 43,
        TURQUOISE = 44,
        WHITE_JADE = 45,
        WHITE_QUARTZ = 46,
        WHITE_SAPPHIRE = 47,
        YELLOW_GARNET = 48,
        YELLOW_TOPAZ = 49,
        ZIRCON = 50,
        IVORY = 51,
        LEATHER = 52,
        DILLO_HIDE = 53,
        GROMNIE_HIDE = 54,
        REEDSHARK_HIDE = 55,
        METAL = 56,
        BRASS = 57,
        BRONZE = 58,
        COPPER = 59,
        GOLD = 60,
        IRON = 61,
        PYREAL = 62,
        SILVER = 63,
        STEEL = 64,
        STONE = 65,
        ALABASTER = 66,
        GRANITE = 67,
        MARBLE = 68,
        OBSIDIAN = 69,
        SANDSTONE = 70,
        SERPENTINE = 71,
        WOOD = 72,
        EBONY = 73,
        MAHOGANY = 74,
        OAK = 75,
        PINE = 76,
        TEAK = 77,
    }

    public struct SalvageData {
        public TinkerType tinkerType;
        public string Name;
        public string MaterialName;

        public SalvageData(TinkerType type, string name) {
            tinkerType = type;
            Name = name;
            MaterialName = name;
        }
    }

    public static class Salvage {
        public static string GetName(WorldObject wo) {
            SalvageData d = GetFromWorldObject(wo);
            return String.Format("{0} {1} [w{2}]",
                d.Name,
                wo.Values(LongValueKey.UsesRemaining),
                Math.Round(wo.Values(DoubleValueKey.SalvageWorkmanship)*100)/100
                );
        }

        public static bool IsSalvageFP(WorldObject wo)
        {
            Match checkfp = Regex.Match(wo.Name, "(^Foolproof (?<Material>.+)||(?<Material>.+) Foolproof)");
            if (wo.ObjectClass == ObjectClass.Misc && checkfp.Success) return true;
            else
            {
                return false;
            }
        }

        public static bool IsSalvage(WorldObject wo)
        {
            if (wo.ObjectClass == ObjectClass.Salvage || IsSalvageFP(wo)) return true;
            
            else 
            { 
                return false;
            }
        }

        public static bool IsWeaponImbueSalvage(WorldObject wo) {
            if (!IsSalvage(wo)) return false;

            switch (wo.Values(LongValueKey.Material)) {
                case (int)Material.JET: return true;
                case (int)Material.EMERALD: return true;
                case (int)Material.IMPERIAL_TOPAZ: return true;
                case (int)Material.BLACK_GARNET: return true;
                case (int)Material.AQUAMARINE: return true;
                case (int)Material.RED_GARNET: return true;
                case (int)Material.WHITE_SAPPHIRE: return true;
                case (int)Material.BLACK_OPAL: return true;
                case (int)Material.FIRE_OPAL: return true;
                case (int)Material.SUNSTONE: return true;
                default: return false;
            }
        }

        public static bool IsMagicWeaponImbueSalvage(WorldObject wo) {
            if (!IsSalvage(wo)) return false;

            switch (wo.Values(LongValueKey.Material)) {
                case (int)Material.JET: return true;
                case (int)Material.EMERALD: return true;
                case (int)Material.IMPERIAL_TOPAZ: return true;
                case (int)Material.BLACK_GARNET: return true;
                case (int)Material.AQUAMARINE: return true;
                case (int)Material.RED_GARNET: return true;
                case (int)Material.WHITE_SAPPHIRE: return true;
                case (int)Material.BLACK_OPAL: return true;
                case (int)Material.FIRE_OPAL: return true;
                default: return false;
            }
        }

        public static bool IsArmorImbueSalvage(WorldObject wo) {
            if (!IsSalvage(wo)) return false;

            switch (wo.Values(LongValueKey.Material)) {
                case (int)Material.ALABASTER: return true;
                case (int)Material.PERIDOT: return true;
                case (int)Material.YELLOW_TOPAZ: return true;
                case (int)Material.ZIRCON: return true;
                default: return false;
            }
        }

        public static bool IsJewelryImbueSalvage(WorldObject wo) {
            if (!IsSalvage(wo)) return false;

            switch (wo.Values(LongValueKey.Material)) {
                case (int)Material.MALACHITE: return true;
                case (int)Material.LAVENDER_JADE: return true;
                case (int)Material.LAPIS_LAZULI: return true;
                case (int)Material.HEMATITE: return true;
                case (int)Material.AZURITE: return true;
                case (int)Material.AGATE: return true;
                case (int)Material.CARNELIAN: return true;
                case (int)Material.CITRINE: return true;
                case (int)Material.BLOODSTONE: return true;
                case (int)Material.ROSE_QUARTZ: return true;
                case (int)Material.RED_JADE: return true;
                case (int)Material.SMOKY_QUARTZ: return true;
                default: return false;
            }
        }

        public static bool IsImbueSalvage(WorldObject wo) {
            return (IsMagicWeaponImbueSalvage(wo) || IsWeaponImbueSalvage(wo) || IsArmorImbueSalvage(wo) || IsJewelryImbueSalvage(wo)) && !IsSalvageFP(wo);
        }
        public static bool IsImbueSalvageFP(WorldObject wo)
        {
            return (IsMagicWeaponImbueSalvage(wo) || IsWeaponImbueSalvage(wo) || IsArmorImbueSalvage(wo) || IsJewelryImbueSalvage(wo)) && IsSalvageFP(wo);
        }
        public static bool IsAbleToApplyToArmor(WorldObject wo) {
            if (!IsSalvage(wo)) return false;

            if (IsAbleToApplyToAnyTreasureGeneratedItem(wo) || IsArmorImbueSalvage(wo)) {
                return true;
            }

            switch (wo.Values(LongValueKey.Material)) {
                case (int)Material.ALABASTER: return true;
                case (int)Material.CERAMIC: return true;
                case (int)Material.BRONZE: return true;
                case (int)Material.DILLO_HIDE: return true;
                case (int)Material.REEDSHARK_HIDE: return true;
                case (int)Material.MARBLE: return true;
                case (int)Material.STEEL: return true;
                case (int)Material.WOOL: return true;
                default: return false;
            }
        }

        public static bool IsAbleToApplyToJewelry(WorldObject wo) {
            if (!IsSalvage(wo)) return false;

            if (IsAbleToApplyToAnyTreasureGeneratedItem(wo) || IsJewelryImbueSalvage(wo)) {
                return true;
            }

            switch (wo.Values(LongValueKey.Material)) {
                default: return false;
            }
        }

        public static bool IsAbleToApplyToAnyMagicWeapon(WorldObject wo) {
            if (!IsSalvage(wo)) return false;

            if (IsMagicWeaponImbueSalvage(wo) || IsAbleToApplyToAnyTreasureGeneratedItem(wo)) {
                return true;
            }

            switch (wo.Values(LongValueKey.Material)) {
                case (int)Material.OPAL: return true;
                case (int)Material.GREEN_GARNET: return true;
                case (int)Material.BRASS: return true;
                default: return false;
            }
        }

        public static bool IsAbleToApplyToAnyWeapon(WorldObject wo) {
            if (!IsSalvage(wo)) return false;

            if (IsWeaponImbueSalvage(wo) || IsAbleToApplyToAnyTreasureGeneratedItem(wo)) {
                return true;
            }

            switch (wo.Values(LongValueKey.Material)) {
                case (int)Material.BRASS: return true;
                default: return false;
            }
        }

        public static bool IsAbleToApplyToMeleeOrMissleWeapon(WorldObject wo) {
            if (!IsSalvage(wo)) return false;

            if (IsAbleToApplyToAnyWeapon(wo)) {
                return true;
            }

            switch (wo.Values(LongValueKey.Material)) {
                case (int)Material.OAK: return true;
                case (int)Material.SUNSTONE: return true;
                case (int)Material.BRASS: return true;
                default: return false;
            }
        }

        public static bool IsAbleToApplyToMeleeWeapon(WorldObject wo) {
            if (!IsSalvage(wo)) return false;

            if (IsAbleToApplyToAnyWeapon(wo) || IsAbleToApplyToMeleeOrMissleWeapon(wo)) {
                return true;
            }

            switch (wo.Values(LongValueKey.Material)) {
                case (int)Material.GRANITE: return true;
                case (int)Material.IRON: return true;
                case (int)Material.VELVET: return true;
                case (int)Material.BRASS: return true;
                default: return false;
            }
        }

        public static bool IsAbleToApplyToMissileWeapon(WorldObject wo) {
            if (!IsSalvage(wo)) return false;

            if (IsAbleToApplyToAnyWeapon(wo) || IsAbleToApplyToMeleeOrMissleWeapon(wo)) {
                return true;
            }

            switch (wo.Values(LongValueKey.Material)) {
                case (int)Material.MAHOGANY: return true;
                case (int)Material.BRASS: return true;
                default: return false;
            }
        }

        public static bool IsAbleToApplyToAnyTreasureGeneratedItem(WorldObject wo) {
            if (!IsSalvage(wo)) return false;

            switch (wo.Values(LongValueKey.Material)) {
                case (int)Material.LINEN: return true;
                case (int)Material.GOLD: return true;
                case (int)Material.EBONY: return true;
                case (int)Material.COPPER: return true;
                case (int)Material.SATIN: return true;
                case (int)Material.PORCELAIN: return true;
                case (int)Material.MOONSTONE: return true;
                case (int)Material.PINE: return true;
                case (int)Material.TEAK: return true;
                case (int)Material.SILVER: return true;
                case (int)Material.LEATHER: return true;
                case (int)Material.IVORY: return true;
                case (int)Material.SANDSTONE: return true;
                case (int)Material.SILK: return true;
                default: return false;
            }
        }

        public static SalvageData GetFromWorldObject(WorldObject wo) {
            switch (wo.Values(LongValueKey.Material)) {
                case 1: return new SalvageData(TinkerType.Unknown, "Ceramic");
                case 2: return new SalvageData(TinkerType.Unknown, "Porcelain");
                case 3: return new SalvageData(TinkerType.Unknown, "Cloth");
                case 4: return new SalvageData(TinkerType.Unknown, "Linen");
                case 5: return new SalvageData(TinkerType.Unknown, "Satin");
                case 6: return new SalvageData(TinkerType.Unknown, "Silk");
                case 7: return new SalvageData(TinkerType.Weapon, "Velvet");
                case 8: return new SalvageData(TinkerType.Unknown, "Wool");
                case 9: return new SalvageData(TinkerType.Unknown, "Gem");
                case 10: return new SalvageData(TinkerType.Unknown, "Agate");
                case 11: return new SalvageData(TinkerType.Unknown, "Amber");
                case 12: return new SalvageData(TinkerType.Unknown, "Amethyst");
                case 13: return new SalvageData(TinkerType.Weapon, "Aquamarine");
                case 14: return new SalvageData(TinkerType.Unknown, "Azurite");
                case 15: return new SalvageData(TinkerType.Weapon, "Black Garnet");
                case 16: return new SalvageData(TinkerType.Weapon, "Black Opal");
                case 17: return new SalvageData(TinkerType.Unknown, "Bloodstone");
                case 18: return new SalvageData(TinkerType.Unknown, "Carnelian");
                case 19: return new SalvageData(TinkerType.Unknown, "Citrine");
                case 20: return new SalvageData(TinkerType.Unknown, "Diamond");
                case 21: return new SalvageData(TinkerType.Weapon, "Emerald");
                case 22: return new SalvageData(TinkerType.Weapon, "Fire Opal");
                case 23: return new SalvageData(TinkerType.Unknown, "Green Garnet");
                case 24: return new SalvageData(TinkerType.Unknown, "Green Jade");
                case 25: return new SalvageData(TinkerType.Unknown, "Hematite");
                case 26: return new SalvageData(TinkerType.Weapon, "Imperial Topaz");
                case 27: return new SalvageData(TinkerType.Weapon, "Jet");
                case 28: return new SalvageData(TinkerType.Unknown, "Lapis Lazuli");
                case 29: return new SalvageData(TinkerType.Unknown, "Lavender Jade");
                case 30: return new SalvageData(TinkerType.Unknown, "Malachite");
                case 31: return new SalvageData(TinkerType.Unknown, "Moonstone");
                case 32: return new SalvageData(TinkerType.Unknown, "Onyx");
                case 33: return new SalvageData(TinkerType.Unknown, "Opal");
                case 34: return new SalvageData(TinkerType.Unknown, "Peridot");
                case 35: return new SalvageData(TinkerType.Weapon, "Red Garnet");
                case 36: return new SalvageData(TinkerType.Unknown, "Red Jade");
                case 37: return new SalvageData(TinkerType.Unknown, "Rose Quartz");
                case 38: return new SalvageData(TinkerType.Unknown, "Ruby");
                case 39: return new SalvageData(TinkerType.Unknown, "Sapphire");
                case 40: return new SalvageData(TinkerType.Unknown, "Smoky Quartz");
                case 41: return new SalvageData(TinkerType.Weapon, "Sunstone");
                case 42: return new SalvageData(TinkerType.Unknown, "Tiger Eye");
                case 43: return new SalvageData(TinkerType.Unknown, "Tourmaline");
                case 44: return new SalvageData(TinkerType.Unknown, "Turquoise");
                case 45: return new SalvageData(TinkerType.Unknown, "White Jade");
                case 46: return new SalvageData(TinkerType.Unknown, "White Quartz");
                case 47: return new SalvageData(TinkerType.Weapon, "White Sapphire");
                case 48: return new SalvageData(TinkerType.Unknown, "Yellow Garnet");
                case 49: return new SalvageData(TinkerType.Unknown, "Yellow Topaz");
                case 50: return new SalvageData(TinkerType.Unknown, "Zircon");
                case 51: return new SalvageData(TinkerType.Unknown, "Ivory");
                case 52: return new SalvageData(TinkerType.Unknown, "Leather");
                case 53: return new SalvageData(TinkerType.Unknown, "Dillo Hide");
                case 54: return new SalvageData(TinkerType.Unknown, "Gromnie Hide");
                case 55: return new SalvageData(TinkerType.Unknown, "Reed Shark Hide");
                case 56: return new SalvageData(TinkerType.Unknown, "Metal");
                case 57: return new SalvageData(TinkerType.Weapon, "Brass");
                case 58: return new SalvageData(TinkerType.Unknown, "Bronze");
                case 59: return new SalvageData(TinkerType.Unknown, "Copper");
                case 60: return new SalvageData(TinkerType.Unknown, "Gold");
                case 61: return new SalvageData(TinkerType.Weapon, "Iron");
                case 62: return new SalvageData(TinkerType.Unknown, "Pyreal");
                case 63: return new SalvageData(TinkerType.Unknown, "Silver");
                case 64: return new SalvageData(TinkerType.Unknown, "Steel");
                case 65: return new SalvageData(TinkerType.Unknown, "Stone");
                case 66: return new SalvageData(TinkerType.Unknown, "Alabaster");
                case 67: return new SalvageData(TinkerType.Weapon, "Granite");
                case 68: return new SalvageData(TinkerType.Unknown, "Marble");
                case 69: return new SalvageData(TinkerType.Unknown, "Obsidian");
                case 70: return new SalvageData(TinkerType.Unknown, "Sandstone");
                case 71: return new SalvageData(TinkerType.Unknown, "Serpentine");
                case 72: return new SalvageData(TinkerType.Unknown, "Wood");
                case 73: return new SalvageData(TinkerType.Unknown, "Ebony");
                case 74: return new SalvageData(TinkerType.Weapon, "Mahogany");
                case 75: return new SalvageData(TinkerType.Weapon, "Oak");
                case 76: return new SalvageData(TinkerType.Unknown, "Pine");
                case 77: return new SalvageData(TinkerType.Unknown, "Teak");

                default: return new SalvageData(TinkerType.Unknown, "Unkown");
            }
        }
    }
}
