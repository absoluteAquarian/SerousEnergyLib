using SerousEnergyLib.API;
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

		public Point16 Target { get; private set; }

		private List<Point16> path;
		private TagCompound itemData;

		private int currentTile;
		private int travelTimeInCurrentTile;
		private Point16 headingFrom;  // Used to help indicate where the item is located in the pipe

		public Point16 CurrentTile { get; private set; }

		private int aliveTime = -1;

		public readonly ItemNetwork network;

		public readonly int UniqueID;

		internal PipedItem(ItemNetwork network, Point16 source, Point16 target, List<Point16> path, Item item, int id) {
			this.network = network;
			headingFrom = source;
			this.Target = target;
			this.path = path;
			itemData = item is null ? null : ItemIO.Save(item);
			UniqueID = id;
		}

		internal PipedItem(ItemNetwork network, Point16 source, Point16 target, List<Point16> path, Item item) : this(network, source, target, path, item, nextUniqueID++) { }

		internal void WriteToPacket(ModPacket packet, bool full) {
			Netcode.WriteNetworkInstanceToPacket(packet, network);

			packet.Write(full);

			packet.Write(UniqueID);
			packet.Write(Target);
			packet.Write(currentTile);
			packet.Write((byte)travelTimeInCurrentTile);
			packet.Write(headingFrom);
			packet.Write(CurrentTile);
			packet.Write(aliveTime);
			packet.Write(Destroyed);

			if (full) {
				packet.Write(path.Count);

				foreach (var node in path)
					packet.Write(node);

				TagIO.Write(itemData, packet);
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
			int current = reader.ReadInt32();
			int travelTime = reader.ReadByte();
			Point16 headingFrom = reader.ReadPoint16();
			Point16 currentTile = reader.ReadPoint16();
			int aliveTime = reader.ReadInt32();
			bool destroyed = reader.ReadBoolean();

			if (item is null) {
				List<Point16> path = new List<Point16>();
				int count = reader.ReadInt32();

				for (int i = 0; i < count; i++)
					path.Add(reader.ReadPoint16());

				TagCompound data = TagIO.Read(reader);

				// Make a new instance
				item = new PipedItem(net, headingFrom, target, path, null, id) {
					itemData = data,
					Destroyed = destroyed
				};

				net.AddPipedItem(item);
			} else {
				if (fullSync) {
					List<Point16> path = new List<Point16>();
					int count = reader.ReadInt32();

					for (int i = 0; i < count; i++)
						path.Add(reader.ReadPoint16());

					TagCompound data = TagIO.Read(reader);

					item.path = path;
					item.itemData = data;
				}

				item.Target = target;
				item.currentTile = current;
				item.travelTimeInCurrentTile = travelTime;
				item.headingFrom = headingFrom;
				item.CurrentTile = currentTile;
				item.aliveTime = aliveTime;
				item.Destroyed = destroyed;
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

			// If the current tile isn't a transport tile, drop this object's item
			// OR try to insert it into a machine/inventory if applicable
			ModTile modTile = TileLoader.GetTile(current.TileType);
			if (modTile is not IItemTransportTile) {
				Item import = ItemIO.Load(itemData);

				AttemptChestImport(import, cur);
			}

			// Attempt to find a new target every 30 ticks
			if (Target == Point16.NegativeOne && aliveTime % 30 == 0)
				FindNewTarget();

			// If there is a target, move along the path to the target
			// Otherwise, wander around in the network unless this is at a dead end, then just keep moving until this exits the network
			if (Target != Point16.NegativeOne) {
				
			}
		}

		private void AttemptChestImport(Item import, Point16 entry) {
			int chestNum = Chest.FindChestByGuessing(entry.X, entry.Y);

			if (chestNum > -1) {
				Chest chest = Main.chest[chestNum];

				chest.ImportItem(import);
			}

			if (import.IsAir)
				Destroy(dropItem: false);
		}

		private void FindNewTarget() {
			Item item = ItemIO.Load(itemData);

			if (network.FindValidImportTarget(item, out Point16 inventory, out _)) {
				// Generate a path to the target
				var left = network.GeneratePath(CurrentTile, inventory + new Point16(-1, 0), out double leftTime);
				var up = network.GeneratePath(CurrentTile, inventory + new Point16(0, -1), out double upTime);
				var right = network.GeneratePath(CurrentTile, inventory + new Point16(1, 0), out double rightTime);
				var down = network.GeneratePath(CurrentTile, inventory + new Point16(0, 1), out double downTime);
				
				if (left is null && up is null && right is null && down is null) {
					// No path found
					return;
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
					UseTarget(inventory, left);
				} else if (up is not null && upTime <= rightTime && upTime <= downTime) {
					// Use the up path
					UseTarget(inventory, up);
				} else if (right is not null && rightTime <= downTime) {
					// Use the right path
					UseTarget(inventory, right);
				} else if (down is not null) {
					// Use the down path
					UseTarget(inventory, down);
				}
			}
		}

		private void UseTarget(Point16 target, List<Point16> pathToTarget) {
			Target = target;
			path = pathToTarget;
			currentTile = 0;
			// NOTE: Do NOT update "headingFrom" here so that the lerping treats the old value of "headingFrom" as the previous tile properly
		}

		public bool Destroyed { get; private set; }

		internal void Destroy(bool dropItem = true) {
			// Invalid instance
			if (Destroyed)
				return;

			Destroyed = true;

			if (dropItem) {
				Item item = ItemIO.Load(itemData);
				ItemFunctions.NewClonedItem(new EntitySource_Misc("PipedItem:Destroy"), CurrentTile.ToWorldCoordinates(), item, item.stack, item.prefix);
			}

			Target = Point16.NegativeOne;
			CurrentTile = Point16.NegativeOne;
			currentTile = -1;
			path = null;
			itemData = null;
			travelTimeInCurrentTile = -1;
			headingFrom = Point16.NegativeOne;
		}
	}
}
