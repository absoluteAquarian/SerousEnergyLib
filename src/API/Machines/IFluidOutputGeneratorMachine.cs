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
		/// Modifies <paramref name="originalProduct"/> based on the upgrades contained in this machine
		/// </summary>
		/// <param name="originalProduct">The original product quantity in Liters</param>
		/// <param name="slot">Which fluid storage slot in <see cref="IFluidMachine.FluidStorage"/> the output will end up in</param>
		/// <returns>The modified product quantity</returns>
		public double CalculateFluidProduct(double originalProduct, int slot)
			=> CalculateFromUpgrades(StatModifier.Default,
				Upgrades.Where(u => CanUpgradeApplyTo(u.Upgrade, slot)),
				static (u, s, v) => u.GetFluidOutputGeneratorProductMultiplier(s).CombineWith(v))
				.ApplyTo(originalProduct);
	}
}
