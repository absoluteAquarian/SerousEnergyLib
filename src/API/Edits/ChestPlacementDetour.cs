using SerousCommonLib.API;
using SerousEnergyLib.Systems;
using SerousEnergyLib.Systems.Networks;
using System;
using Terraria.DataStructures;

namespace SerousEnergyLib.API.Edits {
	internal class ChestPlacementDetour : Edit {
		public override void LoadEdits() {
			On.Terraria.Chest.AfterPlacement_Hook += Hook_AfterPlacement_Hook;
		}

		public override void UnloadEdits() {
			On.Terraria.Chest.AfterPlacement_Hook -= Hook_AfterPlacement_Hook;
		}

		private static int Hook_AfterPlacement_Hook(On.Terraria.Chest.orig_AfterPlacement_Hook orig, int x, int y, int type, int style, int direction, int alternate) {
			// This detour checks the tiles surrounding the chest for any item networks and adds this chest to them
			int chest = orig(x, y, type, style, direction, alternate);

			if (chest > -1) {
				// The chest was able to be placed
				Point16 chestTopLeft = TileFunctions.GetTopLeftTileInMultitile(x, y);

				Span<Point16> offsets = stackalloc Point16[] {
					new(-1, 0), new(-1, 1),  // Left edge
					new(0, -1), new(1, -1),  // Top edge
					new(2, 0),  new(2, 1),   // Right edge
					new(0, 2),  new(1, 2)    // Bottom edge
				};

				for (int i = 0; i < 8; i++) {
					var offset = offsets[i];

					if (Network.GetItemNetworkAt(chestTopLeft.X + offset.X, chestTopLeft.Y + offset.Y) is ItemNetwork net) {
						int chestX = offset.X, chestY = offset.Y;
						
						// Adjust the network offset to its adjacent chest sub-tile location
						if (offset.X == -1)
							chestX++;
						else if (offset.X == 2)
							chestX--;

						if (offset.Y == -1)
							chestY++;
						else if (offset.Y == 2)
							chestY--;

						net.AddAdjacentInventory(chestTopLeft + new Point16(chestX, chestY));
					}
				}
			}

			return chest;
		}
	}
}
