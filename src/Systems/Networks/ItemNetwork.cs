using SerousEnergyLib.API;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Pathfinding.Objects;
using SerousEnergyLib.TileData;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.Systems.Networks {
	public sealed class ItemNetwork : NetworkInstance {
		private HashSet<Point16> adjacentInventoryTiles = new();
		internal List<PipedItem> items = new();

		internal ItemNetwork() : base(NetworkType.Items) { }

		public override void Update() {
			for (int i = 0; i < items.Count; i++) {
				if (items[i].Destroyed)
					items[i] = null;

				if (items[i] is not null)
					items[i].Update();
			}
		}

		public int AddPipedItem(PipedItem item) {
			if (item is null || item.Destroyed)
				return -1;

			for (int i = 0; i < items.Count; i++) {
				PipedItem p = items[i];

				if (p is null || p.Destroyed) {
					items[i] = item;
					return i;
				}
			}

			items.Add(item);
			return items.Count - 1;
		}

		public override void OnNetworkReset() {
			adjacentInventoryTiles.Clear();
		}

		protected override void OnNetworkCloned() {
			// Remove any items no longer in the network
			for (int i = 0; i < items.Count; i++) {
				PipedItem item = items[i];

				if (!HasEntry(item.CurrentTile)) {
					item.Destroy(dropItem: false);
					items[i] = null;
				}
			}
		}

		protected override void SaveExtraData(TagCompound tag) {
			tag["inventories"] = adjacentInventoryTiles.ToList();
		}

		protected override void LoadExtraData(TagCompound tag) {
			adjacentInventoryTiles.Clear();

			if (tag.GetList<Point16>("inventories") is List<Point16> list) {
				foreach (var point in list)
					adjacentInventoryTiles.Add(point);
			}
		}

		protected override void DisposeSelf(bool disposing) {
			if (disposing)
				adjacentInventoryTiles.Clear();

			adjacentInventoryTiles = null;
		}

		/// <summary>
		/// Attempts to find a valid inventory that can have <paramref name="import"/> inserted into it.
		/// </summary>
		/// <param name="import">The item to attempt to insert.</param>
		/// <param name="inventory">The location of the inventory's tile if one was found</param>
		/// <returns>Whether a valid inventory was found</returns>
		public bool FindValidImportTarget(Item import, out Point16 inventory, out int stackImported) {
			stackImported = 0;

			foreach (var adjacent in adjacentInventoryTiles) {
				int chest = Chest.FindChestByGuessing(adjacent.X, adjacent.Y);

				if (chest > -1) {
					// Tile was a chest
					if (Main.chest[chest].CanImportItem(import, out stackImported)) {
						inventory = adjacent;
						return true;
					}

					continue;
				}

				Tile tile = Main.tile[adjacent.X, adjacent.Y];

				if (TileLoader.GetTile(tile.TileType) is IInventoryMachine machine) {
					if (machine.CanImportItem(import, out stackImported)) {
						inventory = adjacent;
						return true;
					}
				}
			}

			inventory = Point16.NegativeOne;
			return false;
		}
	}
}
