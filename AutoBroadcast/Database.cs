using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TShockAPI;
using TShockAPI.DB;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System.Data;
using System.IO;
using TShockAPI.Hooks;

namespace AutoBroadcast
{
	public static class Database
	{
		private static IDbConnection db;
		public static List<Broadcast> Broadcasts = new List<Broadcast>();


		/// <summary>
		/// Creates database and connects to it while ensuring table structure.
		/// </summary>
		public static void Connect()
		{
			switch (TShock.Config.StorageType.ToLower())
			{
				case "mysql":
					string[] dbHost = TShock.Config.MySqlHost.Split(':');
					db = new MySqlConnection()
					{
						ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
							dbHost[0],
							dbHost.Length == 1 ? "3306" : dbHost[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword)

					};
					break;

				case "sqlite":
					string sql = Path.Combine(TShock.SavePath, "AutoBroadcast.sqlite");
					db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
					break;
			}

			SqlTableCreator sqlcreator = new SqlTableCreator(db, db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

			sqlcreator.EnsureTableStructure(new SqlTable("AutoBroadcast",
				new SqlColumn("Name", MySqlDbType.VarChar) { Primary = true, Unique = true, Length = 30 },
				new SqlColumn("Enabled", MySqlDbType.Int32) { NotNull = true, DefaultValue = "0", Length = 1 },
				new SqlColumn("Messages", MySqlDbType.Text) { Length = 500 },
				new SqlColumn("Color", MySqlDbType.Text) { Length = 500 },
				new SqlColumn("BroadcastInterval", MySqlDbType.Int32) { Length = 3 },
				new SqlColumn("StartDelay", MySqlDbType.Int32) { Length = 3 },
				new SqlColumn("TriggerRegions", MySqlDbType.Text) { Length = 500 },
				new SqlColumn("RegionTriggerTo", MySqlDbType.Text) { Length = 500 },
				new SqlColumn("Groups", MySqlDbType.Text) { Length = 500 },
				new SqlColumn("TriggerWords", MySqlDbType.Text) { Length = 500 },
				new SqlColumn("TriggerToWholeGroup", MySqlDbType.Int32) { NotNull = true, DefaultValue = "0", Length = 1 }));
		}

		

		public static bool AddBroadcast(string name, bool enabled, List<string> messages, string color, int interval, 
			int startDelay, List<string> triggerRegions, string regionTriggerTo, List<string> groups, List<string> triggerWords, bool triggerToWholeGroup)
		{
			string query = $"INSERT INTO `AutoBroadcast` (`Name`, `Enabled`, `Messages`, `Color`, `BroadcastInterval`, `StartDelay`, `TriggerRegions`, `RegionTriggerTo`, `Groups`, `TriggerWords`, `TriggerToWholeGroup`) " +
							$"VALUES ('{name}', " +
							$"'{(enabled ? 1 : 0)}', " +
							$"'{string.Join(";", messages)}', " +
							$"'{color}', " +
							$"'{interval}', " +
							$"'{startDelay}', " +
							$"'{string.Join(";", triggerRegions)}', " +
							$"'{regionTriggerTo}', " +
							$"'{string.Join(";", groups)}', " +
							$"'{string.Join(";", triggerWords)}', " +
							$"'{(triggerToWholeGroup ? 1 : 0)}');";

			if (db.Query(query) != 1)
			{
				TShock.Log.ConsoleError("Failed to add CommandPlate to database.");
				return false;
			}

			Broadcasts.Add(new Broadcast(name, enabled, messages, color, interval, startDelay, triggerRegions, regionTriggerTo, groups, triggerWords, triggerToWholeGroup));
			return true;
		}

		internal static bool UpdateBroadcast(string name, bool enabled, List<string> messages, string color, int interval,
			int startDelay, List<string> triggerRegions, string regionTriggerTo, List<string> groups, List<string> triggerWords, bool triggerToWholeGroup)
		{
			string query = $"UPDATE `AutoBroadcast` " +
							$"SET Enabled = '{(enabled ? 1 : 0)}', " +
							$"Messages = '{string.Join(";", messages)}', " +
							$"Color = '{color}', " +
							$"BroadcastInterval = '{interval}', " +
							$"StartDelay = '{startDelay}', " +
							$"TriggerRegions = '{string.Join(";", triggerRegions)}', " +
							$"RegionTriggerTo = '{regionTriggerTo}', " +
							$"Groups = '{string.Join(";", groups)}', " +
							$"TriggerWords = '{string.Join(";", triggerWords)}', " +
							$"TriggerToWholeGroup = '{(triggerToWholeGroup ? 1 : 0)}' " +
							$"WHERE Name = '{name}';";

			int result = db.Query(query);
			if (result != 1)
			{
				TShock.Log.ConsoleError("Failed to update CommandPlate in database.");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Deletes a Broadcast from the database.
		/// </summary>
		/// <param name="plateName">Name of the Broadcast to be deleted.</param>
		/// <returns>Returns false if failed.</returns>
		internal static bool DeleteBroadcast(string broadcastName)
		{
			string query = $"DELETE FROM `AutoBroadcast` WHERE `Name` = '{broadcastName}';";
			int result = db.Query(query);
			if (result != 1)
			{
				TShock.Log.ConsoleError("Failed to remove CommandPlate from database.");
				return false;
			}
			Broadcasts.Remove(Broadcasts.FirstOrDefault(e => e.Name == broadcastName));
			return true;
		}

		internal static bool ReloadBroadcasts()
		{
			Broadcasts.Clear();
			string query = $"SELECT * FROM `AutoBroadcast`";
			using (var reader = db.QueryReader(query))
			{
				while (reader.Read())
				{
					Broadcast broadcast = new Broadcast(
						reader.Get<string>("Name"),
						reader.Get<int>("Enabled") == 1 ? true : false,
						reader.Get<string>("Messages").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
						reader.Get<string>("Color"),
						reader.Get<int>("BroadcastInterval"),
						reader.Get<int>("StartDelay"),
						reader.Get<string>("TriggerRegions").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
						reader.Get<string>("RegionTriggerTo"),
						reader.Get<string>("Groups").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
						reader.Get<string>("TriggerWords").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
						reader.Get<int>("TriggerToWholeGroup") == 1 ? true : false

						); 
					Broadcasts.Add(broadcast);
				}
			}
			TShock.Log.Info("Reloaded AutoBroadcasts from database.");
			return true;
		}
	}
}
