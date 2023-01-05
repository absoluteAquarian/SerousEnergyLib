using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ObjectData;

namespace SerousEnergyLib {
	/// <summary>
	/// A helper class for functions related to tiles
	/// </summary>
	public static class TileFunctions {
		/// <summary>
		/// Atttempts to find the top-left corner of a multitile at location (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <returns>The tile location of the multitile's top-left corner, or the input location if no tile is present or the tile is not part of a multitile</returns>
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

		/// <summary>
		/// Gets the drawing offset caused by certain zoom levels and/or lighting modes
		/// </summary>
		public static Vector2 GetLightingDrawOffset() {
			bool doOffset = Lighting.NotRetro;
			if (!doOffset && Main.GameZoomTarget != 1)
				doOffset = true;

			return doOffset ? new Vector2(12) * 16 : Vector2.Zero;
		}
	}
}
