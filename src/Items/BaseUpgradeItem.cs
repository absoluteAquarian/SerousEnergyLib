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

		/// <summary>
		/// Shorthand for Item.stack
		/// </summary>
		public int Stack {
			get => Item.stack;
			set => Item.stack = value;
		}

		#pragma warning disable CS1591
		public override void SetDefaults() {
			Item.maxStack = 99;
		}
	}
}
