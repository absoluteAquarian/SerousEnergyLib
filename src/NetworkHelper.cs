using Microsoft.Xna.Framework;
using SerousEnergyLib.Tiles;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerousEnergyLib {
	/// <summary>
	/// A helper class containing methods used by the merging logic for <see cref="BaseNetworkTile"/> tiles
	/// </summary>
	public static class NetworkHelper {
		#pragma warning disable CS1591
		public static bool AtLeastOneSurroundingTileIsSolid(int x, int y) {
			return (x > 0 && Main.tile[x - 1, y].HasTile)
				|| (y > 0 && Main.tile[x, y - 1].HasTile)
				|| (x < Main.maxTilesX - 1 && Main.tile[x + 1, y].HasTile)
				|| (y < Main.maxTilesY - 1 && Main.tile[x, y + 1].HasTile);
		}

		public static bool TileIsChest(int tileType) {
			if (tileType == TileID.Containers || tileType == TileID.Containers2 || TileID.Sets.BasicChest[tileType])
				return true;
			
			/*
			ModTile tile = TileLoader.GetTile(tileType);

			return tile is not null && Array.FindIndex(tile.AdjTiles, type => type == TileID.Containers || type == TileID.Containers2) >= 0;
			*/
			return false;
		}

		/// <summary>
		/// Retrieves a color indicator string for use with chat tags
		/// </summary>
		/// <param name="current">The current capacity of the network</param>
		/// <param name="netChange">The change in capacity for the network</param>
		public static string GetNetColor(double current, double netChange) {
			// 0x loss should be white
			// -0.5x loss should be bright red
			// +2x gain should be bright green
			double orig = current - netChange;

			Color netZero = new Color(Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor, Main.mouseTextColor);
			Color netEnd;
			double lerpStart, lerpEnd;

			if (netChange > 0) {
				netEnd = Color.Green;
				lerpStart = orig;
				lerpEnd = orig * 2;
			} else if (netChange < 0) {
				netEnd = Color.Red;
				lerpStart = orig / 2;
				lerpEnd = orig;
			} else
				return netZero.Hex3();

			double t = Utils.GetLerpValue(lerpStart, lerpEnd, current, clamped: true);
			Color lerp = Color.Lerp(netZero, netEnd, (float)t);
			return lerp.Hex3();
		}
	}
}
