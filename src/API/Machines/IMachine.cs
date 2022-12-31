using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.API.Upgrades;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface containing methods used by all machines
	/// </summary>
	public interface IMachine {
		/// <summary>
		/// The ID of the tile that this machine should be bound to.
		/// </summary>
		int MachineTile { get; }

		/// <summary>
		/// The UI instance bound to this machine instance.
		/// </summary>
		BaseMachineUI MachineUI { get; }

		public bool IsTileValid(int x, int y) {
			Tile tile = Main.tile[x, y];
			return tile.HasTile && tile.TileType == MachineTile && tile.TileFrameX == 0 && tile.TileFrameY == 0;
		}

		/// <summary>
		/// The collection of upgrades contained within this machine
		/// </summary>
		List<BaseUpgrade> Upgrades { get; set; }

		public static void Update(IMachine machine) {
			// Ensure that the upgrades collection isn't null
			machine.Upgrades ??= new();
		}

		protected T CalculateFromUpgrades<T>(T @base, Func<BaseUpgrade, T, T> mutator) {
			T calculated = @base;

			foreach (var upgrade in Upgrades) {
				// Invalid upgrades shouldn't be in the collection in the first place, but it's a good idea to double check here
				if (upgrade.CanApplyTo(this))
					calculated = mutator(upgrade, calculated);
			}

			return calculated;
		}

		public void SaveUpgrades(TagCompound tag) {
			static TagCompound GenerateTag(BaseUpgrade upgrade) {
				TagCompound data;
				TagCompound tag = new TagCompound() {
					["mod"] = upgrade.Mod.Name,
					["name"] = upgrade.Name,
					["data"] = data = new TagCompound()
				};

				if (upgrade is not UnloadedUpgrade)
					upgrade.SaveData(data);
				else
					upgrade.SaveData(tag);

				return tag;
			}

			tag["upgrades"] = Upgrades.Select(GenerateTag).ToList();
		}

		public void LoadUpgrades(TagCompound tag) {
			static BaseUpgrade LoadUpgrade(TagCompound tag) {
				string mod = tag.GetString("mod");
				string name = tag.GetString("name");

				// If anything is invalid / can't be "loaded" as an UnloadedUpgrade, trash it
				if (string.IsNullOrWhiteSpace(mod) || string.IsNullOrWhiteSpace(name))
					return null;

				TagCompound data = tag.GetCompound("data") ?? new TagCompound();

				if (!ModLoader.TryGetMod(mod, out Mod source) || !source.TryFind(name, out BaseUpgrade upgrade)) {
					// Upgrade no longer exists
					return new UnloadedUpgrade(mod, name, data);
				}

				upgrade.LoadData(data);

				return upgrade;
			}

			Upgrades = tag.GetList<TagCompound>("upgrades") is List<TagCompound> list
				? list.Select(LoadUpgrade).OfType<BaseUpgrade>().ToList()
				: new();
		}
	}
}
