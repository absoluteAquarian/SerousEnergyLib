using System;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Upgrades {
	/// <summary>
	/// An object representing a stack of <see cref="BaseUpgrade"/> instances in a machine
	/// </summary>
	public sealed class StackedUpgrade {
		/// <summary>
		/// How many upgrades are in this instance
		/// </summary>
		public int Stack { get; internal set; }

		/// <summary>
		/// The <see cref="BaseUpgrade"/> instance
		/// </summary>
		public BaseUpgrade Upgrade { get; internal set; }

		#pragma warning disable CS1591
		public TagCompound Save() {
			TagCompound tag = new() {
				["stack"] = Stack,
				["upgrade"] = UpgradeLoader.SaveUpgrade(Upgrade)
			};
			return tag;
		}

		public static StackedUpgrade Load(TagCompound tag) {
			return new StackedUpgrade() {
				Stack = tag.GetInt("stack"),
				Upgrade = UpgradeLoader.LoadUpgrade(tag.GetCompound("upgrade"))
			};
		}
	}
}
