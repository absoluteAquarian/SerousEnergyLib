using SerousEnergyLib.API;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Pathfinding.Objects;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.Systems.Networks {
	public sealed class ItemNetwork : NetworkInstance {
		private HashSet<Point16> adjacentInventoryTiles = new();
		private Dictionary<Point16, Ref<int>> pumpTimers = new();
		internal List<PipedItem> items = new();

		internal ItemNetwork() : base(NetworkType.Items) { }

		public override void Update() {
			HashSet<Point16> invalidPumps = new();
			foreach (var (loc, timer) in pumpTimers) {
				if (!UpdatePump(loc, timer))
					invalidPumps.Add(loc);
			}

			foreach (var pump in invalidPumps)
				pumpTimers.Remove(pump);

			for (int i = 0; i < items.Count; i++) {
				if (items[i].Destroyed)
					items[i] = null;

				items[i]?.Update();
			}
		}

		internal void AddPumpTimer(Point16 location, int timer) {
			pumpTimers[location] = new Ref<int>(timer);
		}

		private bool UpdatePump(Point16 location, Ref<int> timerRef) {
			ref int timer = ref timerRef.Value;

			timer--;

			if (timer <= 0) {
				// Reset the timer according to the max value for the pump type
				Tile tile = Main.tile[location.X, location.Y];

				if (TileLoader.GetTile(tile.TileType) is not IItemPumpTile pump)
					return false;

				timer = pump.GetMaxTimer(location.X, location.Y);

				// Check if there's an inventory behind this pump
				PumpDirection direction = tile.Get<NetworkTaggedInfo>().PumpDirection;

				Point16 tailOffset = direction switch {
					PumpDirection.Left => new Point16(1, 0),
					PumpDirection.Up => new Point16(0, 1),
					PumpDirection.Right => new Point16(-1, 0),
					PumpDirection.Down => new Point16(0, -1),
					_ => throw new Exception("Invalid pump direction detected"),
				};

				Point16 possibleInventory = location + tailOffset;
				int numExtract = pump.StackPerExtraction;

				List<InventoryExtractionResult> extractions = null;

				if (NetworkHandler.locationToChest.TryGetValue(possibleInventory, out int chestNum)) {
					// Chest should be valid here...
					extractions = Main.chest[chestNum].ExtractItems(chestNum, this, ref numExtract, simulation: false);

					goto makeItems;
				}

				if (IMachine.TryFindMachine(possibleInventory, out IMachine machine) && machine is IInventoryMachine inventory)
					extractions = inventory.ExtractItems(this, ref numExtract, simulation: false);

				// Inform clients of what the current state of the pump is
				makeItems:

				if (extractions is not null) {
					foreach (var result in extractions) {
						var path = AttemptToGeneratePathToInventoryTarget(location, result.target);

						if (path is null)
							continue;

						PipedItem item = new PipedItem(this, possibleInventory, location, result.target, path, result.item);

						AddPipedItem(item);

						Netcode.SyncPipedItem(item, fullSync: true);
					}
				}

				Netcode.SyncPumpTimer(this, location, timer);
			}

			return true;
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

		public void AddAdjacentInventory(Point16 inventory) {
			if (IMachine.TryFindMachine(inventory, out IMachine machine) && machine is IInventoryMachine)
				adjacentInventoryTiles.Add(inventory);
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

			HashSet<Point16> noLongerExist = new();

			foreach (var adjacent in adjacentInventoryTiles) {
				bool exists = false;

				if (NetworkHandler.locationToChest.TryGetValue(adjacent, out int chest)) {
					// Tile was a chest
					if (Main.chest[chest].CanImportItem(import, out stackImported) && stackImported > 0) {
						inventory = adjacent;
						return true;
					}

					exists = true;
					goto doesItStillExist;
				}

				Tile tile = Main.tile[adjacent.X, adjacent.Y];

				if (TileLoader.GetTile(tile.TileType) is IInventoryMachine machine) {
					if (machine.CanImportItem(import, out stackImported) && stackImported > 0) {
						inventory = adjacent;
						return true;
					}

					exists = true;
				}

				doesItStillExist:
				if (!exists)
					noLongerExist.Add(adjacent);
			}

			foreach (var loc in noLongerExist)
				adjacentInventoryTiles.Remove(loc);

			inventory = Point16.NegativeOne;
			stackImported = 0;
			return false;
		}

		public List<Point16> AttemptToGeneratePathToInventoryTarget(Point16 current, Point16 inventory) {
			// Generate a path to the target
			var left = GeneratePath(current, inventory + new Point16(-1, 0), out double leftTime);
			var up = GeneratePath(current, inventory + new Point16(0, -1), out double upTime);
			var right = GeneratePath(current, inventory + new Point16(1, 0), out double rightTime);
			var down = GeneratePath(current, inventory + new Point16(0, 1), out double downTime);
				
			if (left is null && up is null && right is null && down is null) {
				// No path found
				return null;
			}

			if (left is null)
				leftTime = double.PositiveInfinity;
			if (up is null)
				upTime = double.PositiveInfinity;
			if (right is null)
				rightTime = double.PositiveInfinity;
			if (down is null)
				downTime = double.PositiveInfinity;

			if (left is not null && leftTime <= upTime && leftTime <= rightTime && leftTime <= downTime) {
				// Use the left path
				return left;
			} else if (up is not null && upTime <= rightTime && upTime <= downTime) {
				// Use the up path
				return up;
			} else if (right is not null && rightTime <= downTime) {
				// Use the right path
				return right;
			} else if (down is not null) {
				// Use the down path
				return down;
			}

			return null;
		}
	}
}
