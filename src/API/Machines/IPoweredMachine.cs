using SerousEnergyLib.API.Energy;
using Terraria.ModLoader.IO;

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

		public void ImportPower(ref FluxStorage source) {
			double import = GetPowerConsumptionWithUpgrades(1);

			source.ImportFrom(PowerStorage, EnergyConversions.ConvertToTerraFlux(import, EnergyID));
		}

		/// <summary>
		/// Whether the wire at position (<paramref name="wireX"/>, <paramref name="wireY"/>) can merge with this machine's sub-tile at position (<paramref name="machineX"/>, <paramref name="machineY"/>)
		/// </summary>
		/// <param name="wireX">The tile X-coordinate for the wire</param>
		/// <param name="wireY">The tile Y-coordinate for the wire</param>
		/// <param name="machineX">The tile X-coordinate for the machine sub-tile</param>
		/// <param name="machineY">The tile Y-coordinate for the machine sub-tile</param>
		bool CanMergeWithWire(int wireX, int wireY, int machineX, int machineY);

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
			PowerStorage.LoadData(tag.GetCompound("flux"));
		}
	}
}
