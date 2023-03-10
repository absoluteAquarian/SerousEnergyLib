using SerousEnergyLib.API.Energy;
using SerousEnergyLib.Systems.Networks;
using Terraria.ModLoader.IO;
using System.Collections.Generic;
using SerousEnergyLib.TileData;
using System.Linq;
using Terraria.ModLoader;
using SerousEnergyLib.API.Upgrades;
using SerousEnergyLib.Tiles;
using Terraria.DataStructures;
using Terraria;
using System.IO;
using SerousEnergyLib.Systems;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface containing methods used by machines that can consume power
	/// </summary>
	public interface IPoweredMachine : IMachine {
		/// <summary>
		/// The power storage within this machine
		/// </summary>
		FluxStorage PowerStorage { get; }

		/// <summary>
		/// The <see cref="EnergyTypeID"/> that this machine uses
		/// </summary>
		int EnergyID { get; }

		/// <summary>
		/// Return how much power in units represented by <see cref="EnergyID"/> should be consumed for a duration of <paramref name="ticks"/>
		/// </summary>
		/// <param name="ticks">The amount of game ticks to calculate</param>
		double GetPowerConsumption(double ticks);

		/// <summary>
		/// Applies <see cref="BaseUpgrade.GetPowerConsumptionMultiplier(int)"/> to the result of <see cref="GetPowerConsumption(double)"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="ticks">The amount of game ticks to calculate</param>
		public static double GetPowerConsumptionWithUpgrades(IPoweredMachine machine, double ticks)
			=> CalculateFromUpgrades(machine, StatModifier.Default, static (u, s, v) => u.GetPowerConsumptionMultiplier(s).CombineWith(v))
				.ApplyTo(machine.GetPowerConsumption(ticks));

		/// <summary>
		/// Attempts to consume power from the flux storage in <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="power">A quantity of power or the default consumption rate per tick (see: <see cref="GetPowerConsumptionWithUpgrades(IPoweredMachine, double)"/>) if <see langword="null"/>.</param>
		/// <returns>Whether there was enough power for consumption</returns>
		public static bool AttemptToConsumePower(IPoweredMachine machine, double? power = null) {
			double consume = power ?? GetPowerConsumptionWithUpgrades(machine, 1);
			TerraFlux flux = EnergyConversions.ConvertToTerraFlux(consume, machine.EnergyID);

			if (flux <= machine.PowerStorage.CurrentCapacity) {
				machine.PowerStorage.Export(ref flux);

				Netcode.SyncMachinePowerStorage(machine);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Whether the wire at position (<paramref name="wireX"/>, <paramref name="wireY"/>) can merge with this machine's sub-tile at position (<paramref name="machineX"/>, <paramref name="machineY"/>)
		/// </summary>
		/// <param name="wireX">The tile X-coordinate for the wire</param>
		/// <param name="wireY">The tile Y-coordinate for the wire</param>
		/// <param name="machineX">The tile X-coordinate for the machine sub-tile</param>
		/// <param name="machineY">The tile Y-coordinate for the machine sub-tile</param>
		bool CanMergeWithWire(int wireX, int wireY, int machineX, int machineY);

		/// <summary>
		/// Returns an enumeration of all <see cref="PowerNetwork"/> instances that are adjacent to this machine
		/// </summary>
		/// <param name="machine">The machine to process</param>
		public static IEnumerable<PowerNetwork> GetAdjacentPowerNetworks(IPoweredMachine machine) {
			return GetAdjacentNetworks(machine, NetworkType.Power)
				.Where(r => machine.CanMergeWithWire(r.tileInNetwork.X, r.tileInNetwork.Y, r.machineTileAdjacentToNetwork.X, r.machineTileAdjacentToNetwork.Y))
				.Select(r => r.network as PowerNetwork)
				.OfType<PowerNetwork>();
		}

		/// <summary>
		/// Attempts to find an adjacent transport tile in <see cref="PowerNetwork"/> with the highest transfer rate
		/// </summary>
		/// <param name="machine">The machine to try to export from or insert into</param>
		/// <param name="source">The network to examine</param>
		/// <param name="transfer">The highest transfer rate, or <see cref="TerraFlux.Zero"/> if one could not be found.</param>
		/// <param name="exportTileLocation">The tile location of the <see cref="IPowerTransportTile"/> being exported from</param>
		/// <param name="importSubtileLocation">The tile location of the sub-tile in <paramref name="machine"/> being imported into</param>
		public static bool TryGetHighestTransferRate(IPoweredMachine machine, PowerNetwork source, out TerraFlux transfer, out Point16 exportTileLocation, out Point16 importSubtileLocation) {
			// Find the tile adjacent to this machine with the highest export rate and use it
			var adjacent = GetAdjacentNetworks(machine, NetworkType.Power, allowDuplicates: true);

			TerraFlux maxTransfer = TerraFlux.Zero;
			bool entryExists = false;
			exportTileLocation = Point16.NegativeOne;
			importSubtileLocation = Point16.NegativeOne;

			foreach (var result in adjacent) {
				if (result.network.ID != source.ID)
					continue;

				Point16 loc = result.tileInNetwork;

				if (TileLoader.GetTile(Main.tile[loc.X, loc.Y].TileType) is IPowerTransportTile transport && transport.TransferRate > maxTransfer) {
					maxTransfer = transport.TransferRate;
					exportTileLocation = loc;
					importSubtileLocation = result.machineTileAdjacentToNetwork;
					entryExists = true;
				}
			}

			transfer = maxTransfer;
			return entryExists;
		}

		/// <summary>
		/// Exports power from the flux storage in <paramref name="machine"/> to all adjacent <see cref="PowerNetwork"/> instances
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="mode">The mode used to select the order in which the adjacent networks are processed</param>
		public static void ExportPowerToAdjacentNetworks(IPoweredMachine machine, PowerExportPriority mode) {
			// I'd love to use lazy evaluation for this, but it doesn't play nice with Enumerable.Count()
			var nets = OrderByMode(GetAdjacentPowerNetworks(machine), mode).ToList();

			List<FluxStorage> splitStorages = null;
			if (nets.Count > 0 && mode == PowerExportPriority.SplitEvenly) {
				splitStorages = new List<FluxStorage>(nets.Count);
				double capacity = (double)machine.PowerStorage.CurrentCapacity / nets.Count;

				for (int i = 0; i < nets.Count; i++)
					splitStorages.Add(new FluxStorage(machine.PowerStorage.MaxCapacity) { CurrentCapacity = new TerraFlux(capacity) });

				// Clear the original storage
				machine.PowerStorage.CurrentCapacity = TerraFlux.Zero;
			}

			int index = -1;
			foreach (var network in nets) {
				++index;

				if (!TryGetHighestTransferRate(machine, network, out TerraFlux export, out Point16 exportTile, out _))
					continue;

				var storage = mode == PowerExportPriority.SplitEvenly ? splitStorages[index] : machine.PowerStorage;

				storage.ExportTo(network.Storage, export);

				// Does the storage still have power?  If so, split it among the remaining ones
				if (mode == PowerExportPriority.SplitEvenly && !storage.IsEmpty && index < nets.Count - 1) {
					double capacity = (double)storage.CurrentCapacity / (nets.Count - 1 - index);

					for (int i = index; i < nets.Count; i++) {
						TerraFlux import = new(capacity);
						splitStorages[i].Import(ref import);
					}
				}

				Netcode.SyncNetworkPowerStorage(network, exportTile);
			}

			if (nets.Count > 0 && mode == PowerExportPriority.SplitEvenly) {
				// Import any leftovers back into the original storage
				machine.PowerStorage.ImportFrom(splitStorages[^1], machine.PowerStorage.MaxCapacity);
			}
		}

		private static IEnumerable<PowerNetwork> OrderByMode(IEnumerable<PowerNetwork> networks, PowerExportPriority mode) {
			return mode switch {
				PowerExportPriority.FirstComeFirstServe => networks,
				PowerExportPriority.LastComeFirstServe => networks.Reverse(),
				PowerExportPriority.LowestPower => networks.OrderBy(GetOrderPriority),
				PowerExportPriority.HighestPower => networks.OrderByDescending(GetOrderPriority),
				// Special logic is used in ExportPowerToAdjacentNetworks for this mode...
				PowerExportPriority.SplitEvenly => networks,
				_ => networks
			};
		}

		private static double GetOrderPriority(PowerNetwork network)
			=> network.Storage.MaxCapacity == TerraFlux.Zero ? double.PositiveInfinity : (double)network.Storage.CurrentCapacity / (double)network.Storage.MaxCapacity;

		/// <summary>
		/// Imports power from all adjacent <see cref="PowerNetwork"/> instances to the flux storage in <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		public static void ImportPowerFromAdjacentNetworks(IPoweredMachine machine) {
			foreach (var network in GetAdjacentPowerNetworks(machine)) {
				if (!TryGetHighestTransferRate(machine, network, out TerraFlux export, out Point16 exportTile, out _))
					continue;

				network.Storage.ExportTo(machine.PowerStorage, export);

				Netcode.SyncNetworkPowerStorage(network, exportTile);
			}
		}

		/// <summary>
		/// This method applies the upgrades in <paramref name="machine"/> to <see cref="PowerStorage"/>.MaxCapacity
		/// </summary>
		/// <param name="machine"></param>
		public static void Update(IPoweredMachine machine) {
			var storage = machine.PowerStorage;
			storage.MaxCapacity = new TerraFlux(CalculateFromUpgrades(machine, StatModifier.Default,
				static (u, s, v) => u.GetPowerCapacityMultiplier(s).CombineWith(v))
				.ApplyTo((double)storage.BaseMaxCapacity));

			// Prevent overflow when removing upgrades
			if (storage.CurrentCapacity > storage.MaxCapacity) {
				storage.CurrentCapacity = storage.MaxCapacity;
				Netcode.SyncMachinePowerStorage(machine);
			}
		}

		#pragma warning disable CS1591
		public static void SaveData(IPoweredMachine machine, TagCompound tag) {
			TagCompound flux;
			tag["flux"] = flux = new TagCompound();
			machine.PowerStorage.SaveData(flux);
		}

		public static void LoadData(IPoweredMachine machine, TagCompound tag) {
			if (tag.GetCompound("flux") is TagCompound flux)
				machine.PowerStorage.LoadData(flux);
		}

		public static void NetSend(IPoweredMachine machine, BinaryWriter writer) {
			machine.PowerStorage.Send(writer);
		}

		public static void NetReceive(IPoweredMachine machine, BinaryReader reader) {
			machine.PowerStorage.Receive(reader);
		}
	}
}
