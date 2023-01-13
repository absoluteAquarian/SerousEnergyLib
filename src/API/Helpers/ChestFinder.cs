using Terraria;
using Terraria.DataStructures;

namespace SerousEnergyLib.API.Helpers {
	/// <summary>
	/// A helper class for finding chests
	/// </summary>
	public static class ChestFinder {
		/// <summary>
		/// <see cref="Chest.FindChestByGuessing(int, int)"/> fails when <paramref name="x"/> and <paramref name="y"/> refer to any sub-tile in a chest that isn't the top-left subtile.<br/>
		/// This method performs a correct "guessing" algorithm which accounts for all corners
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <returns>The index of the chest in <see cref="Main.chest"/>, or <c>-1</c> if no chest was found</returns>
		public static int FindByGuessingImproved(int x, int y) {
			for (int i = 0; i < Main.maxChests; i++) {
				Chest chest = Main.chest[i];

				if (chest is null)
					continue;

				if (chest.IsChestAt(x, y))
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Performs a similar algorithm as <see cref="FindByGuessingImproved(int, int)"/> to find a chest at location (<paramref name="centerX"/>, <paramref name="centerY"/>) or its adjacent cardinal tiles
		/// </summary>
		/// <param name="centerX">The tile X-coordinate</param>
		/// <param name="centerY">The tile Y-coordinate</param>
		/// <param name="tile">The tile coordinates that the chest was found at, or <see cref="Point16.NegativeOne"/> if no chest was found</param>
		/// <returns>The index of the chest in <see cref="Main.chest"/>, or <c>-1</c> if no chest was found</returns>
		public static int FindChestAtCenterOrCardinalTiles(int centerX, int centerY, out Point16 tile) {
			int left = centerX - 1, up = centerY - 1, right = centerX + 1, down = centerY + 1;

			for (int i = 0; i < Main.maxChests; i++) {
				Chest chest = Main.chest[i];

				if (chest is null)
					continue;

				if (chest.IsChestAt(centerX, centerY)) {
					tile = new Point16(centerX, centerY);
					return i;
				}

				if (chest.IsChestAt(left, centerY)) {
					tile = new Point16(left, centerY);
					return i;
				}

				if (chest.IsChestAt(centerX, up)) {
					tile = new Point16(centerX, up);
					return i;
				}

				if (chest.IsChestAt(right, centerY)) {
					tile = new Point16(right, centerY);
					return i;
				}

				if (chest.IsChestAt(centerX, down)) {
					tile = new Point16(centerX, down);
					return i;
				}
			}

			tile = Point16.NegativeOne;
			return -1;
		}

		/// <summary>
		/// Performs a similar algorithm as <see cref="FindByGuessingImproved(int, int)"/> to find a chest at the adjacent cardinal tiles for location (<paramref name="centerX"/>, <paramref name="centerY"/>)
		/// </summary>
		/// <param name="centerX">The tile X-coordinate</param>
		/// <param name="centerY">The tile Y-coordinate</param>
		/// <param name="tile">The tile coordinates that the chest was found at, or <see cref="Point16.NegativeOne"/> if no chest was found</param>
		/// <returns>The index of the chest in <see cref="Main.chest"/>, or <c>-1</c> if no chest was found</returns>
		public static int FindChestAtCardinalTiles(int centerX, int centerY, out Point16 tile) {
			int left = centerX - 1, up = centerY - 1, right = centerX + 1, down = centerY + 1;

			for (int i = 0; i < Main.maxChests; i++) {
				Chest chest = Main.chest[i];

				if (chest is null)
					continue;

				if (chest.IsChestAt(left, centerY)) {
					tile = new Point16(left, centerY);
					return i;
				}

				if (chest.IsChestAt(centerX, up)) {
					tile = new Point16(centerX, up);
					return i;
				}

				if (chest.IsChestAt(right, centerY)) {
					tile = new Point16(right, centerY);
					return i;
				}

				if (chest.IsChestAt(centerX, down)) {
					tile = new Point16(centerX, down);
					return i;
				}
			}

			tile = Point16.NegativeOne;
			return -1;
		}
	}
}
