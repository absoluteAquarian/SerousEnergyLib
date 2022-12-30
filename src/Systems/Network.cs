using SerousEnergyLib.TileData;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace SerousEnergyLib.Systems {
	/// <summary>
	/// An abstraction over accessing and manipulating <see cref="NetworkInfo"/> data in tiles
	/// </summary>
	public static class Network {
		// Pre-calculated collections of the full path trees in all existing networks
		internal static readonly Dictionary<NetworkType, List<NetworkInstance>> networks = new();

		/// <summary>
		/// Updates the tile at position (<paramref name="x"/>, <paramref name="y"/>) and the 4 entries adjacent to it in the cardinal directions
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <param name="type">The new classification for the entry</param>
		public static void PlaceEntry(int x, int y, NetworkType type) {
			ref NetworkInfo info = ref Main.tile[x, y].Get<NetworkInfo>();

			info.Type = type;

			UpdateEntry(x, y);
			UpdateEntry(x - 1, y);
			UpdateEntry(x, y - 1);
			UpdateEntry(x + 1, y);
			UpdateEntry(x, y + 1);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				Netcode.SyncNetworkInfoDiamond(x, y);
		}

		/// <summary>
		/// Clears the <see cref="NetworkInfo"/> information in the tile at position (<paramref name="x"/>, <paramref name="y"/>) and updates the 4 entries adjacent to it in the cardinal directions
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		public static void RemoveEntry(int x, int y) {
			ref NetworkInfo info = ref Main.tile[x, y].Get<NetworkInfo>();

			info.Type = NetworkType.None;
			info.Connections = ConnectionDirection.None;
			
			UpdateEntry(x - 1, y);
			UpdateEntry(x, y - 1);
			UpdateEntry(x + 1, y);
			UpdateEntry(x, y + 1);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				Netcode.SyncNetworkInfoDiamond(x, y);
		}

		public static bool IsItemPipe(int x, int y) => (Main.tile[x, y].Get<NetworkInfo>().Type & NetworkType.Items) == NetworkType.Items;

		public static bool IsFluidPipe(int x, int y) => (Main.tile[x, y].Get<NetworkInfo>().Type & NetworkType.Fluids) == NetworkType.Fluids;

		public static bool IsWire(int x, int y) => (Main.tile[x, y].Get<NetworkInfo>().Type & NetworkType.Power) == NetworkType.Power;

		internal static void UpdateEntry(int x, int y) {
			ref NetworkInfo center = ref Main.tile[x, y].Get<NetworkInfo>();

			bool hasLeft = false, hasUp = false, hasRight = false, hasDown = false;

			// Check the left tile
			if (x > 0) {
				ref NetworkInfo check = ref Main.tile[x - 1, y].Get<NetworkInfo>();

				if ((center.Type & check.Type) != 0)
					hasLeft = true;
			}

			// Check the up tile
			if (y > 0) {
				ref NetworkInfo check = ref Main.tile[x, y - 1].Get<NetworkInfo>();

				if ((center.Type & check.Type) != 0)
					hasUp = true;
			}

			// Check the right tile
			if (x < Main.maxTilesX - 1) {
				ref NetworkInfo check = ref Main.tile[x + 1, y].Get<NetworkInfo>();

				if ((center.Type & check.Type) != 0)
					hasRight = true;
			}

			// Check the down tile
			if (y < Main.maxTilesY - 1) {
				ref NetworkInfo check = ref Main.tile[x, y + 1].Get<NetworkInfo>();

				if ((center.Type & check.Type) != 0)
					hasDown = true;
			}

			ConnectionDirection dirs = ConnectionDirection.None;
			
			if (hasLeft)
				dirs |= ConnectionDirection.Left;
			if (hasUp)
				dirs |= ConnectionDirection.Up;
			if (hasRight)
				dirs |= ConnectionDirection.Right;
			if (hasDown)
				dirs |= ConnectionDirection.Down;

			center.Connections = dirs;
		}
	}
}
