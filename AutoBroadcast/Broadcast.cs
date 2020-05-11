using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBroadcast
{
	public class Broadcast
	{
		/// <summary>
		/// Name of the Broadcast.
		/// </summary>
		public string Name = string.Empty;
		/// <summary>
		/// Toggles Broadcast on/off.
		/// </summary>
		public bool Enabled = false;
		/// <summary>
		/// A list of messages the Broadcast will display.
		/// </summary>
		public List<string> Messages = new List<string>();
		/// <summary>
		/// Color of the Boradcast messages.
		/// </summary>
		public float[] Color = new float[3];
		/// <summary>
		/// Interval between each time the Broadcast happens.
		/// </summary>
		public int Interval = 0;
		/// <summary>
		/// A start delay of the Broadcast after it gets turned on.
		/// </summary>
		public int StartDelay = 0;
		/// <summary>
		/// A list of region names that trigger a Broadcast.
		/// </summary>
		public List<string> TriggerRegions = new List<string>();
		/// <summary>
		/// "all","region","self", "none"
		/// Broadcast the message to either all players online, all players in the region, or an individual player which has caused the message to trigger.
		/// 
		/// </summary>
		public string RegionTriggerTo = "none";
		/// <summary>
		/// [*] / [playername] / [groupname]` 
		/// Leaving this value blank restricts actions taken by this message to this user, placing a * encompasses all users. Adding a group's name will leave the group and it's users to be affected by the message.
		/// </summary>
		public List<string> Groups = new List<string>();
		/// <summary>
		/// List of words typed in chat to trigger this broadcast message.
		/// </summary>
		public List<string> TriggerWords = new List<string>();
		/// <summary>
		/// Setting this value as true will send this message to all members of the group who triggered this message.
		/// </summary>
		public bool TriggerToWholeGroup = false;

		public Broadcast(string name, bool enabled, List<string> messages, string color, int interval, int startDelay, List<string> triggerRegions, string regionTriggerTo, List<string> groups, List<string> triggerWords, bool triggerToWholeGroup)
		{
			Name = name;
			Enabled = enabled;
			Messages = messages;
			Color = Array.ConvertAll<string, float>(color.Split(','), float.Parse);
			Interval = interval;
			StartDelay = startDelay;
			TriggerRegions = triggerRegions;
			RegionTriggerTo = regionTriggerTo;
			Groups = groups;
			TriggerWords = triggerWords;
			TriggerToWholeGroup = triggerToWholeGroup;
		}
	}
}
