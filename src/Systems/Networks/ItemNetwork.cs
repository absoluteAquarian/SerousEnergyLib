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

				items[i]?.Update();
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

		// IInventoryMachine has a paired method for removing the machine from item networks
		public override void OnEntryAdded(Point16 location) {
			Point16 left = location + new Point16(-1, 0),
				up = location + new Point16(0, -1),
				right = location + new Point16(1, 0),
				down = location + new Point16(0, 1);
			
			// Add adjacent chests
			if (TryFindChest(left, up, right, down) > -1)
				adjacentInventoryTiles.Add(left);

			// Add adjacent machines
			if (IMachine.TryFindMachine(left, out IMachine machine) && machine is IInventoryMachine)
				adjacentInventoryTiles.Add(left);
			if (IMachine.TryFindMachine(up, out machine) && machine is IInventoryMachine)
				adjacentInventoryTiles.Add(up);
			if (IMachine.TryFindMachine(right, out machine) && machine is IInventoryMachine)
				adjacentInventoryTiles.Add(right);
			if (IMachine.TryFindMachine(down, out machine) && machine is IInventoryMachine)
				adjacentInventoryTiles.Add(down);
		}

		// Manual logic to improve OnEntryAdded performance
		private static int TryFindChest(Point16 left, Point16 up, Point16 right, Point16 down) {
			for (int i = 0; i < 8000; i++) {
				Chest chest = Main.chest[i];

				if (chest is null)
					continue;

				if (chest.x >= left.X && chest.x < left.X + 2 && chest.y >= left.Y && chest.y < left.Y + 2)
					return i;

				if (chest.x >= up.X && chest.x < up.X + 2 && chest.y >= up.Y && chest.y < up.Y + 2)
					return i;

				if (chest.x >= right.X && chest.x < right.X + 2 && chest.y >= right.Y && chest.y < right.Y + 2)
					return i;

				if (chest.x >= down.X && chest.x < down.X + 2 && chest.y >= down.Y && chest.y < down.Y + 2)
					return i;
			}

			return -1;
		}

		public void RemoveAdjacentInventory(Point16 inventory) {
			if (adjacentInventoryTiles.Remove(inventory)) {
				// Update any items whose target was this location
				foreach (var item in items) {
					if (item.Target == inventory)
						item.OnTargetLost();
				}
			}
		}

		protected override void SaveExtraData(TagCompound tag) {
			static TagCompound SaveItem(PipedItem item) {
				TagCompound itemTag = new TagCompound();
				item.SaveData(itemTag);
				return itemTag;
			}

			tag["items"] = items.Select(SaveItem).ToList();
		}

		protected override void LoadExtraData(TagCompound tag) {
			items.Clear();

			if (tag.GetList<TagCompound>("items") is List<TagCompound> itemTags) {
				foreach (var item in itemTags)
					items.Add(PipedItem.LoadData(this, item));
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
