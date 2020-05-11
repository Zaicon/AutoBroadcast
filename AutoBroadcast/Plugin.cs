/*
 * Original plugin by Scavenger.
 * 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace AutoBroadcast
{
	[ApiVersion(2, 1)]
	public class Plugin : TerrariaPlugin
	{
		public override string Name { get { return "AutoBroadcast"; } }
		public override string Author { get { return "Maintained by Zaicon"; } }
		public override string Description { get { return "Automatically Broadcast a Message or Command every x seconds"; } }
		public override Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

		public string ConfigPath { get { return Path.Combine(TShock.SavePath, "AutoBroadcastConfig.json"); } }
		//public ABConfig Config = new ABConfig();


		public Plugin(Main Game) : base(Game) { }

		static readonly Timer Update = new Timer(1000);
		public static bool ULock = false;
		public const int UpdateTimeout = 501;

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize, -5);
			ServerApi.Hooks.ServerChat.Register(this, OnChat);
			RegionHooks.RegionEntered += OnRegionEnter;
		}

		protected override void Dispose(bool Disposing)
		{
			if (Disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
				RegionHooks.RegionEntered -= OnRegionEnter;
				Update.Elapsed -= OnUpdate;
				Update.Stop();
			}
			base.Dispose(Disposing);
		}

		public void OnInitialize(EventArgs args)
		{
			Database.Connect();
			Database.ReloadBroadcasts();
			Update.Elapsed += OnUpdate;
			Update.Start();
			var commands = CommandFactory.CommandFactory.GetCommands(typeof(Plugin).Assembly);
			Commands.ChatCommands.AddRange(commands);
		}

		#region Chat
		public void OnChat(ServerChatEventArgs args)
		{
			var Start = DateTime.Now;
			var PlayerGroup = TShock.Players[args.Who].Group.Name;

			lock (Database.Broadcasts)
				foreach (var broadcast in Database.Broadcasts)
				{
					List<string> Groups = new List<string>();
					List<string> Messages = new List<string>();
					float[] Colour = new float[0];

					if (Timeout(Start)) return;
					if (broadcast == null || !broadcast.Enabled || (!broadcast.Groups.Contains(PlayerGroup) && !broadcast.Groups.Contains("*"))) continue;

					List<string> msgs = broadcast.Messages;

					for (int i = 0; i < msgs.Count; i++)
					{
						msgs[i] = msgs[i].Replace("{player}", TShock.Players[args.Who].Name);
					}

					foreach (string Word in broadcast.TriggerWords)
					{
						if (Timeout(Start)) return;
						if (args.Text.Contains(Word))
						{
							if (broadcast.TriggerToWholeGroup && broadcast.Groups.Count > 0)
							{
								Groups = broadcast.Groups;
							}
							Messages = broadcast.Messages;
							Colour = broadcast.Color;
							break;
						}
					}

					bool all = false;

					foreach (string i in Groups)
					{
						if (i == "*")
							all = true;
					}

					if (all)
					{
						Groups = new List<string>() { "*" };
					}

					if (Groups.Count > 0)
					{
						BroadcastToGroups(Groups, Messages, Colour);
					}
					else
					{
						BroadcastToPlayer(args.Who, Messages, Colour);
					}
				}
		}
		#endregion

		#region RegionEnter
		public void OnRegionEnter(RegionHooks.RegionEnteredEventArgs args)
		{
			var Start = DateTime.Now;
			var PlayerGroup = args.Player.Group.Name;

			lock (Database.Broadcasts)
				foreach (Broadcast broadcast in Database.Broadcasts)
				{
					if (Timeout(Start)) return;
					if (broadcast == null || !broadcast.Enabled || !broadcast.Groups.Contains(PlayerGroup)) continue;

					List<string> msgs = broadcast.Messages;

					for (int i = 0; i < msgs.Count; i++)
					{
						msgs[i] = msgs[i].Replace("{player}", args.Player.Name);
						msgs[i] = msgs[i].Replace("{region}", args.Player.CurrentRegion.Name);
					}

					foreach (string reg in broadcast.TriggerRegions)
					{
						if (args.Player.CurrentRegion.Name == reg)
						{
							if (broadcast.RegionTriggerTo == "all")
								BroadcastToAll(msgs, broadcast.Color);
							else if (broadcast.RegionTriggerTo == "region")
								BroadcastToRegion(reg, msgs, broadcast.Color);
							else if (broadcast.RegionTriggerTo == "self")
								BroadcastToPlayer(args.Player.Index, msgs, broadcast.Color);

						}
					}
				}
		}
		#endregion

		#region Update
		public void OnUpdate(object Sender, EventArgs e)
		{
			if (Main.worldID == 0) return;
			if (ULock) return;
			ULock = true;
			var Start = DateTime.Now;

			int NumBroadcasts = 0;
			lock (Database.Broadcasts)
				NumBroadcasts = Database.Broadcasts.Count;
			for (int i = 0; i < NumBroadcasts; i++)
			{
				if (Timeout(Start, UpdateTimeout)) 
					return;
				List<string> Groups = new List<string>();
				List<string> Messages = new List<string>();
				float[] Colour = new float[0];

				lock (Database.Broadcasts)
				{
					if (Database.Broadcasts[i] == null || !Database.Broadcasts[i].Enabled || Database.Broadcasts[i].Interval < 1)
					{
						continue;
					}
					if (Database.Broadcasts[i].StartDelay > 0)
					{
						Database.Broadcasts[i].StartDelay--;
						continue;
					}
					Database.Broadcasts[i].StartDelay = Database.Broadcasts[i].Interval; // Start Delay used as Interval Countdown
					Groups = Database.Broadcasts[i].Groups;
					Messages = Database.Broadcasts[i].Messages;
					Colour = Database.Broadcasts[i].Color;
				}

				bool all = false;

				foreach (string j in Groups)
				{
					if (j == "*")
						all = true;
				}

				if (all)
				{
					Groups = new List<string>() { "*" };
				}

				if (Groups.Count > 0)
				{
					BroadcastToGroups(Groups, Messages, Colour);
				}
				else
				{
					BroadcastToAll(Messages, Colour);
				}
			}
			ULock = false;
		}
		#endregion

		public static void BroadcastToGroups(List<string> Groups, List<string> Messages, float[] Colour)
		{
			foreach (string Line in Messages)
			{
				if (Line.StartsWith(TShock.Config.CommandSpecifier) || Line.StartsWith(TShock.Config.CommandSilentSpecifier))
				{
					Commands.HandleCommand(TSPlayer.Server, Line);
				}
				else
				{
					lock (TShock.Players)
						foreach (var player in TShock.Players)
						{
							if (player != null && (Groups.Contains(player.Group.Name) || Groups[0] == "*"))
							{
								string msg = Line;
								msg = msg.Replace("{player}", player.Name);

								player.SendMessage(msg, (byte)Colour[0], (byte)Colour[1], (byte)Colour[2]);
							}
						}
				}
			}
		}
		public static void BroadcastToRegion(string region, List<string> Messages, float[] Colour)
		{
			foreach (string Line in Messages)
			{
				if (Line.StartsWith(TShock.Config.CommandSpecifier) || Line.StartsWith(TShock.Config.CommandSilentSpecifier))
				{
					Commands.HandleCommand(TSPlayer.Server, Line);
				}
				else
				{
					var players = from TSPlayer plr in TShock.Players where plr != null && plr.CurrentRegion != null && plr.CurrentRegion.Name == region select plr;
					foreach (TSPlayer plr in players)
					{
						plr.SendMessage(Line, (byte)Colour[0], (byte)Colour[1], (byte)Colour[2]);
					}
				}
			}
		}
		public static void BroadcastToAll(List<string> Messages, float[] Colour)
		{
			foreach (string Line in Messages)
			{
				if (Line.StartsWith(TShock.Config.CommandSpecifier) || Line.StartsWith(TShock.Config.CommandSilentSpecifier))
				{
					Commands.HandleCommand(TSPlayer.Server, Line);
				}
				else
				{
					foreach (TSPlayer plr in TShock.Players)
					{
						if (plr != null)
						{
							string msg = Line;
							msg = msg.Replace("{player}", plr.Name);

							plr.SendMessage(msg, (byte)Colour[0], (byte)Colour[1], (byte)Colour[2]);
						}
					}
				}
			}
		}
		public static void BroadcastToPlayer(int plr, List<string> Messages, float[] Colour)
		{
			foreach (string Line in Messages)
			{
				if (Line.StartsWith(TShock.Config.CommandSpecifier) || Line.StartsWith(TShock.Config.CommandSilentSpecifier))
				{
					Commands.HandleCommand(TSPlayer.Server, Line);
				}
				else lock (TShock.Players)
					{
						string msg = Line;
						msg = msg.Replace("{player}", TShock.Players[plr].Name);
						TShock.Players[plr].SendMessage(msg, (byte)Colour[0], (byte)Colour[1], (byte)Colour[2]);
					}
			}
		}

		public static bool Timeout(DateTime Start, int ms = 500, bool warn = true)
		{
			bool ret = (DateTime.Now - Start).TotalMilliseconds >= ms;
			if (ms == UpdateTimeout && ret) ULock = false;
			if (warn && ret)
			{
				Console.WriteLine("Hook timeout detected in AutoBroadcast. You might want to report this.");
				TShock.Log.Error("Hook timeout detected in AutoBroadcast. You might want to report this.");
			}
			return ret;
		}
	}
}
