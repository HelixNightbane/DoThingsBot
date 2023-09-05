using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

using Mag.Shared.Settings;
using static DoThingsBot.Spells;
using DoThingsBot.Lib;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DoThingsBot {
    public class BotConfigChangedEventArgs : EventArgs {
    }

    static class Config {
        public static class Bot {
            public static  Setting<bool> Enabled;
            public static  Setting<int> DefaultHeading;
            public static  Setting<string> Location;
            public static  Setting<bool> RespondToUnknownCommands;
            public static  Setting<int> DontResendDuplicateMessagesWindow;
            public static  Setting<int> BuffRefreshTime;
            public static Setting<double> RecompVendorSellRate;
            public static Setting<bool> FastCastSelfBuffs;
            public static Setting<bool> AnnounceLowComponents;
            public static Setting<bool> AnnounceLowComponentsAfterJob;
            public static Setting<bool> EnableResetCommand;
            public static Setting<bool> EnableStickySpot;
            public static Setting<double> StickySpotMaxDistance;
            public static Setting<double> StickySpotNS;
            public static Setting<double> StickySpotEW;

            public static Setting<int> PrismaticTaperLowCount;
            public static Setting<int> LeadScarabLowCount;
            public static Setting<int> IronScarabLowCount;
            public static Setting<int> CopperScarabLowCount;
            public static Setting<int> SilverScarabLowCount;
            public static Setting<int> GoldScarabLowCount;
            public static Setting<int> PyrealScarabLowCount;
            public static Setting<int> PlatinumScarabLowCount;
            public static Setting<int> ManaScarabLowCount;

            public static Setting<int> DangerousMonsterLogoffDistance;
            public static Setting<List<int>> HarmlessMonsterWeenies;

            public static Setting<List<int>> InfiniteItemIds;

            static Bot() {
            }

            public static void Init() {
                Enabled = new Setting<bool>("Config/Bot/Enabled", "Enable the bot", false);
                DefaultHeading = new Setting<int>("Config/Bot/DefaultHeading", "Default heading while the bot is idle. 0-359. (0=North, 90=East, 180=South, 270=West)", 0);
                Location = new Setting<string>("Config/Bot/Location", "Where in Auberean is your bot? (eg: Holtburg, just east of the lifestone)", "Somewhere in Auberean");
                RespondToUnknownCommands = new Setting<bool>("Config/Bot/RespondToUnknownCommands", "Respond to unknown commands", true);
                DontResendDuplicateMessagesWindow = new Setting<int>("Config/Bot/DontResendDuplicateMessagesWindow", "Don't send repeat messages if they fall within this time window (in seconds)", 2);
                BuffRefreshTime = new Setting<int>("Config/Bot/BuffRefreshTime", "Refresh buffs if time left falls below this amount before a job request. (in minutes)", 5);
                RecompVendorSellRate = new Setting<double>("Config/Bot/RecompVendorSellRate", "The sell rate of the vendor you buy components from (eg treetop is 140% so this should be set to '1.4')", 1.4);
                FastCastSelfBuffs = new Setting<bool>("Config/Bot/FastCastSelfBuffs", "Enable fast casting of self buffs", false);
                AnnounceLowComponents = new Setting<bool>("Config/Bot/AnnounceLowComponents", "Replace announcements with low component warnings when you are low", true);
                AnnounceLowComponentsAfterJob = new Setting<bool>("Config/Bot/AnnounceLowComponentsAfterJob", "Announce low components after a job is finished and you are low", true);
                EnableResetCommand = new Setting<bool>("Config/Bot/EnableResetCommand", "Allow users to tell the bot 'reset' to force close the client (in case of being stuck)", false);
                EnableStickySpot = new Setting<bool>("Config/Bot/EnableStickySpot", "When enabled, the bot will attempt to stay within StickySpotMaxDistance of the bot starting position", true);
                StickySpotMaxDistance = new Setting<double>("Config/Bot/StickySpotMaxDistance", "Max distance in meters away from the bot starting position until it will attempt to navigate back to its starting position.", 1);
                StickySpotNS = new Setting<double>("Config/Bot/StickySpotNS", "NorthSouth coordinates for sticky spot.", 0);
                StickySpotEW = new Setting<double>("Config/Bot/StickySpotEW", "EastWest coordinates for sticky spot.", 0);

                PrismaticTaperLowCount = new Setting<int>("Config/Bot/PrismaticTaperLowCount", "Warn when Pristmatic Tapers fall below this amount (0 disables)", 100);
                LeadScarabLowCount = new Setting<int>("Config/Bot/LeadScarabLowCount", "Warn when Lead Scarabs fall below this amount (0 disables)", 0);
                IronScarabLowCount = new Setting<int>("Config/Bot/IronScarabLowCount", "Warn when Iron Scarabs fall below this amount (0 disables)", 0);
                CopperScarabLowCount = new Setting<int>("Config/Bot/CopperScarabLowCount", "Warn when Copper Scarabs fall below this amount (0 disables)", 10);
                SilverScarabLowCount = new Setting<int>("Config/Bot/SilverScarabLowCount", "Warn when Silver Scarabs fall below this amount (0 disables)", 10);
                GoldScarabLowCount = new Setting<int>("Config/Bot/GoldScarabLowCount", "Warn when Gold Scarabs fall below this amount (0 disables)", 10);
                PyrealScarabLowCount = new Setting<int>("Config/Bot/PyrealScarabLowCount", "Warn when Pyreal Scarabs fall below this amount (0 disables)", 10);
                PlatinumScarabLowCount = new Setting<int>("Config/Bot/PlatinumScarabLowCount", "Warn when Platinum Scarabs fall below this amount (0 disables)", 10);
                ManaScarabLowCount = new Setting<int>("Config/Bot/ManaScarabLowCount", "Warn when Mana Scarabs fall below this amount (0 disables)", 10);

                DangerousMonsterLogoffDistance = new Setting<int>("Config/Bot/DangerousMonsterLogoffDistance", "Log off when dangerous monsters are within this distance in meters (0 disables)", 20);

                var defaultHarmlessWeenies = new List<int> {
                    10950, // Aun Ralirea(10950)
                    11508, // Aun Elder Shaman(11508)
                    11509, // Aun Hunter(11509)
                    11510, // Aun Itealuan(11510)
                    11511, // Aun Nualuan(11511)
                    12698, // Sparring Golem(12698)
                    12704, // Carpenter Wasp(12704)
                    14, // Cow(14)
                    1617, // Amploth Lugian(1617)
                    1618, // Gigas Lugian(1618)
                    19256, // Young Banderling(19256)
                    19257, // Drudge Skulker(19257)
                    19258, // Drudge Slinker(19258)
                    19259, // Mite Scion(19259)
                    19260, // Mite Snippet(19260)
                    19261, // Creeper Mosswart(19261)
                    19262, // Young Mosswart(19262)
                    19263, // Gnawer Shreth(19263)
                    19288, // Bronze Statue of a Drudge(19288)
                    19291, // Bronze Statue of a Gromnie(19291)
                    19294, // Bronze Statue of a Mosswart(19294)
                    19297, // Bronze Statue of a Reedshark(19297)
                    19436, // Old Bones(19436)
                    205, // Obeloth Lugian(205)
                    206, // Lithos Lugian(206)
                    21162, // Stringent(21162)
                    21166, // Flake(21166)
                    220, // Brown Rat(220)
                    24284, // Lugian Juggernaut(24284)
                    24286, // Lugian Titan(24286)
                    24937, // Chicken(24937)
                    25283, // Rooster(25283)
                    2566, // Black Rabbit(2566)
                    2567, // Brown Rabbit(2567)
                    25709, // Bandit(25709)
                    25756, // Sam(25756)
                    26676, // Chick(26676)
                    26677, // Dire Mattie(26677)
                    28662, // Penguin(28662)
                    29332, // Young Olthoi(29332)
                    29333, // Thieving Thrungus(29333)
                    29489, // Sir Belfelor(29489)
                    29490, // Sir Coretto(29490)
                    29491, // Guard(29491)
                    29504, // Red Bull of Sanamar(29504)
                    35273, // Tower Guardian(35273)
                    4125, // Pile O' Bones(4125)
                    4126, // Accursed Miner(4126)
                    4131, // Tan Rat(4131)
                    4132, // Russet Rat(4132)
                    5, // Laigus Lugian(5)
                    5429, // Desert Rabbit(5429)
                    5687, // Alfrega the Reedshark(5687)
                    5705, // Flicker(5705)
                    5760, // Chilly the Snowman(5760)
                    5761, // Snowman(5761)
                    618, // Cow(618)
                    6382, // Static(6382)
                    70056, // Big Chief Hagra(70056)
                    7100, // Extas Lugian(7100)
                    7101, // Tiatus Lugian(7101)
                    7401, // Smith Ejan(7401)
                    8591, // Dark Revenant(8591)
                    8595, // Cursed Bones(8595)
                    949, // Red Rat(949)
                };

                HarmlessMonsterWeenies = new Setting<List<int>>("Config/Bot/HarmlessMonsterWeenies/wcid", "These monster wcids will be ignored by the dangerous monster logoff functionality", defaultHarmlessWeenies);

                InfiniteItemIds = new Setting<List<int>>("Config/Bot/Infinites/Item", "These item ids (of infinite rares) will be available for use to players", new List<int>());

                RecompVendorSellRate.Validate += ValidateVendorRate;
                DefaultHeading.Validate += ValidateHeading;
                DefaultHeading.Validate += ValidatePositiveNumber;
                RecompVendorSellRate.Validate += ValidatePositiveNumber;
                BuffRefreshTime.Validate += ValidatePositiveNumber;
                PrismaticTaperLowCount.Validate += ValidatePositiveNumber;
                LeadScarabLowCount.Validate += ValidatePositiveNumber;
                IronScarabLowCount.Validate += ValidatePositiveNumber;
                CopperScarabLowCount.Validate += ValidatePositiveNumber;
                SilverScarabLowCount.Validate += ValidatePositiveNumber;
                GoldScarabLowCount.Validate += ValidatePositiveNumber;
                PyrealScarabLowCount.Validate += ValidatePositiveNumber;
                PlatinumScarabLowCount.Validate += ValidatePositiveNumber;
                ManaScarabLowCount.Validate += ValidatePositiveNumber;
                DangerousMonsterLogoffDistance.Validate += ValidatePositiveNumber;
            }

            public static bool HasInfiniteLeather() {
                return InfiniteItemIds.Value.Any(i => {
                    var wo = CoreManager.Current.WorldFilter[i];
                    if (wo == null)
                        return false;
                    return wo.Name == "Infinite Leather";
                });
            }

            public static bool HasInfiniteRations() {
                return InfiniteItemIds.Value.Any(i => {
                    var wo = CoreManager.Current.WorldFilter[i];
                    if (wo == null)
                        return false;
                    if (wo.Name == "Infinite Elaborate Dried Rations")
                        return true;
                    if (wo.Name == "Infinite Simple Dried Rations")
                        return true;

                    return false;
                });
            }

            public static bool HasInfiniteDyes() {
                var re = new Regex("^Perennial (?<color>\\w+) Dye$");
                return InfiniteItemIds.Value.Any(i => {
                    var wo = CoreManager.Current.WorldFilter[i];
                    if (wo == null)
                        return false;

                    if (re.IsMatch(wo.Name))
                        return true;

                    return false;
                });
            }

            public static bool HasInfiniteDye(string v) {
                return InfiniteItemIds.Value.Any(i => {
                    var wo = CoreManager.Current.WorldFilter[i];
                    if (wo == null)
                        return false;

                    return wo.Name == v;
                });
            }

            public static List<string> InfiniteDyeColors() {
                var re = new Regex("^Perennial (?<color>\\w+) Dye$");
                var colors = new List<string>();

                InfiniteItemIds.Value.ForEach(i => {
                    var wo = CoreManager.Current.WorldFilter[i];
                    if (wo == null)
                        return;

                    if (re.IsMatch(wo.Name)) {
                        var match = re.Match(wo.Name);
                        colors.Add(match.Groups["color"].Value.ToLower());
                    }
                });

                return colors;
            }

            public static List<SpellClass> GetWantedIdleEnchantments() {
                return Buffs.Buffs.GetBotProfile("idle").familyIds;
            }

            public static List<SpellClass> GetWantedBuffEnchantments() {
                return Buffs.Buffs.GetBotProfile("buff").familyIds;
            }

            public static List<SpellClass> GetWantedTinkerEnchantments() {
                return Buffs.Buffs.GetBotProfile("tinker").familyIds;
            }

            public static List<SpellClass> GetWantedCraftingEnchantments() {
                return Buffs.Buffs.GetBotProfile("crafting").familyIds;
            }

            public static void SetComponentLowWarningLevel(string configKey, int value) {
                switch (configKey) {
                    case "PrismaticTaperLowCount":
                        PrismaticTaperLowCount.Value = value;
                        break;
                    case "LeadScarabLowCount":
                        LeadScarabLowCount.Value = value;
                        break;
                    case "IronScarabLowCount":
                        IronScarabLowCount.Value = value;
                        break;
                    case "CopperScarabLowCount":
                        CopperScarabLowCount.Value = value;
                        break;
                    case "SilverScarabLowCount":
                        SilverScarabLowCount.Value = value;
                        break;
                    case "GoldScarabLowCount":
                        GoldScarabLowCount.Value = value;
                        break;
                    case "PyrealScarabLowCount":
                        PyrealScarabLowCount.Value = value;
                        break;
                    case "PlatinumScarabLowCount":
                        PlatinumScarabLowCount.Value = value;
                        break;
                    case "ManaScarabLowCount":
                        ManaScarabLowCount.Value = value;
                        break;
                }
            }

            public static int GetComponentLowWarningLevel(string configKey) {
                switch (configKey) {
                    case "PrismaticTaperLowCount":
                        return PrismaticTaperLowCount.Value;
                    case "LeadScarabLowCount":
                        return LeadScarabLowCount.Value;
                    case "IronScarabLowCount":
                        return IronScarabLowCount.Value;
                    case "CopperScarabLowCount":
                        return CopperScarabLowCount.Value;
                    case "SilverScarabLowCount":
                        return SilverScarabLowCount.Value;
                    case "GoldScarabLowCount":
                        return GoldScarabLowCount.Value;
                    case "PyrealScarabLowCount":
                        return PyrealScarabLowCount.Value;
                    case "PlatinumScarabLowCount":
                        return PlatinumScarabLowCount.Value;
                    case "ManaScarabLowCount":
                        return ManaScarabLowCount.Value;
                }

                return 0;
            }

        }

        public static class Equipment {
            public static  Setting<List<int>> IdleEquipmentIds;
            public static  Setting<List<int>> BuffEquipmentIds;
            public static Setting<List<int>> TinkerEquipmentIds;
            public static Setting<List<int>> CraftEquipmentIds;
            public static Setting<List<int>> BrillEquipmentIds;

            static Equipment() {
                try {
                }
                catch (Exception e) { Util.LogException(e); }
            }

            public static void Init() {
                IdleEquipmentIds = new Setting<List<int>>("Config/Equipment/Idle/Item", "These item ids will be equipped when you are idle. (everything else will be unequipped)", new List<int>());
                BuffEquipmentIds = new Setting<List<int>>("Config/Equipment/Buffing/Item", "These item ids will be equipped when you are buffing. (everything else will be unequipped)", new List<int>());
                TinkerEquipmentIds = new Setting<List<int>>("Config/Equipment/Tinkering/Item", "These item ids will be equipped when you are tinkering. (everything else will be unequipped)", new List<int>());
                CraftEquipmentIds = new Setting<List<int>>("Config/Equipment/Crafting/Item", "These item ids will be equipped when you are crafting. (everything else will be unequipped)", new List<int>());
                BrillEquipmentIds = new Setting<List<int>>("Config/Equipment/Brill/Item", "These item ids will be equipped when you are casting Brilliance. (everything else will be unequipped)", new List<int>());
            }
        }

        public static class Announcements {
            public static  Setting<bool> Enabled;

            public static Setting<bool> EnableStatSpam;
            public static  Setting<string> StartupMessage;
            public static  Setting<int> SpamInterval;

            public static  Setting<List<string>> Messages;

            static Announcements() {
                try {
                }
                catch (Exception e) { Util.LogException(e); }
            }

            public static void Init() {
                Enabled = new Setting<bool>("Config/Announcements/Enabled", "Enable startup / periodic announcements", true);
                EnableStatSpam = new Setting<bool>("Config/Announcements/EnableStatSpam", "Enable stat spam in local chat", true);

                StartupMessage = new Setting<string>("Config/Announcements/StartupMessage", "Puts a message/command into the chatbox when the bot starts (leave blank for none)", "DoThingsBot Online. I can tinker/buff/summon portals. Tell me 'help' to get started.");
                SpamInterval = new Setting<int>("Config/Announcements/SpamInterval", "The interval in minutes that announcements are sent out.", 15);

                var defaultMessages = new List<string> {
                        "I'm a DoThingsBot. I can tinker/buff/summon portals. Tell me 'help' to get started.",
                        "I can buff you! Tell me 'profiles' to see what I can do.",
                        "I can tinker your items, just stand nearby and tell me 'tinker'.",
                        "I can summon portals! Tell me 'whereto' to see the details.",
                        "Is your character on treestats? Just tell me 'buff' and I'll know what to buff you with.",
                        "Tell me 'stats' to see bot stats about your character."
                    };

                Messages = new Setting<List<string>>("Config/Announcements/Spam/Message", "Announcements go here. It will spam every `Config/Announcements/SpamInterval` seconds.", defaultMessages);
            }
        }

        public static class Portals {
            public static Setting<bool> Enabled;

            public static Setting<string> PrimaryPortalTieLocation;
            public static Setting<int> PrimaryPortalHeading;
            public static Setting<string> PrimaryPortalExtraCommand;

            public static Setting<string> SecondaryPortalTieLocation;
            public static Setting<int> SecondaryPortalHeading;
            public static Setting<string> SecondaryPortalExtraCommand;

            public static Setting<List<string>> PortalGems;
            public static Setting<int> PortalGemLowCount;

            static Portals() {
            }

            public static void Init() {
                Enabled = new Setting<bool>("Config/Portals/Enabled", "Enable portal bot functionality", false);

                PrimaryPortalTieLocation = new Setting<string>("Config/Portals/PrimaryPortalTieLocation", "Your primary portal tie location (eg: Temple of Enlightenment)", "Somewhere");
                PrimaryPortalHeading = new Setting<int>("Config/Portals/PrimaryPortalHeading", "Heading while summoning your primary portal tie. 0-359. (0=North, 90=East, 180=South, 270=West)", 315);
                PrimaryPortalExtraCommand = new Setting<string>("Config/Portals/PrimaryPortalExtraCommand", "Bot will also respond to this command to summon primary portal (eg: tn)", "");

                SecondaryPortalTieLocation = new Setting<string>("Config/Portals/SecondaryPortalTieLocation", "Your secondary portal tie location (eg: Temple of Forgetfulness)", "Somewhere else");
                SecondaryPortalHeading = new Setting<int>("Config/Portals/SecondaryPortalHeading", "Heading while summoning your secondary portal tie. 0-359. (0=North, 90=East, 180=South, 270=West)", 45);
                SecondaryPortalExtraCommand = new Setting<string>("Config/Portals/SecondaryPortalExtraCommand", "Bot will also respond to this command to summon primary portal (eg: tn)", "");

                PortalGems = new Setting<List<string>>("Config/Portals/PortalGems/Gem", "Portal Gem summoning commands", new List<string>());
                PortalGemLowCount = new Setting<int>("Config/Portals/PortalGemLowCount", "Portal Gem low count, 0 disables. Will spam when low.", 5);

                PrimaryPortalHeading.Validate += ValidateHeading;
                SecondaryPortalHeading.Validate += ValidateHeading;
            }

            internal static List<string> GetUniqueGemNames() {
                var names = new List<string>();

                foreach (var gem in PortalGems.Value) {
                    var name = gem.Split('|')[1];
                    if (!names.Contains(name)) {
                        names.Add(name);
                    }
                }

                return names;
            }

            public static string[] GetValidPortalGemCommands() {
                var commands = new List<string>();
                var portalGemCommands = PortalGemCommands();

                foreach (var command in portalGemCommands) {
                    commands.Add(command.Key);
                }

                return commands.ToArray();
            }

            public static Dictionary<string, PortalGem> PortalGemCommands() {
                var portalGemCommands = new Dictionary<string, PortalGem>();
                foreach (var portalGemEntry in PortalGems.Value) {
                    var parts = portalGemEntry.Split('|');
                    if (parts.Length != 4) continue;

                    if (!portalGemCommands.ContainsKey(parts[0])) {
                        int heading = 0;
                        int icon = 0;

                        if (Int32.TryParse(parts[2], out heading) && Int32.TryParse(parts[3], out icon)) {
                            portalGemCommands.Add(parts[0].ToLower(), new PortalGem(parts[1], heading, icon));
                        }
                    }
                }

                return portalGemCommands;
            }
        }
        public static class Stock
        {
            public static Setting<bool> Enabled;

            public static Setting<List<string>> StockItems;
            public static Setting<int> StockLowCount;

            public static Setting<bool> UIStockAllegianceOnly;

            static Stock()
            {
            }

            public static void Init()
            {
                Enabled = new Setting<bool>("Config/Stock/Enabled", "Enable Stock bot functionality", false);

                UIStockAllegianceOnly = new Setting<bool>("Config/Stock/UIStockAllegianceOnly", "Enable stat spam in local chat", true);

                StockItems = new Setting<List<string>>("Config/Stock/Items/StockItems", "Stock commands", new List<string>());
                StockLowCount = new Setting<int>("Config/Stock/StockLowCount", "Stock low count, 0 disables. Will spam when low.", 5);

            }

            internal static List<string> GetUniqueStockItemNames()
            {
                var names = new List<string>();

                foreach (var item in StockItems.Value)
                {
                    var name = item.Split('|')[1];
                    if (!names.Contains(name))
                    {
                        names.Add(name);
                    }
                }

                return names;
            }

            public static string[] GetValidRestockCommands()
            {
                var commands = new List<string>();
                var restockCommands = RestockCommands();

                foreach (var command in restockCommands)
                {
                    commands.Add(command.Key);
                }

                return commands.ToArray();
            }

            public static Dictionary<string, StockItems> RestockCommands()
            {
                var restockCommands = new Dictionary<string, StockItems>();
                foreach (var item in StockItems.Value)
                {
                    var parts = item.Split('|');
                    if (parts.Length != 4) continue;

                    if (!restockCommands.ContainsKey(parts[0]))
                    {
                        int size = 0;
                        int icon = 0;

                        if (Int32.TryParse(parts[2], out size) && Int32.TryParse(parts[3], out icon))
                        {
                            restockCommands.Add(parts[0].ToLower(), new StockItems(parts[1], size, icon));
                        }
                    }
                }

                return restockCommands;
            }

        }
        public static class BuffBot {
            public static Setting<bool> Enabled;
            public static Setting<bool> EnableSingleBuffs;
            public static Setting<bool> EnableTreeStatsBuffs;
            public static Setting<bool> AlwaysEnableBanes;
            public static Setting<int> LimitBuffOtherLevel;
            public static Setting<int> GetManaAt;
            public static Setting<int> GetStaminaAt;

            static BuffBot() {
            }

            public static void Init() {
                Enabled = new Setting<bool>("Config/BuffBot/Enabled", "Enable buff bot functionality", false);
                EnableTreeStatsBuffs = new Setting<bool>("Config/BuffBot/EnableTreeStatsBuffs", "Enable treestats buffs when someone tells you 'buffs'", true);
                EnableSingleBuffs = new Setting<bool>("Config/BuffBot/EnableSingleBuffs", "Enable single buffs (strength, focus, war magic, etc)", true);
                AlwaysEnableBanes = new Setting<bool>("Config/BuffBot/AlwaysEnableBanes", "Enable banes even when target doesn't have a shield equipped. (keep off on GDLE)", false);
                LimitBuffOtherLevel = new Setting<int>("Config/BuffBot/LimitBuffLevel", "Limit buff spell levels to this value", 7);
                GetManaAt = new Setting<int>("Config/BuffBot/GetManaAt", "Restore mana when it reaches this percentage", 50);
                GetStaminaAt = new Setting<int>("Config/BuffBot/GetStaminaAt", "Restore stamina when it reaches this percentage", 50);

                LimitBuffOtherLevel.Validate += ValidateSpellLevel;
                GetManaAt.Validate += ValidatePercentage;
                GetStaminaAt.Validate += ValidatePercentage;
            }
        }

        public static class CraftBot {
            public static Setting<bool> Enabled;
            public static Setting<bool> PauseSessionForOtherJobs;
            public static Setting<int> LimitCraftingSessionsToSeconds;
            public static Setting<bool> SkipMaxSuccessConfirmation;
            public static Setting<int> KeepEquipmentOnDelay;

            static CraftBot() {
            }

            public static void Init() {
                Enabled = new Setting<bool>("Config/CraftBot/Enabled", "Enable craft bot functionality", false);
                PauseSessionForOtherJobs = new Setting<bool>("Config/CraftBot/PauseSessionForOtherJobs", "If enabled, bot will pause crafting for tinkering/buffbot jobs and resume afterwards.", false);
                LimitCraftingSessionsToSeconds = new Setting<int>("Config/CraftBot/LimitCraftingSessionsToSeconds", "Limit each crafting session to this many seconds.", 300);
                SkipMaxSuccessConfirmation = new Setting<bool>("Config/CraftBot/SkipMaxSuccessConfirmation", "Skip user confirmation prompt if success chance is max.", true);
                KeepEquipmentOnDelay = new Setting<int>("Config/CraftBot/KeepEquipmentOnDelay", "How long to keep crafting equipment equipped after a job is finished (in seconds)", 0);

                LimitCraftingSessionsToSeconds.Validate += ValidatePositiveNumber;
                KeepEquipmentOnDelay.Validate += ValidatePositiveNumber;
            }
        }

        public static class BrillBot
        {
            public static Setting<bool> Enabled;

            static BrillBot()
            {
            }

            public static void Init()
            {
                Enabled = new Setting<bool>("Config/BrillBot/Enabled", "Enable Brilliance functionality", false);
            }
        }
            public static class Tinkering {
            public static Setting<bool> Enabled;
            public static Setting<int> KeepEquipmentOnDelay;
            public static Setting<bool> SkipMaxSuccessConfirmation;

            static Tinkering() {

            }

            public static void Init() {
                Enabled = new Setting<bool>("Config/Tinkering/Enabled", "Enable tinker bot functionality", true);
                KeepEquipmentOnDelay = new Setting<int>("Config/Tinkering/KeepEquipmentOnDelay", "How long to keep tinkering equipment equipped after a job is finished (in seconds)", 30);
                SkipMaxSuccessConfirmation = new Setting<bool>("Config/Tinkering/SkipMaxSuccessConfirmation", "Skip user confirmation prompt if success chance is max.", true);
                
                KeepEquipmentOnDelay.Validate += ValidatePositiveNumber;
            }
        }

        public static void Init() {
            try {
                Bot.Init();
                Announcements.Init();
                Portals.Init();
                Tinkering.Init();
                Equipment.Init();
                BuffBot.Init();
                CraftBot.Init();
                BrillBot.Init();
                Stock.Init();
            }
            catch (Exception e) { Util.LogException(e); }
        }

        private static void ValidatePositiveNumber(object sender, ValidateSettingEventArgs<int> e) {
            if (e.Value < 0) e.Invalidate("Should not be less than 0");
        }

        private static void ValidatePositiveNumber(object sender, ValidateSettingEventArgs<double> e) {
            if (e.Value < 0) e.Invalidate("Should not be less than 0");
        }

        private static void ValidatePositiveNumber(object sender, ValidateSettingEventArgs<float> e) {
            if (e.Value < 0) e.Invalidate("Should not be less than 0");
        }

        private static void ValidateHeading(object sender, ValidateSettingEventArgs<int> e) {
            if (e.Value < 0) e.Invalidate("Should not be less than 0");
            if (e.Value > 360) e.Invalidate("Should not be greater than 360");
        }

        private static void ValidateSpellLevel(object sender, ValidateSettingEventArgs<int> e) {
            if (e.Value < 1) e.Invalidate("Should not be less than 1");
            if (e.Value > 8) e.Invalidate("Should not be greater than 8");
        }

        private static void ValidateVendorRate(object sender, ValidateSettingEventArgs<double> e) {
            if (e.Value < 1) e.Invalidate("Should not be less than 1");
            if (e.Value > 2) e.Invalidate("Should not be greater than 2");
        }

        private static void ValidatePercentage(object sender, ValidateSettingEventArgs<int> e) {
            if (e.Value < 0) e.Invalidate("Should not be less than 0");
            if (e.Value > 100) e.Invalidate("Should not be greater than 100");
        }
    }
}
