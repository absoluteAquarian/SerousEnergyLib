using SerousEnergyLib.API.Energy;
using SerousEnergyLib.API.Upgrades;
using SerousEnergyLib.Systems;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface containing methods used by machines that can generate and export power<br/>
	/// <b>NOTE:</b> This interface should NOT be used with <see cref="IPowerStorageMachine"/>
	/// </summary>
	public interface IPowerGeneratorMachine : IPoweredMachine {
		// Generators should not consume power by default...
		double IPoweredMachine.GetPowerConsumption(double ticks) => 0;

		/// <summary>
		/// The mode used when attempting to export power from this machine
		/// </summary>
		PowerExportPriority StorageExportMode { get; set; }

		/// <summary>
		/// Return how much power in units represented by <see cref="IPoweredMachine.EnergyID"/> should be generated for a duration of <paramref name="ticks"/>
		/// </summary>
		/// <param name="ticks">The amount of game ticks to calculate</param>
		double GetPowerGeneration(double ticks);

		/// <summary>
		/// Applies <see cref="BaseUpgrade.GetPowerGenerationMultiplier(int)"/> to the result of <see cref="GetPowerGeneration(double)"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="ticks">The amount of game ticks to calculate</param>
		public static double GetPowerGenerationWithUpgrades(IPowerGeneratorMachine machine, double ticks)
			=> CalculateFromUpgrades(machine, StatModifier.Default, static (u, s, v) => u.GetPowerGenerationMultiplier(s).CombineWith(v))
				.ApplyTo(machine.GetPowerGeneration(ticks));

		/// <summary>
		/// Generates power and adds it to the flux storage in <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		public static void GeneratePower(IPowerGeneratorMachine machine) {
			if (!Network.UpdatingPowerGenerators)
				return;

			double generate = GetPowerGenerationWithUpgrades(machine, 1);
			TerraFlux flux = EnergyConversions.ConvertToTerraFlux(generate, machine.EnergyID);
			machine.PowerStorage.Import(ref flux);

			Netcode.SyncMachinePowerStorage(machine);

			ExportPowerToAdjacentNetworks(machine, machine.StorageExportMode);
		}
	}
}
