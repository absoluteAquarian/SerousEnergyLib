using SerousEnergyLib.API.Fluid;
using SerousEnergyLib.API.Upgrades;
using System.Linq;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface representing a machine that can create fluid outputs<br/>
	/// This interface is used with <see cref="BaseUpgrade"/>
	/// </summary>
	public interface IFluidOutputGeneratorMachine : IFluidMachine, IMachine {
		/// <summary>
		/// Applies <see cref="BaseUpgrade.GetFluidOutputGeneratorProductMultiplier(int, int)"/> to <paramref name="originalProduct"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="fluidType">The <see cref="FluidTypeID"/> being generated</param>
		/// <param name="originalProduct">The original product quantity in Liters</param>
		/// <param name="slot">Which fluid storage slot in <see cref="IFluidMachine.FluidStorage"/> the output will end up in</param>
		/// <returns>The modified product quantity</returns>
		public static double CalculateFluidProduct(IFluidOutputGeneratorMachine machine, int fluidType, double originalProduct, int slot) {
			// Local capturing
			int type = fluidType;

			return CalculateFromUpgrades(machine, StatModifier.Default,
				machine.Upgrades.Where(u => machine.CanUpgradeApplyTo(u.Upgrade, slot)),
				(u, s, v) => u.GetFluidOutputGeneratorProductMultiplier(s, type).CombineWith(v))
				.ApplyTo(originalProduct);
		}

		/// <summary>
		/// Attempts to insert generated fluids into <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machone to process</param>
		/// <param name="fluidType">The <see cref="FluidTypeID"/> being generated</param>
		/// <param name="amount">The original product quantity in Liters</param>
		public static void AttemptToGenerateFluid(IFluidOutputGeneratorMachine machine, int fluidType, double amount) {
			int slot = machine.SelectFluidImportDestinationFromType(fluidType);

			if (slot < 0 || slot >= machine.FluidStorage.Length)
				return;

			// Adjust the amount generated
			amount = CalculateFluidProduct(machine, fluidType, amount, slot);

			if (amount <= 0)
				return;  // Nothing to generate

			machine.FluidStorage[slot].Import(fluidType, ref amount);
		}
	}
}
