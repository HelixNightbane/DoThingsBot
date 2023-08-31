using System;
using System.Text.RegularExpressions;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using DoThingsBot.Views;

namespace DoThingsBot
{
	public static class Globals
	{
		public static void Init(string pluginName, PluginHost host, CoreManager core)
		{
			PluginName = pluginName;

			Host = host;

			Core = core;
        }

		public static string PluginName { get; private set; }

		public static PluginHost Host { get; private set; }

        public static CoreManager Core { get; private set; }

        public static Stats.Stats Stats { get; set; }
        internal static MainView MainView { get; set; }
        public static ProfileManagerView ProfileManagerView { get; set; }
        public static StatsView StatsView { get; set; }
        public static DoThingsBot DoThingsBot { get; set; }

        // You determine that you have a 100 percent chance to succeed.
        // You (?<msg>determine that you have a (?<percent>.+)% chance to succeed. Continue?)
        // You have a 33.3% chance of using Black Garnet Salvage (100) on Green Jade Heavy Crossbow.
        public static readonly Regex PercentConfirmation = new Regex("^You (determine that you )?(?<msg>have a (?<percent>.+)(%| percent) .*)");
    }
}
