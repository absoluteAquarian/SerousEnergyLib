using Microsoft.Xna.Framework;
using SerousEnergyLib.API;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Systems;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.Tiles;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.Pathfinding.Objects {
	/// <summary>
	/// An object representing an item moving in an item pipe
	/// </summary>
	public sealed class PipedItem {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				nextUniqueID = 0;
			}
		}

		private static int nextUniqueID;

		/// <summary>
		/// The tile coordinates where this item is heading toward
		/// </summary>
		public Point16 Target { get; private set; }

		private List<Point16> path;
		private Item item;

		private int pathIndex = 1;

		private double travelFactor;  // How far along in the current tile the item has moved

		/// <summary>
		/// The tile that this item was previously located at
		/// </summary>
		public Point16 PreviousTile { get; private set; }

		/// <summary>
		/// The tile that this item is currently located at
		/// </summary>
		public Point16 CurrentTile { get; private set; }

		/// <summary>
		/// The next tile that this item will be located at
		/// </summary>
		public Point16 NextTile { get; private set; } = Point16.NegativeOne;

		private int aliveTime = -1;

		/// <summary>
		/// The network that this item is assigned to
		/// </summary>
		public readonly ItemNetwork network;

		#pragma warning disable CS1591
		public readonly int UniqueID;

		internal bool delayPumpRetargetting = true;

		internal PipedItem(ItemNetwork network, Point16 source, Point16 current, Point16 target, List<Point16> path, Item item, int id) {
			this.network = network;
			PreviousTile = source;
			CurrentTile = current;
			Target = target;
			this.path = path;
			this.item = item;
			UniqueID = id;
		}

		internal PipedItem(ItemNetwork network, Point16 source, Point16 current, Point16 target, List<Point16> path, Item item) : this(network, source, current, target, path, item, nextUniqueID++) { }

		public void SaveData(TagCompound tag) {
			if (Destroyed)
				return;

			tag["item"] = ItemIO.Save(item);
			tag["target"] = Target;
			tag["path"] = path;
			tag["curPath"] = pathIndex;
			tag["travel"] = travelFactor;
			tag["previous"] = PreviousTile;
			tag["current"] = CurrentTile;
			tag["next"] = NextTile;
		}

		public static PipedItem LoadData(ItemNetwork parent, TagCompound tag) {
			if (tag.GetCompound("item") is not TagCompound itemTag)
				return null;

			Item item = ItemIO.Load(itemTag);

			if (!tag.TryGet("target", out Point16 target))
				target = Point16.NegativeOne;

			if (!tag.TryGet("path", out List<Point16> path))
				target = Point16.NegativeOne;

			int pathIndex = tag.GetInt("curPath");
			double travelFactor = tag.GetDouble("travel");

			if (!tag.TryGet("previous", out Point16 previous))
				return null;

			if (!tag.TryGet("current", out Point16 current))
				return null;

			if (!tag.TryGet("next", out Point16 next))
				return null;

			return new PipedItem(parent, previous, current, target, path, item) {
				NextTile = next,
				pathIndex = pathIndex,
				travelFactor = travelFactor
			};
		}

		internal void WriteTo(BinaryWriter writer, bool full) {
			Netcode.WriteNetworkInstance(writer, network);

			writer.Write(full);

			writer.Write(UniqueID);
			writer.Write(Target);
			writer.Write(pathIndex);
			writer.Write(travelFactor);
			writer.Write(PreviousTile);
			writer.Write(CurrentTile);
			writer.Write(NextTile);
			writer.Write(aliveTime);
			writer.Write(Destroyed);
			writer.Write(delayPumpRetargetting);

			if (full) {
				writer.Write(path.Count);

				foreach (var node in path)
					writer.Write(node);

				ItemIO.Send(item, writer, writeStack: true);
			}
		}

		internal static void CreateOrUpdateFromNet(BinaryReader reader) {
			ItemNetwork net = Netcode.ReadNetworkInstanceOnClient(reader) as ItemNetwork ?? throw new IOException("PipedItem parent network was not an item network");

			bool fullSync = reader.ReadBoolean();

			int id = reader.ReadInt32();
			PipedItem item = null;

			// Find an existing instance.  If there wasn't one, default to creating a new instance
			foreach (var instance in net.items) {
				if (instance.UniqueID == id) {
					item = instance;
					break;
				}
			}

			if (item is null && !fullSync) {
				// Not enough information to create the instance
				return;
			}

			Point16 target = reader.ReadPoint16();
			int pathIndex = reader.ReadInt32();
			double travelFactor = reader.ReadDouble();
			Point16 previousTile = reader.ReadPoint16();
			Point16 currentTile = reader.ReadPoint16();
			Point16 nextTile = reader.ReadPoint16();
			int aliveTime = reader.ReadInt32();
			bool destroyed = reader.ReadBoolean();
			bool delayPump = reader.ReadBoolean();

			if (item is null) {
				List<Point16> path = new List<Point16>();
				int count = reader.ReadInt32();

				for (int i = 0; i < count; i++)
					path.Add(reader.ReadPoint16());

				Item data = ItemIO.Receive(reader, readStack: true);

				// Make a new instance
				item = new PipedItem(net, previousTile, currentTile, target, path, data, id) {
					Destroyed = destroyed,
					NextTile = nextTile,
					delayPumpRetargetting = delayPump
				};

				net.AddPipedItem(item);
			} else {
				if (fullSync) {
					List<Point16> path = new List<Point16>();
					int count = reader.ReadInt32();

					for (int i = 0; i < count; i++)
						path.Add(reader.ReadPoint16());

					Item data = ItemIO.Receive(reader, readStack: true);

					item.path = path;
					item.item = data;
				}

				item.Target = target;
				item.pathIndex = pathIndex;
				item.travelFactor = travelFactor;
				item.PreviousTile = previousTile;
				item.CurrentTile = currentTile;
				item.NextTile = nextTile;
				item.aliveTime = aliveTime;
				item.Destroyed = destroyed;
				item.delayPumpRetargetting = delayPump;
			}
		}

		internal void Update() {
			// Only allow updates on the server
			// The server will inform the clients of the new location information
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			if (Destroyed)
				return;

			aliveTime++;

			Point16 cur = CurrentTile;
			Tile current = Main.tile[cur.X, cur.Y];

			// If the current tile isn't a transport tile, drop this object's item (unless it's a pump, then reverse direction and keep wandering
			// OR try to insert it into a machine/inventory if applicable
			ModTile modTile = TileLoader.GetTile(current.TileType);
			if (modTile is not IItemTransportTile transport) {
				Item import = item.Clone();

				AttemptChestImport(import, cur);
				AttemptMachineImport(import, cur);

				if (import.IsAir) {
					Destroy(dropItem: false);
					return;
				}

				// Only write the stack in case a machine decided to do some funny business
				item.stack = import.stack;

				// Move back to the previous tile
				(NextTile, PreviousTile) = (PreviousTile, NextTile);
				travelFactor = 0;

				// Attempt to retarget a new inventory
				Target = Point16.NegativeOne;

				Netcode.SyncPipedItem(this, fullSync: false);
				return;
			} else if (modTile is not IItemPumpTile)
				delayPumpRetargetting = false;  // Item has exited the pump.  Allow it to be redirected by future pumps

			bool hackPumpRetarget = false;
			if (modTile is IItemPumpTile && !delayPumpRetargetting) {
				// Reverse direction and force a retarget
				NextTile = PreviousTile;
				Target = Point16.NegativeOne;
				hackPumpRetarget = true;
			}

			// Attempt to find a new target every 30 ticks
			bool netSent = false;
			if (Target == Point16.NegativeOne && (hackPumpRetarget || aliveTime % 30 == 0)) {
				FindNewTarget();

				if (Target != Point16.NegativeOne) {
					Netcode.SyncPipedItem(this, fullSync: true);
					netSent = true;
				}
			}

			// If there is a target, move along the path to the target
			// Otherwise, wander around in the network unless this is at a dead end, then just keep moving until this exits the network
			if (Target != Point16.NegativeOne) {
				double pixelsPerSecond = transport.TransportSpeed * 16;
				double pixelsPerTick = pixelsPerSecond / 60d;

				// Pixel distance ranges from 0 to 16, whereas travel factor ranges from 0 to 1
				double factorPerTick = pixelsPerTick / 16;

				if (factorPerTick > 1) {
					// Weird things would happen if the item transportation were too fast
					factorPerTick = 1;
				}

				travelFactor += factorPerTick;

				if (NextTile == Point16.NegativeOne) {
					// NextTile needs to be initialized
					NextTile = pathIndex < path.Count ? path[pathIndex] : Target;
				}

				if (travelFactor > 1) {
					travelFactor %= 1;

					if (pathIndex < path.Count)
						pathIndex++;

					PreviousTile = CurrentTile;
					CurrentTile = NextTile;
					NextTile = pathIndex < path.Count ? path[pathIndex] : Target;
				}
			} else {
				// Once the middle of the tile has been reached, move in a valid direction that isn't backwards
				double oldTravel = travelFactor;

				travelFactor += transport.TransportSpeed * 16d / 60d;

				if (oldTravel < 0.5 && travelFactor >= 0.5) {
					Point16 dir = FindValidRandomDirection();

					Point16 moveDir = CurrentTile - PreviousTile;

					PreviousTile = CurrentTile;
					CurrentTile = NextTile;

					if (dir == Point16.NegativeOne) {
						// No valid direction; item is about to exit network at a dead end
						NextTile = CurrentTile + moveDir;
					} else {
						// Move in the new random direction
						NextTile = CurrentTile + dir;
					}
				}
			}

			if (!netSent)
				Netcode.SyncPipedItem(this, fullSync: false);
		}

		public Item GetItemClone() => item?.Clone();

		private void AttemptChestImport(Item import, Point16 entry) {
			if (import.IsAir)
				return;

			if (NetworkHandler.locationToChest.TryGetValue(entry, out int chestNum)) {
				Chest chest = Main.chest[chestNum];

				chest.ImportItem(chestNum, import);
			}

			if (import.IsAir)
				Destroy(dropItem: false);
		}

		private void AttemptMachineImport(Item import, Point16 entry) {
			if (import.IsAir)
				return;

			if (!IMachine.TryFindMachine(entry, out IInventoryMachine machine))
				return;

			IInventoryMachine.ImportItem(machine, import);

			if (import.IsAir)
				Destroy(dropItem: false);
		}

		private void FindNewTarget() {
			Item import = item.Clone();

			this.path = null;
			pathIndex = -1;

			network.ignoredValidTargets.Clear();

			if (network.FindValidImportTarget(import, out Point16 inventory, out _) && network.AttemptToGeneratePathToInventoryTarget(CurrentTile, inventory) is List<Point16> path)
				UseTarget(inventory, path);
		}

		private void UseTarget(Point16 target, List<Point16> pathToTarget) {
			Target = target;
			path = pathToTarget;
			pathIndex = 1;
			NextTile = path.Count > 1 ? path[1] : Target;
		}

		private Point16 FindValidRandomDirection() {
			Point16 moveDir = CurrentTile - PreviousTile;

			List<Point16> possible = new();

			// Get a valid direction that isn't backwards
			if (moveDir.X < 0 && moveDir.Y == 0) {
				// Moving to the left
				possible.Add(new Point16(-1, 0));  // Left
				possible.Add(new Point16(0, -1));  // Up
				possible.Add(new Point16(0, 1));   // Down
			} else if (moveDir.X == 0 && moveDir.Y < 0) {
				// Moving upward
				possible.Add(new Point16(-1, 0));  // Left
				possible.Add(new Point16(1, 0));   // Right
				possible.Add(new Point16(0, 1));   // Down
			} else if (moveDir.X > 0 && moveDir.Y == 0) {
				// Moving to the right
				possible.Add(new Point16(0, -1));  // Up
				possible.Add(new Point16(1, 0));   // Right
				possible.Add(new Point16(0, 1));   // Down
			} else if (moveDir.X == 0 && moveDir.Y > 0) {
				// Moving downward
				possible.Add(new Point16(-1, 0));  // Left
				possible.Add(new Point16(1, 0));   // Right
				possible.Add(new Point16(0, 1));   // Down
			} else {
				// Invalid direction or not moving
				possible.Add(new Point16(-1, 0));  // Left
				possible.Add(new Point16(0, -1));  // Up
				possible.Add(new Point16(1, 0));   // Right
				possible.Add(new Point16(0, 1));   // Down
			}

			for (int i = possible.Count - 1; i >= 0; i--) {
				if (!network.HasEntry(possible[i]))
					possible.RemoveAt(i);
			}

			// No valid direction?  Keep moving in the same direction since the item is about to exit the network
			if (possible.Count == 0)
				return Point16.NegativeOne;

			return possible[Main.rand.Next(possible.Count)];
		}

		public bool GetItemDrawInformation(out Item item, out Vector2 center, out float size) {
			Point16 cur = CurrentTile;
			Tile current = Main.tile[cur.X, cur.Y];
			ModTile modTile = TileLoader.GetTile(current.TileType);

			if (cur == Target) {
				// Target was either not in the network or is a chest
				// Draw using the previous tile's information
				Point16 prev = PreviousTile;
				Tile prevTile = Main.tile[prev.X, prev.Y];

				if (TileLoader.GetTile(prevTile.TileType) is IPipedItemDrawingTile prevDrawingtile)
					size = prevDrawingtile.GetItemSize(prev.X, prev.Y);
				else
					size = 3.85f * 2f;
			} else if (modTile is not IPipedItemDrawingTile drawingTile) {
				item = null;
				center = Vector2.Zero;
				size = 0;
				return false;
			} else
				size = drawingTile.GetItemSize(cur.X, cur.Y);

			item = this.item.Clone();

			Vector2 toCurrent = (cur - PreviousTile).ToVector2() * 8;
			Vector2 fromCurrent = (NextTile - cur).ToVector2() * 8;

			center = cur.ToWorldCoordinates();

			if (travelFactor < 0.5) {
				// Moving from previous to current
				center -= toCurrent * ((float)(0.5 - travelFactor) * 2);
			} else {
				// Moving from current to next
				center += fromCurrent * ((float)(travelFactor - 0.5) * 2);
			}

			return true;
		}

		internal void OnTargetLost() {
			Target = Point16.NegativeOne;
			NextTile = PreviousTile;
		}

		public bool Destroyed { get; private set; }

		internal void Destroy(bool dropItem = true) {
			// Invalid instance
			if (Destroyed)
				return;

			Destroyed = true;

			if (dropItem)
				ItemFunctions.NewClonedItem(new EntitySource_Misc("PipedItem:Destroy"), CurrentTile.ToWorldCoordinates(), item, item.stack, item.prefix);

			Target = Point16.NegativeOne;
			PreviousTile = Point16.NegativeOne;
			CurrentTile = Point16.NegativeOne;
			NextTile = Point16.NegativeOne;
			pathIndex = -1;
			path = null;
			item = null;
			travelFactor = -1;
		}
	}
}
