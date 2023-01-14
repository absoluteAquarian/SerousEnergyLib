using SerousEnergyLib.API;
using SerousEnergyLib.API.Helpers;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Pathfinding.Objects;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.Systems.Networks {
	/// <summary>
	/// An object representing a <see cref="NetworkInstance"/> for item tiles
	/// </summary>
	public sealed class ItemNetwork : NetworkInstance {
		private HashSet<Point16> adjacentInventoryTiles = new();
		private Dictionary<Point16, Ref<int>> pumpTimers = new();
		internal List<PipedItem> items = new();

		internal int AdjacentInventoryCount => adjacentInventoryTiles.Count;
		internal int PumpCount => pumpTimers.Count;

		internal ItemNetwork() : base(NetworkType.Items) { }

		#pragma warning disable CS1591
		public override void Update() {
			HashSet<Point16> invalidPumps = new();
			foreach (var (loc, timer) in pumpTimers) {
				if (!UpdatePump(loc, timer))
					invalidPumps.Add(loc);
			}

			foreach (var pump in invalidPumps)
				pumpTimers.Remove(pump);

			for (int i = 0; i < items.Count; i++) {
				if (items[i] is { Destroyed: true })
					items[i] = null;

				items[i]?.Update();
			}
		}

		internal void AddPumpTimer(Point16 location, int timer) {
			pumpTimers[location] = new Ref<int>(timer);
		}

		public bool TryGetPumpTimer(Point16 location, out int timer) {
			if (pumpTimers.TryGetValue(location, out Ref<int> timerRef)) {
				timer = timerRef.Value;
				return true;
			}

			timer = -1;
			return false;
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
					extractions = Main.chest[chestNum].ExtractItems(chestNum, this, possibleInventory, ref numExtract, simulation: false);

					goto makeItems;
				}

				if (IMachine.TryFindMachine(possibleInventory, out IInventoryMachine machine))
					extractions = IInventoryMachine.ExtractItems(machine, this, possibleInventory, ref numExtract, simulation: false);

				// Inform clients of what the current state of the pump is
				makeItems:

				if (extractions is { Count: > 0 }) {
					foreach (var result in extractions) {
						var path = AttemptToGeneratePathToInventoryTarget(location, result.target);

						if (path is null) {
							// Failsafe: force the item back into its inventory
							result.UndoExtraction();
							continue;
						}

						PipedItem item = new PipedItem(this, possibleInventory, location, result.target, path, result.item);

						AddPipedItem(item);

						Netcode.SyncPipedItem(item, fullSync: true);
					}
				}

				Netcode.SyncPumpTimer(this, location, timer);
			}

			return true;
		}

		/// <summary>
		/// Attempts to find the first index of a destoyed or null <see cref="PipedItem"/> instance in this network's collection, then overwrites it with <paramref name="item"/>
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <returns>The index of <paramref name="item"/> in this network's collection</returns>
		public int AddPipedItem(PipedItem item) {
			if (item is not { Destroyed: false })
				return -1;

			for (int i = 0; i < items.Count; i++) {
				PipedItem p = items[i];

				if (p is not { Destroyed: false }) {
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

		protected override void CopyExtraData(NetworkInstance source) {
			ItemNetwork src = source as ItemNetwork;

			foreach (var item in src.items) {
				if (item is not { Destroyed: false })
					continue;

				TagCompound tag = new();
				item.SaveData(tag);

				PipedItem clone = PipedItem.LoadData(this, tag);

				items.Add(clone);
			}

			foreach (var (loc, pump) in src.pumpTimers)
				pumpTimers[loc] = new Ref<int>(pump.Value);

			foreach (var loc in src.adjacentInventoryTiles)
				adjacentInventoryTiles.Add(loc);
		}

		protected override void OnNetworkCloned(NetworkInstance orig) {
			// Remove any items no longer in the network
			for (int i = 0; i < items.Count; i++) {
				PipedItem item = items[i];

				if (item is not { Destroyed: false })
					continue;

				if (!HasEntry(item.CurrentTile)) {
					item.Destroy(dropItem: false);
					items[i] = null;
				}
			}
		}

		// IInventoryMachine has a paired method for removing the machine from item networks
		public override void OnEntryAdded(Point16 location) {
			// Add adjacent chests
			if (ChestFinder.FindChestAtCardinalTiles(location.X, location.Y, out Point16 chest) > -1)
				adjacentInventoryTiles.Add(chest);

			Point16 left = location + new Point16(-1, 0),
				up = location + new Point16(0, -1),
				right = location + new Point16(1, 0),
				down = location + new Point16(0, 1);

			// Add adjacent machines
			if (IMachine.TryFindMachine(left, out IInventoryMachine _))
				adjacentInventoryTiles.Add(left);
			if (IMachine.TryFindMachine(up, out IInventoryMachine _))
				adjacentInventoryTiles.Add(up);
			if (IMachine.TryFindMachine(right, out IInventoryMachine _))
				adjacentInventoryTiles.Add(right);
			if (IMachine.TryFindMachine(down, out IInventoryMachine _))
				adjacentInventoryTiles.Add(down);
		}

		/// <summary>
		/// Attempts to add an adjacent <see cref="IInventoryMachine"/> machine or <see cref="Chest"/> entry location at <paramref name="inventory"/>
		/// </summary>
		/// <param name="inventory">The tile location of the adjacent machine or chest to add</param>
		public void AddAdjacentInventory(Point16 inventory) {
			if (IMachine.TryFindMachine(inventory, out IInventoryMachine _))
				adjacentInventoryTiles.Add(inventory);
			if (ChestFinder.FindByGuessingImproved(inventory.X, inventory.Y) > -1)
				adjacentInventoryTiles.Add(inventory);
		}

		/// <summary>
		/// Removes an adjacent <see cref="IInventoryMachine"/> machine or <see cref="Chest"/> entry location
		/// </summary>
		/// <param name="inventory">The tile location of the adjacent machine or chest to remove</param>
		public void RemoveAdjacentInventory(Point16 inventory) {
			if (adjacentInventoryTiles.Remove(inventory)) {
				// Update any items whose target was this location
				foreach (var item in items) {
					if (item.Target == inventory)
						item.OnTargetLost();
				}
			}
		}

		/// <summary>
		/// Returns whether the tile at <paramref name="inventory"/> is considered an adjacent inventory to this network
		/// </summary>
		/// <param name="inventory">The tile location of the adjacent tile</param>
		public bool HasAdjacentInventory(Point16 inventory) => adjacentInventoryTiles.Contains(inventory);

		internal void AttemptToRetargetWanderingItems(Point16 inventory) {
			if (!adjacentInventoryTiles.Contains(inventory))
				return;

			foreach (var item in items) {
				if (item is not { Destroyed: false })
					continue;

				item.FindNewTargetIfWandering();
			}
		}

		protected override void DisposeSelf(bool disposing) {
			if (disposing) {
				adjacentInventoryTiles.Clear();
				items.Clear();
				pumpTimers.Clear();
			}

			adjacentInventoryTiles = null;
			items = null;
			pumpTimers = null;
		}

		public override void OnEntryRemoved(Point16 location) {
			// Drop all items that are at the removed location
			for (int i = 0; i < items.Count; i++) {
				PipedItem item = items[i];

				if (item is not { Destroyed: false })
					continue;

				if (item.CurrentTile == location) {
					item.Destroy(dropItem: HasEntry(location));

					Netcode.SyncPipedItem(item, fullSync: false);

					items[i] = null;
				}
			}

			// Remove any adjacent inventories
			Point16 left = location + new Point16(-1, 0),
				up = location = new Point16(0, -1),
				right = location + new Point16(1, 0),
				down = location + new Point16(0, 1);

			adjacentInventoryTiles.Remove(left);
			adjacentInventoryTiles.Remove(up);
			adjacentInventoryTiles.Remove(right);
			adjacentInventoryTiles.Remove(down);

			// Remove the pump timer if it's present
			pumpTimers.Remove(location);
		}

		/// <summary>
		/// The list of coordinates that <see cref="FindValidImportTarget(Item, out Point16, out int)"/> should ignore
		/// </summary>
		public readonly HashSet<Point16> ignoredValidTargets = new();

		/// <summary>
		/// Attempts to find a valid adjacent inventory that can have <paramref name="import"/> inserted into it.
		/// </summary>
		/// <param name="import">The item to attempt to insert.</param>
		/// <param name="inventory">The location of the inventory's tile if one was found</param>
		/// <param name="stackImported">The quantity of <paramref name="import"/> that can be inserted in the found target, or zero if no target was found</param>
		/// <returns>Whether a valid inventory was found</returns>
		public bool FindValidImportTarget(Item import, out Point16 inventory, out int stackImported) {
			stackImported = 0;

			HashSet<Point16> noLongerExist = new();

			foreach (var adjacent in adjacentInventoryTiles) {
				bool exists = false;

				if (ignoredValidTargets.Contains(adjacent))
					continue;

				// If the inventory is no longer adjacent, it needs to be removed from the collection
				if (!HasEntry(adjacent + new Point16(-1, 0)) && !HasEntry(adjacent + new Point16(0, -1)) && !HasEntry(adjacent + new Point16(1, 0)) && !HasEntry(adjacent + new Point16(0, 1)))
					goto doesItStillExist;

				if (NetworkHandler.locationToChest.TryGetValue(adjacent, out int chest)) {
					// Tile was a chest
					if (Main.chest[chest].CheckItemImportPrediction(this, import, out stackImported) && stackImported > 0) {
						inventory = adjacent;
						return true;
					}

					exists = true;
					goto doesItStillExist;
				}

				Tile tile = Main.tile[adjacent.X, adjacent.Y];

				if (TileLoader.GetTile(tile.TileType) is IMachineTile && IMachine.TryFindMachine(adjacent, out IInventoryMachine machine)) {
					if (IInventoryMachine.CheckItemImportPrediction(machine, this, import, out stackImported) && stackImported > 0) {
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

		/// <summary>
		/// This field is used to initialize the pathfinding for <see cref="AttemptToGeneratePathToInventoryTarget(Point16, Point16)"/>
		/// </summary>
		internal Point16 pipedItemDirection = Point16.NegativeOne;

		/// <summary>
		/// Attempts to generate a path from <paramref name="current"/> to <paramref name="inventory"/>
		/// </summary>
		/// <param name="current">The tile coordinate to start the pathfinding at</param>
		/// <param name="inventory">The tile coordinate to end the pathfinding at</param>
		/// <returns>A list of tile coordinates for pathfinding, or <see langword="null"/> if no path was found</returns>
		public List<Point16> AttemptToGeneratePathToInventoryTarget(Point16 current, Point16 inventory) {
			// Generate a path to the target
			// Pumps cannot be the final tile in the path
			Point16 leftPos = inventory + new Point16(-1, 0);
			double leftTime = 0;
			base.PathfindingStartDirection = pipedItemDirection;
			var left = !HasPump(leftPos, out _) ? GeneratePath(current, leftPos, out leftTime) : null;

			Point16 upPos = inventory + new Point16(0, -1);
			double upTime = 0;
			base.PathfindingStartDirection = pipedItemDirection;
			var up = !HasPump(upPos, out _) ? GeneratePath(current, upPos, out upTime) : null;
			
			Point16 rightPos = inventory + new Point16(1, 0);
			double rightTime = 0;
			base.PathfindingStartDirection = pipedItemDirection;
			var right = !HasPump(rightPos, out _) ? GeneratePath(current, rightPos, out rightTime) : null;
			
			Point16 downPos = inventory + new Point16(0, 1);
			double downTime = 0;
			base.PathfindingStartDirection = pipedItemDirection;
			var down = !HasPump(downPos, out _) ? GeneratePath(current, downPos, out downTime) : null;

			pipedItemDirection = Point16.NegativeOne;
				
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

		protected override void SaveExtraData(TagCompound tag) {
			static TagCompound SaveItem(PipedItem item) {
				TagCompound itemTag = new TagCompound();
				item.SaveData(itemTag);
				return itemTag;
			}

			static TagCompound SavePump(Point16 location, Ref<int> timer) {
				Tile tile = Main.tile[location.X, location.Y];

				return new TagCompound() {
					["x"] = location.X,
					["y"] = location.Y,
					["time"] = timer.Value,
					["dir"] = (byte)(tile.TileFrameX / 18)
				};
			}

			tag["items"] = items.OfType<PipedItem>().Select(SaveItem).ToList();

			tag["pumps"] = pumpTimers.Select(static kvp => SavePump(kvp.Key, kvp.Value)).ToList();
		}

		protected override void LoadExtraData(TagCompound tag) {
			items.Clear();

			if (tag.GetList<TagCompound>("items") is List<TagCompound> itemTags) {
				foreach (var item in itemTags)
					items.Add(PipedItem.LoadData(this, item));
			}

			pumpTimers.Clear();

			if (tag.GetList<TagCompound>("pumps") is List<TagCompound> pumpTags) {
				foreach (var pump in pumpTags) {
					if (!pump.TryGet("x", out short x))
						continue;

					if (!pump.TryGet("y", out short y))
						continue;

					if (!pump.TryGet("time", out int time))
						continue;

					if (!pump.TryGet("dir", out byte dir))
						continue;

					pumpTimers[new Point16(x, y)] = new Ref<int>(time);

					Tile tile = Main.tile[x, y];

					ref var netInfo = ref tile.Get<NetworkInfo>();

					if (netInfo.IsPump && (netInfo.Type & NetworkType.Items) == NetworkType.Items) {
						tile.TileFrameX = (short)(dir * 18);
						tile.TileFrameY = 0;
					}
				}
			}
		}

		public override void SendExtraData(BinaryWriter writer) {
			writer.Write(adjacentInventoryTiles.Count);
			foreach (var adjacent in adjacentInventoryTiles)
				writer.Write(adjacent);

			writer.Write(pumpTimers.Count);
			foreach (var (position, timer) in pumpTimers) {
				writer.Write(position);
				writer.Write(timer.Value);
			}

			writer.Write(items.Count);
			foreach (var item in items) {
				if (item is not { Destroyed: false })
					writer.Write(true);
				else {
					writer.Write(false);
					item.WriteTo(writer, full: true);
				}
			}
		}

		public override void ReceiveExtraData(BinaryReader reader) {
			adjacentInventoryTiles.Clear();

			int adjacentCount = reader.ReadInt32();
			for (int i = 0; i < adjacentCount; i++)
				adjacentInventoryTiles.Add(reader.ReadPoint16());

			pumpTimers.Clear();

			int pumpCount = reader.ReadInt32();
			for (int i = 0; i < pumpCount; i++)
				pumpTimers.Add(reader.ReadPoint16(), new Ref<int>(reader.ReadInt32()));

			items.Clear();

			int itemCount = reader.ReadInt32();
			for (int i = 0; i < itemCount; i++) {
				if (reader.ReadBoolean())
					items.Add(null);
				else {
					// Read ahead to get the ID so that the receive logic overwrites the data in this instance instead of trying to find an index to replace
					long pos = reader.BaseStream.Position;
					
					reader.ReadBoolean();  // bool fullSync
					int id = reader.ReadInt32();

					items.Add(new PipedItem(this, Point16.Zero, Point16.Zero, Point16.Zero, null, null, id));

					reader.BaseStream.Position = pos;

					PipedItem.CreateOrUpdateFromNet(reader);
				}
			}
		}
	}
}
