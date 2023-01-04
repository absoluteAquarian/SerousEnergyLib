using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.API.Upgrades;
using SerousEnergyLib.Systems;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// The base interface used by all machine tile entities
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

		public static bool TryFindMachine(Point16 location, out IMachine machine) {
			Point16 topleft = TileFunctions.GetTopLeftTileInMultitile(location.X, location.Y);

			if (TileEntity.ByPosition.TryGetValue(topleft, out TileEntity entity) && entity is IMachine m) {
				machine = m;
				return true;
			}

			machine = null;
			return false;
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

		public readonly struct NetworkSearchResult {
			public readonly NetworkInstance network;
			public readonly Point16 machineTileAdjacentToNetwork;

			internal NetworkSearchResult(NetworkInstance instance, Point16 adjacentLocation) {
				network = instance;
				machineTileAdjacentToNetwork = adjacentLocation;
			}
		}

		public IEnumerable<NetworkSearchResult> GetAdjacentNetworks(NetworkType type) {
			if (this is not ModTileEntity entity)
				yield break;

			if (TileLoader.GetTile(MachineTile) is not IMachineTile machineTile)
				yield break;

			// Check all adjacent tiles in the cardinal directions
			machineTile.GetMachineDimensions(out uint width, out uint height);

			int x = -1;
			int y;

			// Check left edge
			for (y = 0; y < height; y++) {
				if (Network.GetNetworkAt(entity.Position.X + x, entity.Position.Y + y, type, out _) is NetworkInstance net)
					yield return new NetworkSearchResult(net, new Point16(entity.Position.X, entity.Position.Y + y));
			}

			// Check top edge
			y = -1;
			for (x = 0; x < width; x++) {
				if (Network.GetNetworkAt(entity.Position.X + x, entity.Position.Y + y, type, out _) is NetworkInstance net)
					yield return new NetworkSearchResult(net, new Point16(entity.Position.X + x, entity.Position.Y));
			}

			// Check right edge
			x = (int)width;
			for (y = 0; y < height; y++) {
				if (Network.GetNetworkAt(entity.Position.X + x, entity.Position.Y + y, type, out _) is NetworkInstance net)
					yield return new NetworkSearchResult(net, new Point16(entity.Position.X, entity.Position.Y + y));
			}

			// Check bottom edge
			y = (int)height;
			for (x = 0; x < width; x++) {
				if (Network.GetNetworkAt(entity.Position.X + x, entity.Position.Y + y, type, out _) is NetworkInstance net)
					yield return new NetworkSearchResult(net, new Point16(entity.Position.X + x, entity.Position.Y));
			}
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
