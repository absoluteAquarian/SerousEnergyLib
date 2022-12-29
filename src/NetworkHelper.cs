using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerousEnergyLib {
	public static class NetworkHelper {
		public static bool AtLeastOneSurroundingTileIsSolid(int x, int y) {
			return (x > 0 && Main.tile[x - 1, y].HasTile)
				|| (y > 0 && Main.tile[x, y - 1].HasTile)
				|| (x < Main.maxTilesX - 1 && Main.tile[x + 1, y].HasTile)
				|| (y < Main.maxTilesY - 1 && Main.tile[x, y + 1].HasTile);
		}

		public static bool TileIsChest(int tileType) {
			if (tileType == TileID.Containers || tileType == TileID.Containers2 || TileID.Sets.BasicChest[tileType])
				return true;
			
			ModTile tile = TileLoader.GetTile(tileType);

			return tile is not null && Array.FindIndex(tile.AdjTiles, type => type == TileID.Containers || type == TileID.Containers2) >= 0;
		}
	}
}
