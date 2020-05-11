using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBroadcast
{
	public static class Utils
	{
		/// <summary>
		/// Finds a Broadcast in memory storage by name and returns the object.
		/// </summary>
		/// <param name="broadcastName">Name of the plate.</param>
		/// <returns>The CommandPlate object.</returns>
		public static Broadcast GetBroadcastByName(string broadcastName)
		{
			return Database.Broadcasts.FirstOrDefault(e => e.Name == broadcastName);
		}
		/// <summary>
		/// Checks if Broadcast exsists in memory storage.
		/// </summary>
		/// <param name="broadcastName"></param>
		/// <returns>Returns true if exists.</returns>
		public static bool BroadcastExists(string broadcastName)
		{
			return Database.Broadcasts.Contains(GetBroadcastByName(broadcastName));
		}
	}
}
