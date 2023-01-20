using Microsoft.Xna.Framework;
using SerousEnergyLib.Tiles;
using Terraria;
using Terraria.ID;

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
		/// <param name="netChange">The change in capacity for the network</param>
		public static string GetNetColor(double netChange) {
			Color net;

			if (netChange > 0)
				net = Color.Green;
			else if (netChange < 0)
				net = Color.Red;
			else
				net = Color.White;

			return new Color(net.ToVector3() * (Main.mouseTextColor / 255f)).Hex3();
		}
	}
}
