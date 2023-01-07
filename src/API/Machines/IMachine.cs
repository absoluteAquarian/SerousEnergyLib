using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.API.Upgrades;
using SerousEnergyLib.Items;
using SerousEnergyLib.Systems;
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
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// The base interface used by all machine tile entities
	/// </summary>
	public interface IMachine {
		#pragma warning disable CS1591
		/// <summary>
		/// The ID of the tile that this machine should be bound to.
		/// </summary>
		int MachineTile { get; }

		/// <summary>
		/// The UI instance bound to this machine type.
		/// </summary>
		BaseMachineUI MachineUI { get; }

		/// <summary>
		/// This method returns true if the tile at location (<paramref name="x"/>, <paramref name="y"/>) is active, its type is equal to <see cref="MachineTile"/> and its sprite frame is at position (0, 0)
		/// </summary>
		/// <param name="machine">The machine to retrieve the tile type from</param>
		/// <param name="x">The tile X-coordinate</param>
		/// <param name="y">The tile Y-coordinate</param>
		public static bool IsTileValid(IMachine machine, int x, int y) {
			Tile tile = Main.tile[x, y];
			return tile.HasTile && tile.TileType == machine.MachineTile && tile.TileFrameX == 0 && tile.TileFrameY == 0;
		}

		/// <summary>
		/// The collection of upgrades contained within this machine
		/// </summary>
		List<StackedUpgrade> Upgrades { get; set; }

		/// <summary>
		/// This method ensures that <see cref="Upgrades"/> is not <see langword="null"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		public static void Update(IMachine machine) {
			// Ensure that the upgrades collection isn't null
			machine.Upgrades ??= new();
		}

		/// <summary>
		/// Attempts to find a machine bound to a multitile at <paramref name="location"/>
		/// </summary>
		/// <param name="location">The tile coordinates to look for a machine's entity at</param>
		/// <param name="machine">The machine instance if one was found</param>
		/// <returns>Whether a machine entity could be found</returns>
		public static bool TryFindMachine(Point16 location, out IMachine machine) {
			Point16 topleft = TileFunctions.GetTopLeftTileInMultitile(location.X, location.Y);

			if (TileEntity.ByPosition.TryGetValue(topleft, out TileEntity entity) && entity is IMachine m) {
				machine = m;
				return true;
			}

			machine = null;
			return false;
		}

		internal static ModTileEntity PlaceInWorld(IMachine machine, Point16 location) {
			if (machine is not ModTileEntity entity)
				throw new ArgumentException("IMachine parameter was not a ModTileEntity", nameof(machine));

			if (entity.Find(location.X, location.Y) == -1) {
				int id = entity.Place(location.X, location.Y);

				if (Main.netMode == NetmodeID.MultiplayerClient)
					NetMessage.SendData(MessageID.TileEntitySharing, ignoreClient: Main.myPlayer, number: id);

				ModTileEntity placed = TileEntity.ByID[id] as ModTileEntity;

				if (placed is not IMachine placedMachine)
					return null;

				placedMachine.AddToAdjacentNetworks();

				Netcode.SyncMachinePlacement(placed.Type, location);

				return placed;
			}

			return null;
		}

		internal static TagCompound RemoveFromWorld(Point16 location) {
			if (!TryFindMachine(location, out IMachine machine))
				return null;

			if (machine is not ModTileEntity entity)
				throw new InvalidOperationException("IMachine was not a ModTileEntity");

			TagCompound tag = new();
			entity.SaveData(tag);

			machine.RemoveFromAdjacentNetworks();

			entity.Kill(entity.Position.X, entity.Position.Y);

			if (Main.netMode == NetmodeID.MultiplayerClient)
				NetMessage.SendData(MessageID.TileEntitySharing, ignoreClient: Main.myPlayer, number: entity.ID);

			Netcode.SyncMachineRemoval(entity.Type, location);

			return tag;
		}

		internal void AddToAdjacentNetworks() {
			NetworkType search = NetworkType.None;
			if (this is IInventoryMachine)
				search |= NetworkType.Items;
			if (this is IFluidMachine)
				search |= NetworkType.Fluids;
			if (this is IPoweredMachine)
				search |= NetworkType.Power;

			if (search != NetworkType.None) {
				foreach (var result in GetAdjacentNetworks(search)) {
					var net = result.network;

					if (net is ItemNetwork itemNet)
						itemNet.AddAdjacentInventory(result.machineTileAdjacentToNetwork);
					else if (net is FluidNetwork fluidNet)
						fluidNet.AddAdjacentFluidStorage(result.machineTileAdjacentToNetwork);
					else if (net is PowerNetwork powerNet)
						powerNet.AddAdjacentFluxStorage(result.machineTileAdjacentToNetwork);
				}
			}
		}

		internal void RemoveFromAdjacentNetworks() {
			NetworkType search = NetworkType.None;
			if (this is IInventoryMachine)
				search |= NetworkType.Items;
			if (this is IFluidMachine)
				search |= NetworkType.Fluids;
			if (this is IPoweredMachine)
				search |= NetworkType.Power;

			if (search != NetworkType.None) {
				foreach (var result in GetAdjacentNetworks(search)) {
					var net = result.network;

					if (net is ItemNetwork itemNet)
						itemNet.RemoveAdjacentInventory(result.machineTileAdjacentToNetwork);
					else if (net is FluidNetwork fluidNet)
						fluidNet.RemoveAdjacentFluidStorage(result.machineTileAdjacentToNetwork);
					else if (net is PowerNetwork powerNet)
						powerNet.RemoveAdjacentFluxStorage(result.machineTileAdjacentToNetwork);
				}
			}
		}

		/// <summary>
		/// Iterates over <see cref="Upgrades"/> and applies <paramref name="mutator"/> to each of them
		/// </summary>
		/// <param name="base">The default value for the calculated result</param>
		/// <param name="mutator">A function which modifies the calculated result</param>
		/// <returns>The final calculated result</returns>
		protected T CalculateFromUpgrades<T>(T @base, Func<StackedUpgrade, T, T> mutator) {
			T calculated = @base;

			foreach (var upgrade in Upgrades) {
				// Invalid upgrades shouldn't be in the collection in the first place, but it's a good idea to double check here
				if (upgrade.Upgrade.CanApplyTo(this))
					calculated = mutator(upgrade, calculated);
			}

			return calculated;
		}

		/// <summary>
		/// Iterates over <paramref name="upgrades"/> and applies <paramref name="mutator"/> to each of them
		/// </summary>
		/// <param name="upgrades">An enumeration of upgrades</param>
		/// <inheritdoc cref="CalculateFromUpgrades{T}(T, Func{StackedUpgrade, T, T})"/>
		/// <param name="base"></param>
		/// <param name="mutator"></param>
		protected T CalculateFromUpgrades<T>(T @base, IEnumerable<StackedUpgrade> upgrades, Func<StackedUpgrade, T, T> mutator) {
			T calculated = @base;

			foreach (var upgrade in upgrades) {
				// Invalid upgrades shouldn't be in the collection in the first place, but it's a good idea to double check here
				if (upgrade.Upgrade.CanApplyTo(this))
					calculated = mutator(upgrade, calculated);
			}

			return calculated;
		}

		/// <summary>
		/// Adds <paramref name="upgrade"/> to the upgrade collection in <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="upgrade">The upgrade to add</param>
		/// <returns>Whether <paramref name="upgrade"/> could be added to <paramref name="machine"/></returns>
		public static bool AddUpgrade(IMachine machine, BaseUpgrade upgrade) {
			if (!upgrade.CanApplyTo(machine))
				return false;

			int type = upgrade.Type;
			var existing = machine.Upgrades.Where(u => u.Upgrade.Type == type).FirstOrDefault();

			if (existing is null) {
				machine.Upgrades.Add(new StackedUpgrade() { Stack = 1, Upgrade = upgrade });

				Netcode.SyncMachineUpgrades(machine);
				return true;
			}

			if (existing.Stack + 1 >= upgrade.MaxUpgradesPerMachine)
				return false;

			existing.Stack++;
			Netcode.SyncMachineUpgrades(machine);
			return true;
		}

		/// <summary>
		/// Removes upgrades from <paramref name="machine"/> whose type equals <paramref name="type"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="type">The ID of the <see cref="BaseUpgrade"/> to remove</param>
		/// <param name="stackToRemove">How many upgrades to remove.  To remove all of them, pass <see cref="int.MaxValue"/> to this parameter</param>
		/// <param name="removedUpgradeItems">A collection of <see cref="BaseUpgradeItem"/> items corresponding to the removed items, if any</param>
		/// <returns>Whether any upgrades were removed</returns>
		public static bool RemoveUpgrade(IMachine machine, int type, int stackToRemove, out List<Item> removedUpgradeItems) {
			var instance = UpgradeLoader.Get(type);

			if (instance is null)
				throw new ArgumentException("Requested ID does not correspond to a valid BaseUpgrade instance ID");

			removedUpgradeItems = new();

			var existing = machine.Upgrades.Where(u => u.Upgrade.Type == type);

			if (!existing.Any())
				return false;

			var toRemove = existing.ToList();

			static Item CreateUpgradeItem(StackedUpgrade obj, int stack) {
				int itemType = obj.Upgrade.ItemType;
				Item item = new Item(itemType, stack);

				if (item.ModItem is not BaseUpgradeItem)
					throw new InvalidOperationException($"ItemType property for upgrade type \"{obj.Upgrade.Mod.Name}:{obj.Upgrade.Name}\" did not correspond to a BaseUpgradeItem instance");

				return item;
			}

			foreach (var obj in toRemove) {
				if (obj.Stack >= stackToRemove) {
					removedUpgradeItems.Add(CreateUpgradeItem(obj, stackToRemove));

					obj.Stack -= stackToRemove;
					Netcode.SyncMachineUpgrades(machine);
					return true;
				}

				removedUpgradeItems.Add(CreateUpgradeItem(obj, obj.Stack));
				
				stackToRemove -= obj.Stack;
				obj.Stack = 0;

				machine.Upgrades.Remove(obj);
			}

			Netcode.SyncMachineUpgrades(machine);
			return true;
		}

		/// <summary>
		/// A struct representing a search result for an adjacent network
		/// </summary>
		public readonly struct NetworkSearchResult {
			/// <summary>
			/// The adjacent network instance
			/// </summary>
			public readonly NetworkInstance network;

			/// <summary>
			/// The tile in the network that was adjacent to <see cref="machineTileAdjacentToNetwork"/>
			/// </summary>
			public readonly Point16 tileInNetwork;

			/// <summary>
			/// The tile within this machine that is adjacent to the network
			/// </summary>
			public readonly Point16 machineTileAdjacentToNetwork;

			internal NetworkSearchResult(NetworkInstance instance, Point16 location, Point16 adjacentLocation) {
				network = instance;
				tileInNetwork = location;
				machineTileAdjacentToNetwork = adjacentLocation;
			}
		}

		public IEnumerable<NetworkSearchResult> GetAdjacentNetworks(NetworkType type, bool allowDuplicates = false) {
			if (this is not ModTileEntity entity)
				yield break;

			if (TileLoader.GetTile(MachineTile) is not IMachineTile machineTile)
				yield break;

			HashSet<int> returnedIds = new();

			// Check all adjacent tiles in the cardinal directions
			machineTile.GetMachineDimensions(out uint width, out uint height);

			int x = -1;
			int y;

			// Check left edge
			for (y = 0; y < height; y++) {
				if (Network.GetNetworkAt(entity.Position.X + x, entity.Position.Y + y, type) is NetworkInstance net && (allowDuplicates || returnedIds.Add(net.ID)))
					yield return new NetworkSearchResult(net, new Point16(entity.Position.X + x, entity.Position.Y + y), new Point16(entity.Position.X, entity.Position.Y + y));
			}

			// Check top edge
			y = -1;
			for (x = 0; x < width; x++) {
				if (Network.GetNetworkAt(entity.Position.X + x, entity.Position.Y + y, type) is NetworkInstance net && (allowDuplicates || returnedIds.Add(net.ID)))
					yield return new NetworkSearchResult(net, new Point16(entity.Position.X + x, entity.Position.Y + y), new Point16(entity.Position.X + x, entity.Position.Y));
			}

			// Check right edge
			x = (int)width;
			for (y = 0; y < height; y++) {
				if (Network.GetNetworkAt(entity.Position.X + x, entity.Position.Y + y, type) is NetworkInstance net && (allowDuplicates || returnedIds.Add(net.ID)))
					yield return new NetworkSearchResult(net, new Point16(entity.Position.X + x, entity.Position.Y + y), new Point16(entity.Position.X, entity.Position.Y + y));
			}

			// Check bottom edge
			y = (int)height;
			for (x = 0; x < width; x++) {
				if (Network.GetNetworkAt(entity.Position.X + x, entity.Position.Y + y, type) is NetworkInstance net && (allowDuplicates || returnedIds.Add(net.ID)))
					yield return new NetworkSearchResult(net, new Point16(entity.Position.X + x, entity.Position.Y + y), new Point16(entity.Position.X + x, entity.Position.Y));
			}
		}

		public static void SaveUpgrades(IMachine machine, TagCompound tag) {
			tag["upgrades"] = machine.Upgrades.Select(static s => s.Save()).ToList();
		}

		public static void LoadUpgrades(IMachine machine, TagCompound tag) {
			machine.Upgrades = tag.GetList<TagCompound>("upgrades") is List<TagCompound> list
				? list.Select(StackedUpgrade.Load).Where(static s => s.Upgrade is not null).ToList()
				: new();
		}
	}
}
