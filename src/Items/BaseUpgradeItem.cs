using SerousEnergyLib.API.Upgrades;
using Terraria.ModLoader;

namespace SerousEnergyLib.Items {
	/// <summary>
	/// The base implementation for an item containin a <see cref="BaseUpgrade"/> that can be placed in a machine
	/// </summary>
	public abstract class BaseUpgradeItem : ModItem {
		/// <summary>
		/// The upgrade instance.  This property should be treated as a singleton
		/// </summary>
		public abstract BaseUpgrade Upgrade { get; }

		#pragma warning disable CS1591
		public override void SetDefaults() {
			Item.maxStack = 99;
		}
	}
}
