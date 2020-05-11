using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBroadcast
{
	public static class Extensions
	{
		public static string ToColorString(this float[] array)
		{
			return string.Join(",", array);
		}

		public static float[] FloatFromRGB(this string value)
		{
			if (String.IsNullOrWhiteSpace(value))
				return null;

			return Array.ConvertAll<string, float>(value.Split(','), float.Parse);
		}
	}
}
