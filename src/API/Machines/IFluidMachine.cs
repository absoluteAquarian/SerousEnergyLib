using SerousEnergyLib.API.Fluid;
using SerousEnergyLib.Systems.Networks;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using System.Linq;
using System.Collections.Generic;
using SerousEnergyLib.TileData;
using System;
using SerousEnergyLib.API.Upgrades;
using Terraria.ModLoader;
using SerousEnergyLib.Tiles;
using Terraria;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface containing methods used by machines that can store fluids
	/// </summary>
	public interface IFluidMachine : IMachine {
		/// <summary>
		/// The fluid storages within this machine
		/// </summary>
		FluidStorage[] FluidStorage { get; protected set; }

		/// <summary>
		/// Whether the fluid pipe at position (<paramref name="pipeX"/>, <paramref name="pipeY"/>) can merge with this machine's sub-tile at position (<paramref name="machineX"/>, <paramref name="machineY"/>)
		/// </summary>
		/// <param name="pipeX">The tile X-coordinate for the fluid pipe</param>
		/// <param name="pipeY">The tile Y-coordinate for the fluid pipe</param>
		/// <param name="machineX">The tile X-coordinate for the machine sub-tile</param>
		/// <param name="machineY">The tile Y-coordinate for the machine sub-tile</param>
		bool CanMergeWithFluidPipe(int pipeX, int pipeY, int machineX, int machineY);

		/// <summary>
		/// Select which fluid inventory can be exported from a given <paramref name="pump"/> location at the given <paramref name="subtile"/> location within this machine
		/// </summary>
		/// <param name="pump">The tile coordinates of the pump tile</param>
		/// <param name="subtile">The tile coordinates of which sub-tile of this machine is being pumped from</param>
		/// <returns>An instance from <see cref="FluidStorage"/> or <see langword="null"/> if no fluids can be extracted</returns>
		FluidStorage SelectFluidExportSource(Point16 pump, Point16 subtile);

		/// <summary>
		/// Select which fluid inventory can be imported into from a given <paramref name="pipe"/> location at the given <paramref name="subtile"/> location within this machine
		/// </summary>
		/// <param name="pipe">The tile coordinates of the <see cref="IFluidTransportTile"/> tile</param>
		/// <param name="subtile">The tile coordinates of which sub-tile of this machine is being imported into</param>
		/// <returns>An instance from <see cref="FluidStorage"/> or <see langword="null"/> if no fluid storages can be imported into</returns>
		FluidStorage SelectFluidImportDestination(Point16 pipe, Point16 subtile);

		/// <summary>
		/// Whether <paramref name="upgrade"/> can apply to the storage in <see cref="FluidStorage"/> at index <paramref name="slot"/>
		/// </summary>
		/// <param name="upgrade">The upgrade to check</param>
		/// <param name="slot">The index on <see cref="FluidStorage"/></param>
		bool CanUpgradeApplyTo(BaseUpgrade upgrade, int slot);

		/// <summary>
		/// Returns an enumeration of <see cref="FluidNetwork"/> instances that are adjacent to this machine
		/// </summary>
		public IEnumerable<FluidNetwork> GetAdjacentFluidNetworks() {
			return GetAdjacentNetworks(NetworkType.Fluids)
				.Select(r => r.network as FluidNetwork)
				.OfType<FluidNetwork>();
		}

		/// <summary>
		/// Attempts to find an adjacent transport tile in <see cref="FluidNetwork"/> with the highest transfer rate
		/// </summary>
		/// <param name="machine">The machine to try to export into</param>
		/// <param name="source">The network to examine</param>
		/// <param name="export">The highest exported rate, or zero if one could not be found.</param>
		/// <param name="exportTileLocation">The tile location of the <see cref="IFluidTransportTile"/> being exported from</param>
		/// <param name="importSubtileLocation">The tile location of the sub-tile in <paramref name="machine"/> being imported into</param>
		public static bool TryGetHighestExportRate(IFluidMachine machine, FluidNetwork source, out double export, out Point16 exportTileLocation, out Point16 importSubtileLocation) {
			// Find the tile adjacent to this machine with the highest export rate and use it
			var adjacent = machine.GetAdjacentNetworks(NetworkType.Fluids, allowDuplicates: true);

			double maxExport = 0;
			bool entryExists = false;
			exportTileLocation = Point16.NegativeOne;
			importSubtileLocation = Point16.NegativeOne;

			foreach (var result in adjacent) {
				if (result.network.ID != source.ID)
					continue;

				Point16 loc = result.tileInNetwork;

				if (TileLoader.GetTile(Main.tile[loc.X, loc.Y].TileType) is IFluidTransportTile transport && transport.ExportRate > maxExport) {
					maxExport = transport.ExportRate;
					exportTileLocation = loc;
					importSubtileLocation = result.machineTileAdjacentToNetwork;
					entryExists = true;
				}
			}

			export = maxExport;
			return entryExists;
		}

		/// <summary>
		/// This method applies any upgrades that can increase fluid storage capacity
		/// </summary>
		/// <param name="machine">The machine to process</param>
		public static void Update(IFluidMachine machine) {
			machine.FluidStorage ??= Array.Empty<FluidStorage>();

			for (int i = 0; i < machine.FluidStorage.Length; i++) {
				var storage = machine.FluidStorage[i];

				// Local capture for lambda
				int slot = i;

				storage.MaxCapacity = machine.CalculateFromUpgrades(StatModifier.Default,
					machine.Upgrades.Where(u => machine.CanUpgradeApplyTo(u.Upgrade, slot)),
					static (u, s, v) => u.GetFluidCapacityMultiplier(s).CombineWith(v))
					.ApplyTo(storage.BaseMaxCapacity);

				if (storage.CurrentCapacity > storage.MaxCapacity)
					storage.CurrentCapacity = storage.MaxCapacity;
			}
		}

		#pragma warning disable CS1591
		public static void SaveFluids(IFluidMachine machine, TagCompound tag) {
			static TagCompound SaveFluid(FluidStorage storage) {
				TagCompound fluidTag = new TagCompound();
				storage.SaveData(fluidTag);
				return fluidTag;
			}

			tag["fluids"] = machine.FluidStorage.Select(SaveFluid).ToList();
		}

		public static void LoadFluids(IFluidMachine machine, TagCompound tag) {
			var storage = machine.FluidStorage;
			if (tag.GetList<TagCompound>("fluids") is List<TagCompound> fluids && fluids.Count == storage.Length) {
				for (int i = 0; i < storage.Length; i++)
					storage[i].LoadData(fluids[i]);
			}
		}
	}
}
