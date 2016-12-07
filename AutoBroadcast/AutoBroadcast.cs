/*
 * Original plugin by Scavenger.
 * 
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace AutoBroadcast
{
	[ApiVersion(1, 26)]
	public class AutoBroadcast : TerrariaPlugin
	{
		public override string Name { get { return "AutoBroadcast"; } }
		public override string Author { get { return "Maintained by Zaicon"; } }
		public override string Description { get { return "Automatically Broadcast a Message or Command every x seconds"; } }
		public override Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

		public string ConfigPath { get { return Path.Combine(TShock.SavePath, "AutoBroadcastConfig.json"); } }
		public ABConfig Config = new ABConfig();

		public AutoBroadcast(Main Game) : base(Game) { }

		static readonly Timer Update = new Timer(1000);
		public static bool ULock = false;
		public const int UpdateTimeout = 501;

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize, -5);
			ServerApi.Hooks.ServerChat.Register(this, OnChat);
            TShockAPI.Hooks.RegionHooks.RegionEntered += OnRegionEnter;
		}

		protected override void Dispose(bool Disposing)
		{
			if (Disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
                TShockAPI.Hooks.RegionHooks.RegionEntered -= OnRegionEnter;
				Update.Elapsed -= OnUpdate;
				Update.Stop();
			}
			base.Dispose(Disposing);
		}

		public void OnInitialize(EventArgs args)
		{
			Commands.ChatCommands.Add(new Command("abroadcast", AutoBC, "autobc"));

			try
			{
				Config = ABConfig.Read(ConfigPath).Write(ConfigPath);
			}
			catch (Exception ex)
			{
				Config = new ABConfig();
				TShock.Log.ConsoleError("[AutoBroadcast] An exception occurred while parsing the AutoBroadcast config!\n{0}".SFormat(ex.ToString()));
			}
			Update.Elapsed += OnUpdate;
			Update.Start();
		}

		public void AutoBC(CommandArgs args)
		{
			try
			{
				Config = ABConfig.Read(ConfigPath).Write(ConfigPath);
				args.Player.SendSuccessMessage("Successfully reloaded AutoBroadcast config!");
			}
			catch (Exception ex)
			{
				Config = new ABConfig();
				args.Player.SendWarningMessage("An exception occurred while parsing the AutoBroadcast config! check logs for more details!");
				TShock.Log.Error("[AutoBroadcast] An exception occurred while parsing the AutoBroadcast config!\n{0}".SFormat(ex.ToString()));
			}
		}

		#region Chat
		public void OnChat(ServerChatEventArgs args)
		{
			var Start = DateTime.Now;
			var PlayerGroup = TShock.Players[args.Who].Group.Name;

			lock (Config.Broadcasts)
				foreach (var broadcast in Config.Broadcasts)
				{
					string[] Groups = new string[0];
					string[] Messages = new string[0];
					float[] Colour = new float[0];

					if (Timeout(Start)) return;
					if (broadcast == null || !broadcast.Enabled || (!broadcast.Groups.Contains(PlayerGroup) && !broadcast.Groups.Contains("*"))) continue;

                    string[] msgs = broadcast.Messages;

                    for (int i = 0; i < msgs.Length; i++)
                    {
                        msgs[i] = msgs[i].Replace("{player}", TShock.Players[args.Who].Name);
                    }

                    foreach (string Word in broadcast.TriggerWords)
					{
						if (Timeout(Start)) return;
						if (args.Text.Contains(Word))
						{
							if (broadcast.TriggerToWholeGroup && broadcast.Groups.Length > 0)
							{
								Groups = broadcast.Groups;
							}
							Messages = broadcast.Messages;
							Colour = broadcast.ColorRGB;
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
						Groups = new string[1] { "*" };
					}

					if (Groups.Length > 0)
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
        public void OnRegionEnter(TShockAPI.Hooks.RegionHooks.RegionEnteredEventArgs args)
        {
            var Start = DateTime.Now;
            var PlayerGroup = args.Player.Group.Name;

            lock (Config.Broadcasts)
                foreach (Broadcast broadcast in Config.Broadcasts)
                {
                    if (Timeout(Start)) return;
                    if (broadcast == null || !broadcast.Enabled || !broadcast.Groups.Contains(PlayerGroup)) continue;

                    string[] msgs = broadcast.Messages;

                    for (int i = 0; i < msgs.Length; i++ )
                    {
                        msgs[i] = msgs[i].Replace("{player}", args.Player.Name);
                        msgs[i] = msgs[i].Replace("{region}", args.Player.CurrentRegion.Name);
                    }

                    foreach (string reg in broadcast.TriggerRegions)
                    {
                        if (args.Player.CurrentRegion.Name == reg)
                        {
                            if (broadcast.RegionTrigger == "all")
                                BroadcastToAll(msgs, broadcast.ColorRGB);
                            else if (broadcast.RegionTrigger == "region")
                                BroadcastToRegion(reg, msgs, broadcast.ColorRGB);
                            else if (broadcast.RegionTrigger == "self")
                                BroadcastToPlayer(args.Player.Index, msgs, broadcast.ColorRGB);

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
			lock (Config.Broadcasts)
			NumBroadcasts = Config.Broadcasts.Length;
			for (int i = 0; i < NumBroadcasts; i++)
			{
				if (Timeout(Start, UpdateTimeout)) return;
				string[] Groups = new string[0];
				string[] Messages = new string[0];
				float[] Colour = new float[0];

				lock (Config.Broadcasts)
				{
					if (Config.Broadcasts[i] == null || !Config.Broadcasts[i].Enabled || Config.Broadcasts[i].Interval < 1)
					{
						continue;
					}
					if (Config.Broadcasts[i].StartDelay > 0)
					{
						Config.Broadcasts[i].StartDelay--;
						continue;
					}
					Config.Broadcasts[i].StartDelay = Config.Broadcasts[i].Interval; // Start Delay used as Interval Countdown
					Groups = Config.Broadcasts[i].Groups;
					Messages = Config.Broadcasts[i].Messages;
					Colour = Config.Broadcasts[i].ColorRGB;
				}

                bool all = false;

                foreach (string j in Groups)
                {
                    if (j == "*")
                        all = true;
                }

                if (all)
                {
                    Groups = new string[1] { "*" };
                }

                if (Groups.Length > 0)
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

		public static void BroadcastToGroups(string[] Groups, string[] Messages, float[] Colour)
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
        public static void BroadcastToRegion(string region, string[] Messages, float[] Colour)
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
		public static void BroadcastToAll(string[] Messages, float[] Colour)
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
		public static void BroadcastToPlayer(int plr, string[] Messages, float[] Colour)
		{
			foreach (string Line in Messages)
			{
                if (Line.StartsWith(TShock.Config.CommandSpecifier) || Line.StartsWith(TShock.Config.CommandSilentSpecifier))
				{
					Commands.HandleCommand(TSPlayer.Server, Line);
				}
				else lock(TShock.Players)
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
