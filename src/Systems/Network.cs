using SerousEnergyLib.API.Fluid;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

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
		/// Whether an <see cref="IPowerGeneratorMachine"/> machine should update.
		/// Use this property to prevent a generator machine from updating twice per game tick.
		/// </summary>
		public static bool UpdatingPowerGenerators { get; private set; }

		/// <summary>
		/// Whether an <see cref="IPowerStorageMachine"/> machine should update.
		/// Use this property to prevent a storage machine from udpating twice per game tick.
		/// </summary>
		public static bool UpdatingPowerStorages { get; private set; }

		internal static void UpdateItemNetworks() {
			// Ensure that no empty networks can exist
			itemNetworks.RemoveAll(static i => i.IsEmpty);

			foreach (var net in itemNetworks)
				net.Update();
		}

		internal static void UpdateFluidNetworks() {
			// Ensure that no empty networks can exist
			fluidNetworks.RemoveAll(static f => f.IsEmpty);

			foreach (var net in fluidNetworks)
				net.Update();
		}

		internal static void UpdatePowerNetworks() {
			// Ensure that no empty networks can exist
			powerNetworks.RemoveAll(static f => f.IsEmpty);

			foreach (var net in powerNetworks) {
				var power = net as PowerNetwork;
				power.previousPower = power.Storage.CurrentCapacity;
			}

			var generators = TileEntity.ByPosition.Values.OfType<IPowerGeneratorMachine>();

			var storages = TileEntity.ByPosition.Values.OfType<IPowerStorageMachine>();
			
			// Send power form power generators
			UpdatingPowerGenerators = true;

			foreach (var machine in generators) {
				// Update() will be blocked later.  This is used to force power generators to run before power consumers
				(machine as ModTileEntity).Update();
			}

			foreach (var machine in generators)
				IPowerGeneratorMachine.GeneratePower(machine);
			
			UpdatingPowerGenerators = false;

			// Send power from power storages
			UpdatingPowerStorages = true;

			foreach (var machine in storages)
				IPoweredMachine.ExportPowerToAdjacentNetworks(machine, machine.StorageExportMode);

			foreach (var machine in storages) {
				// Update() will be blocked later.  This is used to force power storages to run before power consumers
				(machine as ModTileEntity).Update();
			}

			UpdatingPowerStorages = false;

			// Send power to power consumers
			foreach (var net in powerNetworks)
				net.Update();

			// Delay power storage importing so that it has lower priority than power consumers
			UpdatingPowerStorages = true;

			foreach (var machine in storages)
				IPoweredMachine.ImportPowerFromAdjacentNetworks(machine);

			UpdatingPowerStorages = false;

			foreach (var net in powerNetworks) {
				var power = net as PowerNetwork;
				power.netPower = power.Storage.CurrentCapacity - power.previousPower;

				Netcode.SyncNetworkPowerStorage(power, power.FirstNode);
			}
		}

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

			Tile tile = Main.tile[x, y];

			ref NetworkInfo info = ref tile.Get<NetworkInfo>();
			info.Type |= type;

			ref NetworkTaggedInfo tags = ref tile.Get<NetworkTaggedInfo>();
			tags.Color = NetworkColor.None;

			bool isPump;
			int maxPumpTime = -1;
			ModTile modTile = TileLoader.GetTile(tile.TileType);
			if (modTile is IPumpTile pump) {
				info.IsPump = isPump = true;
				tags.PumpDirection = pump.GetDirection(x, y);
				maxPumpTime = pump.GetMaxTimer(x, y);
			} else {
				info.IsPump = isPump = false;
				tags.PumpDirection = PumpDirection.Left;
			}

			UpdateEntryConnections(x, y);
			UpdateEntryConnections(x - 1, y);
			UpdateEntryConnections(x, y - 1);
			UpdateEntryConnections(x + 1, y);
			UpdateEntryConnections(x, y + 1);

			Netcode.SyncNetworkInfoDiamond(x, y);

			// Combine any adjacent networks
			if (modTile is not NetworkJunction) {
				var allowed = PermittedAdjacentNetworks.All;
				PlaceEntry_CheckFilter(x, y, type, NetworkType.Items, allowed, itemNetworks);
				PlaceEntry_CheckFilter(x, y, type, NetworkType.Fluids, allowed, fluidNetworks);
				PlaceEntry_CheckFilter(x, y, type, NetworkType.Power, allowed, powerNetworks);
			} else {
				int mode = tile.TileFrameX / 18;

				var allowed = mode switch {
					0 => PermittedAdjacentNetworks.LeftRight,
					1 => PermittedAdjacentNetworks.LeftDown,
					2 => PermittedAdjacentNetworks.LeftUp,
					_ => throw new Exception("Invalid NetworkJunction frameX")
				};

				PlaceEntry_CheckFilter(x, y, type, NetworkType.Items, allowed, itemNetworks);
				PlaceEntry_CheckFilter(x, y, type, NetworkType.Fluids, allowed, fluidNetworks);
				PlaceEntry_CheckFilter(x, y, type, NetworkType.Power, allowed, powerNetworks);

				allowed = mode switch {
					0 => PermittedAdjacentNetworks.UpDown,
					1 => PermittedAdjacentNetworks.UpRight,
					2 => PermittedAdjacentNetworks.RightDown,
					_ => default
				};

				PlaceEntry_CheckFilter(x, y, type, NetworkType.Items, allowed, itemNetworks);
				PlaceEntry_CheckFilter(x, y, type, NetworkType.Fluids, allowed, fluidNetworks);
				PlaceEntry_CheckFilter(x, y, type, NetworkType.Power, allowed, powerNetworks);
			}

			if (isPump) {
				Point16 location = new Point16(x, y);

				if (GetItemNetworkAt(x, y) is ItemNetwork itemNet) {
					itemNet.AddPumpTimer(location, maxPumpTime);
					Netcode.SyncPumpTimer(itemNet, location, maxPumpTime);
				}
				
				if (GetFluidNetworkAt(x, y) is FluidNetwork fluidNet) {
					fluidNet.AddPumpTimer(location, maxPumpTime);
					Netcode.SyncPumpTimer(fluidNet, location, maxPumpTime);
				}
			}
		}

		private readonly struct PermittedAdjacentNetworks {
			public readonly bool left;
			public readonly bool up;
			public readonly bool right;
			public readonly bool down;

			public static readonly PermittedAdjacentNetworks All = new(true, true, true, true);
			public static readonly PermittedAdjacentNetworks LeftRight = new(true, false, true, false);
			public static readonly PermittedAdjacentNetworks UpDown = new(false, true, false, true);
			public static readonly PermittedAdjacentNetworks LeftUp = new(true, true, false, false);
			public static readonly PermittedAdjacentNetworks RightDown = new(false, false, true, true);
			public static readonly PermittedAdjacentNetworks LeftDown = new(true, false, false, true);
			public static readonly PermittedAdjacentNetworks UpRight = new(false, true, true, false);

			public PermittedAdjacentNetworks(bool left, bool up, bool right, bool down) {
				this.left = left;
				this.up = up;
				this.right = right;
				this.down = down;
			}
		}

		private static void PlaceEntry_CheckFilter(int x, int y, NetworkType type, NetworkType filter, PermittedAdjacentNetworks allowed, List<NetworkInstance> source) {
			if ((type & filter) == filter) {
				// Update any nearby item networks or create one if necessary
				List<NetworkInstance> adjacent = GetCardinalAdjacentNetworks(x, y, source, allowed, out List<int> indices);

				Point16 location = new Point16(x, y);

				if (adjacent.Count == 0) {
					// No adjacent networks.  Create a new one
					NetworkInstance net = NetworkInstance.CreateNetwork(filter);
					net.ReserveNextID();

					net.Recalculate(location);

					source.Add(net);

					Netcode.SyncFullNetworkData(net.ID);
				} else if (adjacent.Count == 1) {
					// Only one adjacent network detected.  Just add the entry to the network
					NetworkInstance net = adjacent[0];

					net.AddEntry(location);

					Netcode.SyncNetworkInstanceEntryPlacement(net, location);
				} else {
					// Combine the existing networks into one network
					NetworkInstance net = adjacent[0];

					net.AddEntry(location);

					// Adjacent networks MUST have a matching fluid type ID or be empty
					if (filter == NetworkType.Fluids) {
						var storage = (net as FluidNetwork).Storage;
						int fluidID = storage.FluidType;

						var adjFluid = new List<NetworkInstance>(adjacent.Skip(1));
						adjacent = new List<NetworkInstance>() { net };

						foreach (var adj in adjFluid.OfType<FluidNetwork>()) {
							var adjStorage = adj.Storage;
							var adjID = adjStorage.FluidType;

							if (adjID > FluidTypeID.None && fluidID == FluidTypeID.None) {
								// Adjacent network will overwrite this network's type
								storage.FluidID = adjStorage.FluidID;
								fluidID = storage.FluidType;

								adjacent.Add(adj);
							} else if (adjID == FluidTypeID.None) {
								// Adjacent network will have its type "overwritten" by this network
								adjacent.Add(adj);
							} else if (adjID == fluidID) {
								// Adjacent network has the same type
								adjacent.Add(adj);
							}
						}
					}

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

		private static List<NetworkInstance> GetCardinalAdjacentNetworks(int x, int y, List<NetworkInstance> source, PermittedAdjacentNetworks allowed, out List<int> indices) {
			List<NetworkInstance> instances = new();
			HashSet<int> netIDs = new();
			indices = new();

			Point16 orig = new Point16(x, y);
			int index = 0;
			foreach (NetworkInstance net in source) {
				if (allowed.left && net.HasEntry(x - 1, y) && netIDs.Add(net.ID) && NetworkInstance.CanContinuePath(orig, orig + new Point16(-1, 0))) {
					instances.Add(net);
					indices.Add(index);
				} else if (allowed.up && net.HasEntry(x, y - 1) && netIDs.Add(net.ID) && NetworkInstance.CanContinuePath(orig, orig + new Point16(0, -1))) {
					instances.Add(net);
					indices.Add(index);
				} else if (allowed.right && net.HasEntry(x + 1, y) && netIDs.Add(net.ID) && NetworkInstance.CanContinuePath(orig, orig + new Point16(1, 0))) {
					instances.Add(net);
					indices.Add(index);
				} else if (allowed.down && net.HasEntry(x, y + 1) && netIDs.Add(net.ID) && NetworkInstance.CanContinuePath(orig, orig + new Point16(0, 1))) {
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
		/// <param name="type">The network types to clear from the tile</param>
		public static void RemoveEntry(int x, int y, NetworkType type) {
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				Netcode.RequestNetworkEntryRemoval(x, y, type);
				return;
			}

			ref NetworkInfo info = ref Main.tile[x, y].Get<NetworkInfo>();

			info.Type &= ~type;
			info.IsPump = false;

			if (info.Type == NetworkType.None)
				info.Connections = ConnectionDirection.None;
			else
				UpdateEntryConnections(x, y);

			UpdateEntryConnections(x - 1, y);
			UpdateEntryConnections(x, y - 1);
			UpdateEntryConnections(x + 1, y);
			UpdateEntryConnections(x, y + 1);

			Netcode.SyncNetworkInfoDiamond(x, y);

			// NOTE: pump timer does not need to be synced... The network will automatically remove it during its update code

			// Split the network into separate networks
			RemoveEntry_CheckFilter(x, y, type, NetworkType.Items, itemNetworks);
			RemoveEntry_CheckFilter(x, y, type, NetworkType.Fluids, fluidNetworks);
			RemoveEntry_CheckFilter(x, y, type, NetworkType.Power, powerNetworks);
		}

		private static void RemoveEntry_CheckFilter(int x, int y, NetworkType type, NetworkType filter, List<NetworkInstance> source) {
			if ((type & filter) == filter) {
				// Force a ToList() since the collection will be modified
				var networks = GetNetworksAt(x, y, type & filter).ToList();
				// Reverse the collection so that networks with higher indices are processed first
				networks.Reverse();

				foreach (var parent in networks) {
					if (parent.EntryCount <= 1) {
						// This was a single-entry network.  Just remove it
						Netcode.SendNetworkRemoval(parent.ID);
					
						source.RemoveAt(parent.networkIndex);
						parent.Dispose();
					} else {
						// Remove the parent since it won't be returned by RemoveEntry
						source.RemoveAt(parent.networkIndex);

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
		}

		/// <summary>
		/// Attempts to find a network at the location (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y=coordinate</param>
		/// <param name="type">The filter to use when checking for networks.  Multiple network types can be searched through via OR-ing multiple <see cref="NetworkType"/> constants</param>
		/// <returns>The first valid <see cref="NetworkInstance"/> object, or <see langword="null"/> if one wasn't found at the provided location</returns>
		public static NetworkInstance GetNetworkAt(int x, int y, NetworkType type) {
			Point16 loc = new Point16(x, y);

			int networkIndex;
			if ((type & NetworkType.Items) == NetworkType.Items) {
				networkIndex = 0;
				foreach (NetworkInstance net in itemNetworks) {
					net.networkIndex = networkIndex;

					if (net.HasEntry(loc))
						return net;

					networkIndex++;
				}
			}

			if ((type & NetworkType.Fluids) == NetworkType.Fluids) {
				networkIndex = 0;
				foreach (NetworkInstance net in fluidNetworks) {
					net.networkIndex = networkIndex;

					if (net.HasEntry(loc))
						return net;

					networkIndex++;
				}
			}

			if ((type & NetworkType.Power) == NetworkType.Power) {
				networkIndex = 0;
				foreach (NetworkInstance net in powerNetworks) {
					net.networkIndex = networkIndex;

					if (net.HasEntry(loc))
						return net;

					networkIndex++;
				}
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
		/// <returns>The first <see cref="ItemNetwork"/> instance if one was found, <see langword="null"/> otherwise</returns>
		public static ItemNetwork GetItemNetworkAt(int x, int y) => GetNetworkAt(x, y, NetworkType.Items) as ItemNetwork;

		/// <summary>
		/// Attempts to find a fluid network at location (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <returns>The first <see cref="FluidNetwork"/> instance if one was found, <see langword="null"/> otherwise</returns>
		public static FluidNetwork GetFluidNetworkAt(int x, int y) => GetNetworkAt(x, y, NetworkType.Fluids) as FluidNetwork;

		/// <summary>
		/// Attempts to find a power network at location (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <returns>The first <see cref="PowerNetwork"/> instance if one was found, <see langword="null"/> otherwise</returns>
		public static PowerNetwork GetPowerNetworkAt(int x, int y) => GetNetworkAt(x, y, NetworkType.Power) as PowerNetwork;

		/// <summary>
		/// Attempts to find any networks at the location (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y=coordinate</param>
		/// <param name="type">The filter to use when checking for networks.  Multiple network types can be searched through via OR-ing multiple <see cref="NetworkType"/> constants</param>
		/// <returns>An enumeration of valid <see cref="NetworkInstance"/> objects if any where found, an empty enumeration otherwise</returns>
		public static IEnumerable<NetworkInstance> GetNetworksAt(int x, int y, NetworkType type) {
			Point16 loc = new Point16(x, y);

			int networkIndex;
			if ((type & NetworkType.Items) == NetworkType.Items) {
				networkIndex = 0;
				foreach (NetworkInstance net in itemNetworks) {
					net.networkIndex = networkIndex;

					if (net.HasEntry(loc))
						yield return net;

					networkIndex++;
				}
			}

			if ((type & NetworkType.Fluids) == NetworkType.Fluids) {
				networkIndex = 0;
				foreach (NetworkInstance net in fluidNetworks) {
					net.networkIndex = networkIndex;

					if (net.HasEntry(loc))
						yield return net;

					networkIndex++;
				}
			}

			if ((type & NetworkType.Power) == NetworkType.Power) {
				networkIndex = 0;
				foreach (NetworkInstance net in powerNetworks) {
					net.networkIndex = networkIndex;

					if (net.HasEntry(loc))
						yield return net;

					networkIndex++;
				}
			}
		}

		/// <summary>
		/// Attempts to find an item network at location (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <returns>An enumeration of <see cref="ItemNetwork"/> instances if any were found, an empty enumeration otherwise</returns>
		public static IEnumerable<ItemNetwork> GetItemNetworksAt(int x, int y) => GetNetworksAt(x, y, NetworkType.Items).OfType<ItemNetwork>();

		/// <summary>
		/// Attempts to find a fluid network at location (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <returns>An enumeration of <see cref="FluidNetwork"/> instances if any were found, an empty enumeration otherwise</returns>
		public static IEnumerable<FluidNetwork> GetFluidNetworksAt(int x, int y) => GetNetworksAt(x, y, NetworkType.Fluids).OfType<FluidNetwork>();

		/// <summary>
		/// Attempts to find a power network at location (<paramref name="x"/>, <paramref name="y"/>)
		/// </summary>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		/// <returns>An enumeration of <see cref="PowerNetwork"/> instances if any were found, an empty enumeration otherwise</returns>
		public static IEnumerable<PowerNetwork> GetPowerNetworksAt(int x, int y) => GetNetworksAt(x, y, NetworkType.Power).OfType<PowerNetwork>();

		internal static void UpdateEntryConnections(int x, int y) {
			Tile tile = Main.tile[x, y];
			ref NetworkInfo info = ref tile.Get<NetworkInfo>();

			// TileFrame updates the connection bits
			if (info.Type != NetworkType.None && TileLoader.GetTile(tile.TileType) is BaseNetworkTile) {
				bool resetFrame = false, noBreak = false;

				TileLoader.TileFrame(x, y, tile.TileType, ref resetFrame, ref noBreak);
			}
		}
	}
}
