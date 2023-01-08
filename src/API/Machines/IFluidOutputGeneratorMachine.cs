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
		/// Applies <see cref="BaseUpgrade.GetFluidOutputGeneratorProductMultiplier(int)"/> to <paramref name="originalProduct"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="originalProduct">The original product quantity in Liters</param>
		/// <param name="slot">Which fluid storage slot in <see cref="IFluidMachine.FluidStorage"/> the output will end up in</param>
		/// <returns>The modified product quantity</returns>
		public static double CalculateFluidProduct(IFluidOutputGeneratorMachine machine, double originalProduct, int slot)
			=> CalculateFromUpgrades(machine, StatModifier.Default,
				machine.Upgrades.Where(u => machine.CanUpgradeApplyTo(u.Upgrade, slot)),
				static (u, s, v) => u.GetFluidOutputGeneratorProductMultiplier(s).CombineWith(v))
				.ApplyTo(originalProduct);
	}
}
