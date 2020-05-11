using CommandFactory;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TShockAPI;

namespace AutoBroadcast
{
	[Command("autobroadcast", "autobc", AllowServer = true, Permissions = new[] { "autobroadcast.manage" })]
	[CommandSyntax("/autobc <add/delete/info/list>")]
	[CommandDescription("A plugin to install message scheduled broadcasts to the server.")]
	public static class ABCommands
	{
		/// <summary>
		/// Creates a new Broadcast with set parameters.
		/// </summary>
		/// <param name="args"></param>
		[Command("add", "create", Permissions = new[] { "autobroadcast.manage" })]
		[CommandSyntax("/autobc add <broadcast name> <interval> <startDelay> Message here")]
		[CommandDescription("Creates a new broadcast with given attributes.")]
		public static void AddBroadcast(CommandArgs args)
		{

			if (args.Parameters.Count < 4)
				throw new CommandException("Invalid usage! Usage: /autobc add <broadcast name>  <interval> <startDelay> Message here");

			string broadcastName = args.Parameters[0];
			if (!int.TryParse(args.Parameters[1], out int interval))
				throw new CommandException("Invalid interval value! Enter interval in seconds. Example usage: /autobc add \"testbroadcast\" 60 5 Hello world!");

			if (!int.TryParse(args.Parameters[2], out int startDelay))
				throw new CommandException("Invalid startDelay value! Enter delay in seconds. Example usage: /autobc add \"testbroadcast\" 60 5 Hello world!");

			string message = string.Join(" ", args.Parameters.Skip(3));

			if (Utils.BroadcastExists(broadcastName))
				throw new CommandException("A Broadcast with that name already exists!");


			if (!Database.AddBroadcast(broadcastName, true,  new List<string> { message }, "255,255,255", interval, startDelay, new List<string>(), "none", new List<string>(), new List<string>() , false))
				args.Player.SendErrorMessage($"Failed to add Broadcast \"{broadcastName}\" to the database!");
			else
				args.Player.SendSuccessMessage($"Successfully added Broadcast \"{broadcastName}\" to the database!");

		}
		/// <summary>
		/// TShock command to delete a Broadcast.
		/// </summary>
		/// <param name="args"></param>
		[Command("delete", "del", Permissions = new[] { "autobroadcast.manage" })]
		[CommandSyntax("/autobc delete <broadcast name>")]
		[CommandDescription("Deletes an existing broadcast.")]
		public static void DeleteBroadcast(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
				throw new CommandException("Invalid usage! Usage: /autobc delete <broadcast name>");

			string broadcastName = args.Parameters[0];

			if (!Utils.BroadcastExists(broadcastName))
				throw new CommandException("A Broadcast with that name does not exist!");

			if (!Database.DeleteBroadcast(broadcastName))
				throw new CommandException("Failed to delete Broadcast due to a database error! Check logs/console for details!");
			else
			{
				args.Player.SendSuccessMessage($"Successfully deleted Broadcast \"{broadcastName}\"");
			}
		}

		/// <summary>
		/// TShock command to display info of a Broadcast.
		/// </summary>
		/// <param name="args"></param>
		[Command("info", Permissions = new[] { "autobroadcast.manage" })]
		[CommandSyntax("/autobc info <broadcast name>")]
		[CommandDescription("Prints information of an existing broadcast.")]
		public static void BroadcastInfo(CommandArgs args)
		{
			int pageParamIndex = 1;
			int pageNumber;
			
			if (args.Parameters.Count < 1)
				throw new CommandException("Invalid usage! Usage: /autobc info <broadcast name>");

			string broadcastName = args.Parameters[0];

			if (!Utils.BroadcastExists(broadcastName))
				throw new CommandException("A Broadcast with that name does not exist! Type /autobc list, for a list of Broadcasts.");

			Broadcast broadcast = Utils.GetBroadcastByName(broadcastName);

			string messages = string.Join("; ", broadcast.Messages);
			if (broadcast.Messages.Count > 1)
				messages = $"Enter /autobc listmessages \"{broadcastName}\" for the messages of this broadcast.";
			string triggerRegions = string.Join("; ", broadcast.TriggerRegions);
			/*if (broadcast.TriggerRegions.Count > 4)
				triggerRegions = $"Enter /autobc listregions \"{broadcastName}\" for the triggerRegions of this broadcast.";*/
			string groups = string.Join("; ", broadcast.Groups);
		/*	if (broadcast.TriggerRegions.Count > 5)
				groups = $"Enter /autobc listregions \"{broadcastName}\" for the triggerRegions of this broadcast.";*/
			string triggerWords = string.Join("; ", broadcast.TriggerWords);
		/*	if (broadcast.TriggerRegions.Count > 5)
				triggerRegions = $"Enter /autobc listregions \"{broadcastName}\" for the triggerRegions of this broadcast.";*/


			string[] broadcastInfo = new[]
			{

				$"Enabled: {(broadcast.Enabled ? "true" : "false")}",
				$"Interval: {broadcast.Interval}",
				$"StartDelay: {broadcast.StartDelay}",
				$"Color: {string.Join(", ", broadcast.Color)}",
				$"Message: {messages}",
				$"TriggerRegion: {triggerRegions}",
				$"RegionTriggerTo: {broadcast.RegionTriggerTo}",
				$"Groups: {groups}",
				$"triggerWords: {triggerWords}",
				$"TriggerToWholeGroup: {(broadcast.TriggerToWholeGroup ? "true" : "false")}",
			};

			
			if (!PaginationTools.TryParsePageNumber(args.Parameters, pageParamIndex, args.Player, out pageNumber))
				return;

			PaginationTools.SendPage(args.Player, pageNumber, broadcastInfo,
			  new PaginationTools.Settings
			  {
				  HeaderFormat = $"\"{broadcastName}\" Info ({{0}}/{{1}}):",
				  FooterFormat = $"Type /autobc info \"{broadcastName}\" {{0}} for more.",
				  MaxLinesPerPage = 5,
			  });
		}
		/// <summary>
		/// TShock command to list all Broadcasts from the memory storage.
		/// </summary>
		/// <param name="args"></param>
		[Command("list", Permissions = new[] { "autobroadcast.manage" })]
		[CommandSyntax("/autobroadcast list")]
		[CommandDescription("Lists existing broadcasts.")]
		public static void ListBroadcasts(CommandArgs args)
		{
			int pageParamIndex = 1;
			int pageNumber;

			List<string> broadcastNames = PaginationTools.BuildLinesFromTerms(Database.Broadcasts.Select(e => e.Name));

			if (!PaginationTools.TryParsePageNumber(args.Parameters, pageParamIndex, args.Player, out pageNumber))
				return;

			PaginationTools.SendPage(args.Player, pageNumber, broadcastNames,
			  new PaginationTools.Settings
			  {
				  HeaderFormat = "Broadcasts ({0}/{1}):",
				  FooterFormat = $"Type /autobc list {{0}} for more.",
				  MaxLinesPerPage = 4,
				  NothingToDisplayString = "There aren't any Broadcasts set."
			  });
		}
		/// <summary>
		/// Reloads the AutoBroadcast data from database.
		/// </summary>
		/// <param name="args"></param>
		[Command("reload", Permissions = new[] { "autobroadcast.manage" })]
		[CommandSyntax("/autobc reload")]
		[CommandDescription("Reloads Broadcasts from database.")]
		public static void ReloadBroadcasts(CommandArgs args)
		{
			if (!Database.ReloadBroadcasts())
				throw new CommandException("Failed to reload Broadcast from database!");

			args.Player.SendSuccessMessage("Reloaded Broadcasts from database!");
		}
		/// <summary>
		/// Toggles broadcast on/off.
		/// </summary>
		/// <param name="args"></param>
		[Command("enable", Permissions = new[] { "autobroadcast.manage" })]
		[CommandSyntax("/autobc enable <broadcast name>")]
		[CommandDescription("Toggles a broadcast on/off.")]
		public static void Enable (CommandArgs args)
		{
			if (args.Parameters.Count < 1)
				throw new CommandException("Invalid usage! Usage: /autobc enable <broadcast name>");
			string broadcastName = args.Parameters[0];

			if (!Utils.BroadcastExists(broadcastName))
				throw new CommandException("A Broadcast with that name does not exist! Type /autobc list, for a list of Broadcasts.");

			Broadcast broadcast = Utils.GetBroadcastByName(broadcastName);

			if (!Database.UpdateBroadcast(broadcast.Name, !broadcast.Enabled, broadcast.Messages, string.Join(", ", broadcast.Color), broadcast.Interval, broadcast.StartDelay, broadcast.TriggerRegions, 
				broadcast.RegionTriggerTo, broadcast.Groups, broadcast.TriggerWords, broadcast.TriggerToWholeGroup ? true : false))
				throw new CommandException($"Failed turn {(broadcast.Enabled ? "on" : "off")} of Broadcast \"{broadcast.Name}\" due to a database error.");

			broadcast.Enabled = !broadcast.Enabled;
			args.Player.SendSuccessMessage($"Successfully turned {(broadcast.Enabled ? "on" : "off")} Broadcast \"{broadcast.Name}\"!");
		}

		public static string[] TweakOptions = new string[]
		{
			"-color", "-interval", "-startdelay", "-triggerregions", "-regiontriggerto", "-groups", "-triggerwords", "-triggertowholegroup"
		};

		[Command("set", Permissions = new[] { "autobroadcast.set" })]
		[CommandSyntax("/autobc set <broadcast name> [flags]")]
		[CommandDescription("Sets variables of a broadcast")]
		public static void Set(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
				throw new CommandException("Invalid usage! Usage: /autobc set <broadcast name> [flags] \nType /autobc listflags for a list of flags.");
			string broadcastName = args.Parameters[0];

			if (!Utils.BroadcastExists(broadcastName))
				throw new CommandException("A Broadcast with that name does not exist! Type /autobc list, for a list of Broadcasts.");

			Broadcast broadcast = Utils.GetBroadcastByName(broadcastName);

			int interval = -1, startDelay = -1;
			string color = "", triggerRegions = "", regionTriggerTo = "", groups = "", triggerWords = "", triggertowholegroup = "";

			OptionSet o = new OptionSet
			{
				{ "c|color=", v => color = v },
				{ "i|interval=", v => Int32.TryParse(v, out interval) },
				{ "sd|startdelay=", v =>  Int32.TryParse(v, out startDelay) },
				{ "tregions|triggerregions=", v => triggerRegions = v },
				{ "regionto|regiontriggerto=", v => regionTriggerTo = v },
				{ "g|groups=", v => groups = v },
				{ "twords|triggerwords=", v => triggerWords = v },
				{ "ttwg|triggertowholegroup=", v => triggertowholegroup = String.IsNullOrWhiteSpace(v) ? "true" : v }
			};

			List<string> parsed = o.Parse(args.Parameters);

			if (!String.IsNullOrWhiteSpace(color))
				broadcast.Color = color.FloatFromRGB();

			if (interval != -1)
				broadcast.Interval = interval;

			if (startDelay != -1)
				broadcast.StartDelay = startDelay;

			if (!String.IsNullOrWhiteSpace(triggerRegions))
				broadcast.TriggerRegions = triggerRegions.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

			if (!String.IsNullOrWhiteSpace(regionTriggerTo) && new string[] { "all", "region", "self" }.Contains(regionTriggerTo))
				broadcast.RegionTriggerTo = regionTriggerTo;

			if (!String.IsNullOrWhiteSpace(groups))
				broadcast.Groups = groups.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

			if (!String.IsNullOrWhiteSpace(triggerWords))
				broadcast.TriggerWords = triggerWords.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

			if (!String.IsNullOrWhiteSpace(triggertowholegroup))
				broadcast.TriggerToWholeGroup = triggertowholegroup == "true" ? true : false;



			if (!Database.UpdateBroadcast(broadcast.Name, broadcast.Enabled, broadcast.Messages, broadcast.Color.ToColorString(), broadcast.Interval, broadcast.StartDelay, broadcast.TriggerRegions,
				broadcast.RegionTriggerTo, broadcast.Groups, broadcast.TriggerWords, broadcast.TriggerToWholeGroup ? true : false))
				throw new CommandException($"Failed set parameters of Broadcast \"{broadcast.Name}\" due to a database error.");

			args.Player.SendSuccessMessage($"Successfully set variables for Broadcast \"{broadcast.Name}\"!");
		}

		[Command("listflags", AllowServer = true, Permissions = new[] { "autobroadcast.manage" })]
		[CommandSyntax("/autobc listflags")]
		[CommandDescription("Lists the flags that can modify a broadcast.")]
		public static void ListFlags(CommandArgs args)
		{
			int pageParamIndex = 1;
			int pageNumber;
			List<string> options = PaginationTools.BuildLinesFromTerms(TweakOptions);


			if (!PaginationTools.TryParsePageNumber(args.Parameters, pageParamIndex, args.Player, out pageNumber))
				return;

			PaginationTools.SendPage(args.Player, pageNumber, options,
			  new PaginationTools.Settings
			  {
				  HeaderFormat = "Flags ({0}/{1}):",
				  FooterFormat = $"Type /autobc listflags {{0}} for more.",
				  MaxLinesPerPage = 4
			  });
		}


		/// <summary>
		/// Manages the messages of a broadcast.
		/// </summary>
		/// <param name="args"></param>
		[Command("messages", AllowServer = true, Permissions = new[] { "autobroadcast.manage" })]
		[CommandSyntax("/autobc messages <add/delete/list>")]
		[CommandDescription("Manages the messages of a broadcast.")]
		public static void Messages(CommandArgs args)
		{
			if (args.Parameters.Count > 0)
			{
				try
				{
					switch (args.Parameters[0])
					{
						case "add":
							{
								AddMessage(args);
								break;
							}
						case "delete":
							{
								DelMessage(args);
								break;
							}
						case "list":
							{
								ListMessages(args);
								break;
							}
						case "edit":
							{
								EditMessage(args);
								break;
							}
						default:
							{
								throw new CommandException("Usage: /autobc messages <add/delete/list/edit>");
								break;
							}
					}
				}
				catch (CommandException ex)
				{
					args.Player.SendErrorMessage(ex.Message);
					return;
				}
				return;
			}
			
			throw new CommandException("Usage: /autobc messages <add/delete/list/edit>");

		}

		public static void AddMessage(CommandArgs args)
		{
			if (args.Parameters.Count < 3)
				throw new CommandException("Invalid usage! Usage: /autobc messages add <broadcast name> <message>");
			string broadcastName = args.Parameters[1];
			string message = string.Join(" ", args.Parameters.Skip(2));

			if (!Utils.BroadcastExists(broadcastName))
				throw new CommandException("A Broadcast with that name does not exist! Type /autobc list, for a list of Broadcasts.");

			Broadcast broadcast = Utils.GetBroadcastByName(broadcastName);
			List<string> messages = new List<string>();

			messages.AddRange(broadcast.Messages);
			messages.Add(message);

			if (!Database.UpdateBroadcast(broadcast.Name, broadcast.Enabled ? true : false, messages, Extensions.ToColorString(broadcast.Color), broadcast.Interval, broadcast.StartDelay, broadcast.TriggerRegions,
				broadcast.RegionTriggerTo, broadcast.Groups, broadcast.TriggerWords, broadcast.TriggerToWholeGroup ? true : false))
				throw new CommandException($"Failed to add message to Broadcast \"{broadcast.Name}\" due to a database error.");

			broadcast.Messages.Add(message);
			args.Player.SendSuccessMessage($"Successfully added message to Broadcast \"{broadcast.Name}\"!");
		}

		public static void DelMessage(CommandArgs args)
		{
			if (args.Parameters.Count < 3)
				throw new CommandException("Invalid usage! Usage: /autobc messages delete <broadcast name> <message ID>");

			string broadcastName = args.Parameters[1];

			if (!Utils.BroadcastExists(broadcastName))
				throw new CommandException("A Broadcast with that name does not exist! Type /autobc list, for a list of Broadcasts.");

			if (!int.TryParse(args.Parameters[2], out int messageID))
				throw new CommandException("Invalid message ID! Enter a numeric ID. Example usage: /autobc messages delete announcment 2");

			Broadcast broadcast = Utils.GetBroadcastByName(broadcastName);

			if (broadcast.Messages.Count < messageID)
				throw new CommandException("Broadcast does not contain a message with that ID. Type /autobc messages list <broadcast name> for the list!");

			List<string> messageList = new List<string>();
			messageList.AddRange(broadcast.Messages);
			string messageRemoved = messageList.ElementAt(messageID);
			messageList.RemoveAt(messageID);
			if (!Database.UpdateBroadcast(broadcast.Name, broadcast.Enabled, messageList, Extensions.ToColorString(broadcast.Color), broadcast.Interval, broadcast.StartDelay, broadcast.TriggerRegions,
				broadcast.RegionTriggerTo, broadcast.Groups, broadcast.TriggerWords, broadcast.TriggerToWholeGroup))
				throw new CommandException($"Failed remove message from Broadcast \"{broadcast.Name}\" due to a database error.");

			args.Player.SendSuccessMessage($"Successfully removed message: [{messageID}] {messageRemoved}");
			broadcast.Messages.RemoveAt(messageID);
		}
		public static void ListMessages(CommandArgs args)
		{
			if (args.Parameters.Count < 2)
				throw new CommandException("Invalid usage! Usage: /autobc messages list <broadcast name> ");
			string broadcastName = args.Parameters[1];

			if (!Utils.BroadcastExists(broadcastName))
				throw new CommandException("A Broadcast with that name does not exist! Type /autobc list, for a list of Broadcasts.");

			Broadcast broadcast = Utils.GetBroadcastByName(broadcastName);
			int pageParamIndex = 3;
			int pageNumber;

			if (!PaginationTools.TryParsePageNumber(args.Parameters, pageParamIndex, args.Player, out pageNumber))
				return;

			PaginationTools.SendPage(args.Player, pageNumber, broadcast.Messages.Select((e, index) => $"[{index}] " + e).ToList(),
			  new PaginationTools.Settings
			  {
				  HeaderFormat = "Messages ({0}/{1}):",
				  FooterFormat = $"Type /autobc messages list \"{broadcastName}\" {{0}} for more.",
				  MaxLinesPerPage = 4
			  });
		}

		public static void EditMessage(CommandArgs args)
		{
			if (args.Parameters.Count < 4)
				throw new CommandException("Invalid usage! Usage: /autobc messages edit <broadcast name> <messageID> <new message>");
			string broadcastName = args.Parameters[1];
			string message = string.Join(" ", args.Parameters.Skip(3));

			if (!Utils.BroadcastExists(broadcastName))
				throw new CommandException("A Broadcast with that name does not exist! Type /autobc list, for a list of Broadcasts.");

			if (!int.TryParse(args.Parameters[2], out int messageID))
				throw new CommandException("Invalid message ID! Enter a numeric ID. Example usage: /autobc messages edit testbroadcast 2 new message");

			Broadcast broadcast = Utils.GetBroadcastByName(broadcastName);
			List<string> messages = new List<string>();

			messages.AddRange(broadcast.Messages);
			messages[messageID] = message;

			if (!Database.UpdateBroadcast(broadcast.Name, broadcast.Enabled ? true : false, messages, Extensions.ToColorString(broadcast.Color), broadcast.Interval, broadcast.StartDelay, broadcast.TriggerRegions,
				broadcast.RegionTriggerTo, broadcast.Groups, broadcast.TriggerWords, broadcast.TriggerToWholeGroup ? true : false))
				throw new CommandException($"Failed to edit message of Broadcast \"{broadcast.Name}\" due to a database error.");

			broadcast.Messages[messageID] = message;
			args.Player.SendSuccessMessage($"Successfully edited message of Broadcast \"{broadcast.Name}\"!");
		}
	}
}
