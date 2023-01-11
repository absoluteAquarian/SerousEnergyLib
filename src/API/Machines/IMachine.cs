using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.API.Upgrades;
using SerousEnergyLib.Items;
using SerousEnergyLib.Systems;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System;
using System.Collections.Generic;
using System.IO;
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
		List<BaseUpgradeItem> Upgrades { get; set; }

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

		/// <inheritdoc cref="TryFindMachine(Point16, out IMachine)"/>
		public static bool TryFindMachine<T>(Point16 location, out T machine) where T : IMachine {
			Point16 topleft = TileFunctions.GetTopLeftTileInMultitile(location.X, location.Y);

			if (TileEntity.ByPosition.TryGetValue(topleft, out TileEntity entity) && entity is T m) {
				machine = m;
				return true;
			}

			machine = default;
			return false;
		}

		/// <summary>
		/// Attempts to find a machine at <paramref name="location"/>
		/// </summary>
		/// <param name="location">The exact tile coordinates to for a machine's entity at</param>
		/// <param name="machine">The machine instanc if one was found</param>
		/// <returns>Whether a machine entity could be found</returns>
		public static bool TryFindMachineExact(Point16 location, out IMachine machine) {
			if (TileEntity.ByPosition.TryGetValue(location, out TileEntity entity) && entity is IMachine m) {
				machine = m;
				return true;
			}

			machine = null;
			return false;
		}

		/// <inheritdoc cref="TryFindMachineExact(Point16, out IMachine)"/>
		public static bool TryFindMachineExact<T>(Point16 location, out T machine) where T : IMachine {
			if (TileEntity.ByPosition.TryGetValue(location, out TileEntity entity) && entity is T m) {
				machine = m;
				return true;
			}

			machine = default;
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
				foreach (var result in GetAdjacentNetworks(this, search)) {
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
				foreach (var result in GetAdjacentNetworks(this, search)) {
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
		/// <param name="machine">The machine to process</param>
		/// <param name="base">The default value for the calculated result</param>
		/// <param name="mutator">A function which modifies the calculated result</param>
		/// <returns>The final calculated result</returns>
		protected static T CalculateFromUpgrades<T>(IMachine machine, T @base, Func<BaseUpgrade, int, T, T> mutator) {
			T calculated = @base;

			foreach (var upgrade in machine.Upgrades) {
				// Invalid upgrades shouldn't be in the collection in the first place, but it's a good idea to double check here
				if (upgrade.Upgrade.CanApplyTo(machine))
					calculated = mutator(upgrade.Upgrade, upgrade.Stack, calculated);
			}

			return calculated;
		}

		/// <summary>
		/// Iterates over <paramref name="upgrades"/> and applies <paramref name="mutator"/> to each of them
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="upgrades">An enumeration of upgrades</param>
		/// <inheritdoc cref="CalculateFromUpgrades{T}(IMachine, T, Func{BaseUpgrade, int, T, T})"/>
		/// <param name="base"></param>
		/// <param name="mutator"></param>
		protected static T CalculateFromUpgrades<T>(IMachine machine, T @base, IEnumerable<BaseUpgradeItem> upgrades, Func<BaseUpgrade, int, T, T> mutator) {
			T calculated = @base;

			foreach (var upgrade in upgrades) {
				// Invalid upgrades shouldn't be in the collection in the first place, but it's a good idea to double check here
				if (upgrade.Upgrade.CanApplyTo(machine))
					calculated = mutator(upgrade.Upgrade, upgrade.Stack, calculated);
			}

			return calculated;
		}

		/// <summary>
		/// Applies <see cref="BaseUpgrade.GetLuckPercentageMultiplier(int)"/> to <paramref name="orig"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="orig">The original luck threshold, given the equation:<br/>
		/// <c>Main.rand.NextDouble() &lt; orig</c></param>
		/// <returns></returns>
		public static double GetLuckThreshold(IMachine machine, double orig)
			=> CalculateFromUpgrades(machine, StatModifier.Default, static (u, s, v) => u.GetLuckPercentageMultiplier(s).CombineWith(v)).ApplyTo(orig);

		/// <summary>
		/// Adds <paramref name="upgrade"/> to the upgrade collection in <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="upgrade">The upgrade to add</param>
		/// <returns>Whether <paramref name="upgrade"/> could be added to <paramref name="machine"/></returns>
		public static bool AddUpgrade(IMachine machine, BaseUpgradeItem upgrade) {
			// Allow callees to do things like "IMachine.AddUpgrade(machine, item.ModItem as BaseUpgradeItem)"
			if (upgrade is null)
				return false;

			if (!upgrade.Upgrade.CanApplyTo(machine))
				return false;

			int type = upgrade.Type;
			var existing = machine.Upgrades.Where(u => u.Upgrade.Type == type).FirstOrDefault();

			if (existing is null) {
				var clone = upgrade.Item.Clone().ModItem as BaseUpgradeItem;

				machine.Upgrades.Add(clone);
				upgrade.Item.TurnToAir();

				Netcode.SyncMachineUpgrades(machine);
				return true;
			}

			int max = upgrade.Upgrade.MaxUpgradesPerMachine;

			if (existing.Stack >= max)
				return false;

			if (existing.Stack + upgrade.Stack > max) {
				int diff = max - existing.Stack;
				existing.Stack = max;
				upgrade.Stack -= diff;
				return true;
			}

			existing.Stack += upgrade.Stack;

			upgrade.Item.TurnToAir();

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

		/// <summary>
		/// Returns an enumeration of the adjacent networks to <paramref name="machine"/> which satisfy the filter, <paramref name="type"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="type">The network types to filter</param>
		/// <param name="allowDuplicates">Whether a network can appear multiple times in the enumeration</param>
		public static IEnumerable<NetworkSearchResult> GetAdjacentNetworks(IMachine machine, NetworkType type, bool allowDuplicates = false) {
			if (machine is not ModTileEntity entity)
				yield break;

			if (TileLoader.GetTile(machine.MachineTile) is not IMachineTile machineTile)
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

		public static void SaveData(IMachine machine, TagCompound tag) {
			tag["upgrades"] = machine.Upgrades.Select(UpgradeLoader.SaveUpgrade).ToList();
		}

		public static void LoadData(IMachine machine, TagCompound tag) {
			machine.Upgrades = tag.GetList<TagCompound>("upgrades") is List<TagCompound> list
				? list.Select(UpgradeLoader.LoadUpgrade).OfType<BaseUpgradeItem>().Where(static s => s.Upgrade is not null).ToList()
				: new();
		}

		public static void NetSend(IMachine machine, BinaryWriter writer) {
			writer.Write((short)machine.Upgrades.Count);

			foreach (var upgrade in machine.Upgrades) {
				writer.Write(upgrade.Type);
				writer.Write((short)upgrade.Stack);
			}
		}

		public static void NetReceive(IMachine machine, BinaryReader reader) {
			if (machine.Upgrades is null)
				machine.Upgrades = new();
			else
				machine.Upgrades.Clear();

			short count = reader.ReadInt16();

			for (int i = 0; i < count; i++) {
				BaseUpgradeItem upgradeItem = new Item(reader.ReadInt32()).ModItem as BaseUpgradeItem
					?? throw new IOException("Item ID did not refer to a BaseUpgradeItem instance");
				short stack = reader.ReadInt16();

				upgradeItem.Stack = stack;

				machine.Upgrades.Add(upgradeItem);
			}
		}
	}
}
