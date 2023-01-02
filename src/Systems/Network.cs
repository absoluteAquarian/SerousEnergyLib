using Mono.Cecil;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.TileData;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace SerousEnergyLib.Systems {
	/// <summary>
	/// An abstraction over accessing and manipulating <see cref="NetworkInfo"/> data in tiles
	/// </summary>
	public static class Network {
		// Pre-calculated collections of the full path trees in all existing networks
		internal static readonly List<NetworkInstance> itemNetworks = new();
		internal static readonly List<NetworkInstance> fluidNetworks = new();
		internal static readonly List<NetworkInstance> powerNetworks = new();

		/// <summary>
		/// Updates the tile at position (<paramref name="x"/>, <paramref name="y"/>) and the 4 entries adjacent to it in the cardinal directions
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <param name="type">The new classification for the entry</param>
		public static void PlaceEntry(int x, int y, NetworkType type) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				Netcode.RequestNetworkEntryPlacement(x, y, type);
				return;
			}

			ref NetworkInfo info = ref Main.tile[x, y].Get<NetworkInfo>();

			info.Type |= type;

			UpdateEntryConnections(x, y);
			UpdateEntryConnections(x - 1, y);
			UpdateEntryConnections(x, y - 1);
			UpdateEntryConnections(x + 1, y);
			UpdateEntryConnections(x, y + 1);

			Netcode.SyncNetworkInfoDiamond(x, y);

			// Combine any adjacent networks
			PlaceEntry_UpdateNetworkInstances(x, y, type);
		}

		private static void PlaceEntry_UpdateNetworkInstances(int x, int y, NetworkType type) {
			PlaceEntry_UpdateNetworkInstances_CheckFilter(x, y, type, NetworkType.Items, itemNetworks);
			PlaceEntry_UpdateNetworkInstances_CheckFilter(x, y, type, NetworkType.Fluids, fluidNetworks);
			PlaceEntry_UpdateNetworkInstances_CheckFilter(x, y, type, NetworkType.Power, powerNetworks);
		}

		private static void PlaceEntry_UpdateNetworkInstances_CheckFilter(int x, int y, NetworkType type, NetworkType filter, List<NetworkInstance> source) {
			if ((type & filter) == filter) {
				// Update any nearby item networks or create one if necessary
				List<NetworkInstance> adjacent = GetCardinalAdjacentNetworks(x, y, source, out List<int> indices);

				Point16 location = new Point16(x, y);

				if (adjacent.Count == 0) {
					// No adjacent networks.  Create a new one
					NetworkInstance net = NetworkInstance.CreateNetwork(filter);
					net.ReserveNextID();
					
					net.AddEntry(location);

					source.Add(net);

					Netcode.SyncFullNetworkData(net.ID);
				} else if (adjacent.Count == 0) {
					// Only one adjacent network detected.  Just add the entry to the network
					NetworkInstance net = adjacent[0];

					net.AddEntry(location);

					Netcode.SyncNetworkInstanceEntryPlacement(net, location);
				} else {
					// Combine the existing networks into one network
					NetworkInstance net = adjacent[0];

					// Copy the data
					for (int i = 1; i < adjacent.Count; i++) {
						net.delayCoarsePathCalculationFromCopy = i < adjacent.Count - 1;

						net.CopyFrom(adjacent[i]);
					}

					Netcode.SyncFullNetworkData(net.ID);

					// Destroy the old instances
					for (int i = indices.Count - 1; i > 0; i--) {
						int index = indices[i];
						NetworkInstance removed = source[index];

						Netcode.SendNetworkRemoval(removed.ID);

						removed.Dispose();

						source.RemoveAt(index);
					}
				}
			}
		}

		private static List<NetworkInstance> GetCardinalAdjacentNetworks(int x, int y, List<NetworkInstance> source, out List<int> indices) {
			List<NetworkInstance> instances = new();
			HashSet<int> netIDs = new();
			indices = new();

			int index = 0;
			foreach (NetworkInstance net in source) {
				if (net.HasEntry(x - 1, y) && !netIDs.Add(net.ID)) {
					instances.Add(net);
					indices.Add(index);
				} else if (net.HasEntry(x, y - 1) && !netIDs.Add(net.ID)) {
					instances.Add(net);
					indices.Add(index);
				} else if (net.HasEntry(x + 1, y) && !netIDs.Add(net.ID)) {
					instances.Add(net);
					indices.Add(index);
				} else if (net.HasEntry(x, y + 1) && !netIDs.Add(net.ID)){
					instances.Add(net);
					indices.Add(index);
				}

				++index;
			}

			return instances;
		}

		/// <summary>
		/// Clears the <see cref="NetworkInfo"/> information in the tile at position (<paramref name="x"/>, <paramref name="y"/>) and updates the 4 entries adjacent to it in the cardinal directions
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		public static void RemoveEntry(int x, int y, NetworkType type) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				Netcode.RequestNetworkEntryRemoval(x, y, type);
				return;
			}

			ref NetworkInfo info = ref Main.tile[x, y].Get<NetworkInfo>();

			info.Type &= ~type;

			if (info.Type == NetworkType.None)
				info.Connections = ConnectionDirection.None;
			else
				UpdateEntryConnections(x, y);

			UpdateEntryConnections(x - 1, y);
			UpdateEntryConnections(x, y - 1);
			UpdateEntryConnections(x + 1, y);
			UpdateEntryConnections(x, y + 1);

			Netcode.SyncNetworkInfoDiamond(x, y);

			// Split the network into separate networks
			RemoveEntry_UpdateNetworkInstances(x, y, type);
		}

		private static void RemoveEntry_UpdateNetworkInstances(int x, int y, NetworkType type) {
			RemoveEntry_UpdateNetworkInstances_CheckFilter(x, y, type, NetworkType.Items, itemNetworks);
			RemoveEntry_UpdateNetworkInstances_CheckFilter(x, y, type, NetworkType.Fluids, fluidNetworks);
			RemoveEntry_UpdateNetworkInstances_CheckFilter(x, y, type, NetworkType.Power, powerNetworks);
		}

		private static void RemoveEntry_UpdateNetworkInstances_CheckFilter(int x, int y, NetworkType type, NetworkType filter, List<NetworkInstance> source) {
			if ((type & filter) == filter) {
				NetworkInstance parent = GetNetworkAt(x, y, filter, out int index);

				if (parent is null)
					return;  // No network present

				if (parent.EntryCount == 0) {
					// This was a single-entry network.  Just remove it
					Netcode.SendNetworkRemoval(parent.ID);
					
					source.RemoveAt(index);
					parent.Dispose();
				} else {
					// Split the network into multiple networks
					parent.OnEntryRemoved(new Point16(x, y));

					// Remove the parent since it won't be returned by RemoveEntry
					source.RemoveAt(index);

					Netcode.SendNetworkRemoval(parent.ID);

					// Add the new networks to the collection
					foreach (var net in NetworkInstance.RemoveEntry(parent, x, y)) {
						source.Add(net);

						Netcode.SyncFullNetworkData(net.ID);
					}

					parent.Dispose();
				}
			}
		}

		private static NetworkInstance GetNetworkAt(int x, int y, NetworkType type, out int networkIndex) {
			List<NetworkInstance> source;
			networkIndex = -1;

			switch (type) {
				case NetworkType.Items:
					source = itemNetworks;
					break;
				case NetworkType.Fluids:
					source = fluidNetworks;
					break;
				case NetworkType.Power:
					source = powerNetworks;
					break;
				default:
					// Type was invalid
					return null;
			}

			Point16 loc = new Point16(x, y);
			networkIndex = 0;
			foreach (NetworkInstance net in source) {
				if (net.HasEntry(loc))
					return net;

				networkIndex++;
			}

			// No network found
			networkIndex = -1;
			return null;
		}

		/// <summary>
		/// Attempts to find an item network at location (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <returns>An <see cref="ItemNetwork"/> instance if one was found, <see langword="null"/> otherwise.</returns>
		public static ItemNetwork GetItemNetworkAt(int x, int y) => GetNetworkAt(x, y, NetworkType.Items, out _) as ItemNetwork;

		/// <summary>
		/// Attempts to find a fluid network at location (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <returns>A <see cref="FluidNetwork"/> instance if one was found, <see langword="null"/> otherwise.</returns>
		public static FluidNetwork GetFluidNetworkAt(int x, int y) => GetNetworkAt(x, y, NetworkType.Fluids, out _) as FluidNetwork;

		/// <summary>
		/// Attempts to find a power network at location (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <returns>An <see cref="PowerNetwork"/> instance if one was found, <see langword="null"/> otherwise.</returns>
		public static PowerNetwork GetPowerNetworkAt(int x, int y) => GetNetworkAt(x, y, NetworkType.Power, out _) as PowerNetwork;

		public static bool IsItemPipe(int x, int y) => (Main.tile[x, y].Get<NetworkInfo>().Type & NetworkType.Items) == NetworkType.Items;

		public static bool IsFluidPipe(int x, int y) => (Main.tile[x, y].Get<NetworkInfo>().Type & NetworkType.Fluids) == NetworkType.Fluids;

		public static bool IsWire(int x, int y) => (Main.tile[x, y].Get<NetworkInfo>().Type & NetworkType.Power) == NetworkType.Power;

		internal static void UpdateEntryConnections(int x, int y) {
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

		private static void AttemptToCombineNetworks(int x, int y, NetworkType type) {
			
		}
	}
}
