using SerousEnergyLib.API.Energy;
using SerousEnergyLib.Systems;
using SerousEnergyLib.Systems.Networks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface containing methods used by machines that can generate power
	/// </summary>
	public interface IPowerGeneratorMachine : IPoweredMachine {
		// Generators should not consume power by default...
		double IPoweredMachine.GetPowerConsumption(double ticks) => 0;

		protected double GetPowerGeneration(double ticks);

		public double GetPowerGenerationWithUpgrades(double ticks) => GetPowerGeneration(ticks) * CalculateFromUpgrades(1d, static (u, v) => u.GetPowerGenerationMultiplier() * v);

		protected double GetPowerExportRate(double ticks);

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

		public void ExportPowerToAdjacentNetworks() {
			List<PowerNetwork> adjacent = GetAdjacentPowerNetworks().ToList();

			if (adjacent.Count == 0)
				return;  // No networks to export to

			double export = GetPowerExportRate(1);
			TerraFlux slicedExport = EnergyConversions.ConvertToTerraFlux(Math.Min(export, (double)PowerStorage.CurrentCapacity) / adjacent.Count, EnergyID);

			foreach (var network in adjacent)
				PowerStorage.ExportTo(network.Storage, slicedExport);
		}
	}
}
