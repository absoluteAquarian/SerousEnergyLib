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

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface containing methods used by machines that can store and/or use power
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
		/// Applies <see cref="BaseUpgrade.GetPowerConsumptionMultiplier"/> to the result of <see cref="GetPowerConsumption(double)"/>
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
				.Select(r => r.network as PowerNetwork)
				.OfType<PowerNetwork>();
		}

		/// <summary>
		/// Attempts to find an adjacent transport tile in <see cref="PowerNetwork"/> with the highest transfer rate
		/// </summary>
		/// <param name="machine">The machine to try to export from or insert into</param>
		/// <param name="source">The network to examine</param>
		/// <param name="transfer">The highest transfer rate, or <see cref="TerraFlux.Zero"/> if one could not be found.</param>
		public static bool TryGetHighestTransferRate(IPoweredMachine machine, PowerNetwork source, out TerraFlux transfer) {
			// Find the tile adjacent to this machine with the highest export rate and use it
			var adjacent = GetAdjacentNetworks(machine, NetworkType.Power, allowDuplicates: true);

			TerraFlux maxTransfer = TerraFlux.Zero;
			bool entryExists = false;

			foreach (var result in adjacent) {
				if (result.network.ID != source.ID)
					continue;

				Point16 loc = result.tileInNetwork;

				if (TileLoader.GetTile(Main.tile[loc.X, loc.Y].TileType) is IPowerTransportTile transport && transport.TransferRate > maxTransfer) {
					maxTransfer = transport.TransferRate;
					entryExists = true;
				}
			}

			transfer = maxTransfer;
			return entryExists;
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
			if (storage.CurrentCapacity > storage.MaxCapacity)
				storage.CurrentCapacity = storage.MaxCapacity;
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
