using System;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

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

		public static TagCompound SaveUpgrade(BaseUpgrade upgrade) {
			TagCompound tag = new TagCompound();

			if (upgrade is UnloadedUpgrade unloaded) {
				tag["mod"] = unloaded.unloadedMod;
				tag["name"] = unloaded.unloadedName;
			} else {
				tag["mod"] = upgrade.Mod.Name;
				tag["name"] = upgrade.Name;
			}

			return tag;
		}

		public static BaseUpgrade LoadUpgrade(TagCompound tag) {
			string mod = tag.GetString("mod");
			string name = tag.GetString("name");

			// If anything is invalid / can't be "loaded" as an UnloadedUpgrade, trash it
			if (string.IsNullOrWhiteSpace(mod) || string.IsNullOrWhiteSpace(name))
				return null;

			if (!ModLoader.TryGetMod(mod, out Mod source) || !source.TryFind(name, out BaseUpgrade upgrade)) {
				// Upgrade no longer exists
				return new UnloadedUpgrade(mod, name);
			}

			return upgrade;
		}
	}
}
