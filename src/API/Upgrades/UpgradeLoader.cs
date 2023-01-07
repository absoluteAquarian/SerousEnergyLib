using SerousEnergyLib.Items;
using System.Collections.Generic;
using Terraria;
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

		public static TagCompound SaveUpgrade(BaseUpgradeItem item) {
			return new TagCompound() {
				["mod"] = item.Mod.Name,
				["name"] = item.Name,
				["stack"] = item.Stack
			};
		}

		public static BaseUpgradeItem LoadUpgrade(TagCompound tag) {
			string mod = tag.GetString("mod");
			string name = tag.GetString("name");

			if (string.IsNullOrWhiteSpace(mod) || string.IsNullOrWhiteSpace(name))
				return null;

			if (!ModLoader.TryGetMod(mod, out Mod source) || !source.TryFind(name, out BaseUpgradeItem item))
				item = new Item(ModContent.ItemType<UnloadedUpgradeItem>()).ModItem as BaseUpgradeItem;

			int stack = tag.GetInt("stack");

			if (stack <= 0)
				return null;

			item.Stack = stack;

			return item;
		}
	}
}
