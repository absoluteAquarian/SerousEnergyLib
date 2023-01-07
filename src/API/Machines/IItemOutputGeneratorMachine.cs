using SerousEnergyLib.API.Upgrades;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface representing a machine that can create item outputs<br/>
	/// This interface is used with <see cref="BaseUpgrade"/>
	/// </summary>
	public interface IItemOutputGeneratorMachine : IInventoryMachine, IMachine {
		/// <summary>
		/// Modifies <paramref name="originalStack"/> based on the upgrades contained in this machine
		/// </summary>
		/// <param name="originalStack">The original stack</param>
		/// <returns>The modified item stack</returns>
		public int CalculateItemStack(int originalStack) => CalculateFromUpgrades(originalStack, static (u, s, v) => v + u.GetItemOutputGeneratorExtraStack(s, v));
	}
}
