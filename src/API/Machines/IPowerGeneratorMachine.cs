using SerousEnergyLib.API.Energy;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface containing methods used by machines that can generate power
	/// </summary>
	public interface IPowerGeneratorMachine : IPoweredMachine {
		// Generators should not consume power by default...
		double IPoweredMachine.GetPowerConsumption(double ticks) => 0;

		protected double GetPowerGeneration(double ticks);

		public double GetPowerGenerationWithUpgrades(double ticks) => GetPowerGeneration(ticks) * CalculateFromUpgrades(1d, static (u, v) => u.GetPowerGenerationMultiplier() * v);

		public void ExportPower(ref FluxStorage destination) {
			double export = GetPowerGenerationWithUpgrades(1);

			PowerStorage.ExportTo(destination, EnergyConversions.ConvertToTerraFlux(export, EnergyID));
		}
	}
}
