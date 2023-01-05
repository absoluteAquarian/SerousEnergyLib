using SerousEnergyLib.API.Energy;
using SerousEnergyLib.API.Upgrades;
using SerousEnergyLib.Systems;
using SerousEnergyLib.Systems.Networks;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface containing methods used by machines that can generate power
	/// </summary>
	public interface IPowerGeneratorMachine : IPoweredMachine {
		// Generators should not consume power by default...
		double IPoweredMachine.GetPowerConsumption(double ticks) => 0;

		/// <summary>
		/// Return how much power in units represented by <see cref="IPoweredMachine.EnergyID"/> should be generated for a duration of <paramref name="ticks"/>
		/// </summary>
		/// <param name="ticks">The amount of game ticks to calculate</param>
		double GetPowerGeneration(double ticks);

		/// <summary>
		/// Applies <see cref="BaseUpgrade.GetPowerGenerationMultiplier"/> to the result of <see cref="GetPowerGeneration(double)"/>
		/// </summary>
		/// <param name="ticks"></param>
		/// <returns></returns>
		public double GetPowerGenerationWithUpgrades(double ticks)
			=> CalculateFromUpgrades(StatModifier.Default, static (u, v) => u.GetPowerGenerationMultiplier().CombineWith(v))
				.ApplyTo(GetPowerGeneration(ticks));

		/// <summary>
		/// Generates power and adds it to this machine's flux storage
		/// </summary>
		public void GeneratePower() {
			if (!Network.UpdatingPowerGenerators)
				return;

			double generate = GetPowerGenerationWithUpgrades(1);
			TerraFlux flux = EnergyConversions.ConvertToTerraFlux(generate, EnergyID);
			PowerStorage.Import(ref flux);

			ExportPowerToAdjacentNetworks();
		}

		/// <summary>
		/// Exports power from this machine's storage to all adjacent <see cref="PowerNetwork"/> instances
		/// </summary>
		public void ExportPowerToAdjacentNetworks() {
			foreach (var network in GetAdjacentPowerNetworks()) {
				if (!TryGetHighestTransferRate(this, network, out TerraFlux export))
					continue;

				PowerStorage.ExportTo(network.Storage, export);
			}
		}
	}
}
