using System.Collections.Generic;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Upgrades {
	/// <summary>
	/// The centeral class for loading <see cref="BaseUpgrade"/> instnaces
	/// </summary>
	public static class UpgradeLoader {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				upgrades.Clear();
			}
		}

		#pragma warning disable CS1591
		private static readonly List<BaseUpgrade> upgrades = new();

		public static int Count => upgrades.Count;

		internal static int Register(BaseUpgrade upgrade) {
			upgrades.Add(upgrade);
			return Count - 1;
		}

		public static BaseUpgrade Get(int index) => index < 0 || index >= Count ? null : upgrades[index];
	}
}
