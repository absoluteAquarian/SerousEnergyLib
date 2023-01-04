using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ObjectData;

namespace SerousEnergyLib {
	public static class TileFunctions {
		public static Point16 GetTopLeftTileInMultitile(int x, int y) {
			Tile tile = Main.tile[x, y];

			int frameX = 0;
			int frameY = 0;

			if (tile.HasTile) {
				int style = 0, alt = 0;
				TileObjectData.GetTileInfo(tile, ref style, ref alt);
				TileObjectData data = TileObjectData.GetTileData(tile.TileType, style, alt);

				if (data != null) {
					int size = 16 + data.CoordinatePadding;

					frameX = tile.TileFrameX % (size * data.Width) / size;
					frameY = tile.TileFrameY % (size * data.Height) / size;
				}
			}

			return new Point16(x - frameX, y - frameY);
		}

		public static Vector2 GetLightingDrawOffset() {
			bool doOffset = Lighting.NotRetro;
			if (!doOffset && Main.GameZoomTarget != 1)
				doOffset = true;

			return doOffset ? new Vector2(12) * 16 : Vector2.Zero;
		}
	}
}
