using SerousEnergyLib.API.Energy;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.Systems;
using SerousEnergyLib.Tiles;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.DataStructures;
using System.Collections.Generic;
using SerousEnergyLib.TileData;
using System.Linq;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface containing methods used by machines that can store and/or use power
	/// </summary>
	public interface IPoweredMachine : IMachine {
		FluxStorage PowerStorage { get; }

		/// <summary>
		/// The <see cref="EnergyTypeID"/> that this machine uses
		/// </summary>
		int EnergyID { get; }

		double GetPowerConsumption(double ticks);

		public double GetPowerConsumptionWithUpgrades(double ticks) => GetPowerConsumption(ticks) * CalculateFromUpgrades(1d, static (u, v) => u.GetPowerConsumptionMultiplier() * v);

		public void ImportPower(FluxStorage source) {
			double import = GetPowerConsumptionWithUpgrades(1);

			PowerStorage.ImportFrom(source, EnergyConversions.ConvertToTerraFlux(import, EnergyID));
		}

		/// <summary>
		/// Attempts to consume power from this machine's flux storage
		/// </summary>
		/// <param name="power">A quantity of power or the default consumption rate per tick (see: <see cref="GetPowerConsumptionWithUpgrades(double)"/>) if <see langword="null"/>.</param>
		/// <returns>Whether there was enough power for consumption</returns>
		public bool AttemptToConsumePower(double? power = null) {
			double consume = power ?? GetPowerConsumptionWithUpgrades(1);
			TerraFlux flux = EnergyConversions.ConvertToTerraFlux(consume, EnergyID);

			if (flux <= PowerStorage.CurrentCapacity) {
				PowerStorage.Export(ref flux);
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

		public void RemoveFromNearbyFluxNetworks() {
			foreach (var result in GetAdjacentNetworks(NetworkType.Power))
				(result.network as PowerNetwork)?.RemoveAdjacentFluxStorage(result.machineTileAdjacentToNetwork);
		}

		public IEnumerable<PowerNetwork> GetAdjacentPowerNetworks() {
			return GetAdjacentNetworks(NetworkType.Power)
				.Select(r => r.network as PowerNetwork)
				.OfType<PowerNetwork>();
		}

		public static void Update(IPoweredMachine machine) {
			machine.PowerStorage.MaxCapacity = machine.PowerStorage.BaseMaxCapacity * machine.CalculateFromUpgrades(1d, static (u, v) => u.GetPowerCapacityMultiplier() * v);

			// Prevent overflow when removing upgrades
			if (machine.PowerStorage.CurrentCapacity > machine.PowerStorage.MaxCapacity)
				machine.PowerStorage.CurrentCapacity = machine.PowerStorage.MaxCapacity;
		}

		public void SavePower(TagCompound tag) {
			TagCompound flux;
			tag["flux"] = flux = new TagCompound();
			PowerStorage.SaveData(flux);
		}

		public void LoadPower(TagCompound tag) {
			if (tag.GetCompound("flux") is TagCompound flux)
				PowerStorage.LoadData(flux);
		}
	}
}
