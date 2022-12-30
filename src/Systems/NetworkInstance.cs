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

namespace SerousEnergyLib.Systems {
	public class NetworkInstance {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				nextID = 0;
			}
		}

		public NetworkType Filter { get; private set; }

		private readonly Dictionary<Point16, NetworkInstanceNode> nodes = new();
		private readonly HashSet<Point16> foundJunctions = new();

		public int ID { get; private set; }

		public NetworkInstance(NetworkType filter) {
			Filter = filter;
		}

		internal static int nextID;

		internal void ReserveNextID() {
			ID = nextID++;
		}

		private Queue<Point16> queue = new();

		public void Recalculate(Point16 start) {
			nodes.Clear();
			foundJunctions.Clear();

			if (!IsValidTile(start.X, start.Y))
				return;

			HashSet<Point16> walked = new();

			queue.Clear();
			queue.Enqueue(start);

			Span<Point16> adjacent = stackalloc Point16[4];

			while (queue.TryDequeue(out Point16 location)) {
				if (!walked.Add(location))
					continue;

				int x = location.X, y = location.Y;

				Tile tile = Main.tile[location.X, location.Y];

				adjacent.Clear();

				int nextIndex = 0;
				if (TileLoader.GetTile(tile.TileType) is not NetworkJunction) {
					CheckTile(x, y, -1, 0, ref adjacent, ref nextIndex);
					CheckTile(x, y, 0, -1, ref adjacent, ref nextIndex);
					CheckTile(x, y, 1, 0, ref adjacent, ref nextIndex);
					CheckTile(x, y, 0, 1, ref adjacent, ref nextIndex);
				} else {
					// Junctions need to be handled specifically in any pathfinding due to them having unorthodox connection directions
					foundJunctions.Add(location);
				}

				nodes.Add(location, new NetworkInstanceNode(location, nextIndex == 0 ? Array.Empty<Point16>() : adjacent[..(nextIndex - 1)].ToArray()));
			}
		}

		public bool HasEntry(Point16 location) => nodes.ContainsKey(location);

		public bool TryGetEntry(Point16 location, out NetworkInstanceNode result) {
			if (nodes.TryGetValue(location, out NetworkInstanceNode value)) {
				result = value;
				return true;
			}

			result = default;
			return false;
		}

		public bool IsJunction(Point16 location) => foundJunctions.Contains(location);

		private void CheckTile(int x, int y, int dirX, int dirY, ref Span<Point16> adjacent, ref int nextIndex) {
			// Ignore the "parent" tile
			if (dirX == 0 && dirY == 0)
				return;

			// Ignore ordinal tiles
			if (dirX != 0 && dirY != 0)
				return;

			if (IsValidTile(x + dirX, y + dirY)) {
				Point16 pos = new Point16(x + dirX, y + dirY);
				adjacent[nextIndex++] = pos;
				queue.Enqueue(pos);
			}
		}

		internal bool IsValidTile(int x, int y) {
			if (!WorldGen.InWorld(x, y))
				return false;

			Tile tile = Main.tile[x, y];
			return (tile.Get<NetworkInfo>().Type & Filter) != 0;
		}

		internal void CombineFrom(NetworkInstance other) {
			if (Filter != other.Filter)
				return;  // Cannot combine the networks

			foreach (var (pos, node) in other.nodes)
				nodes.Add(pos, node);

			foreach (var junction in foundJunctions)
				foundJunctions.Add(junction);
		}

		public void SaveData(TagCompound tag) {
			tag["filter"] = (byte)Filter;

			// Save the "start" of the network so that the logic is forced to recalculate it when loading the world
			if (nodes.Count > 0)
				tag["start"] = nodes.Keys.First();
		}

		public void LoadData(TagCompound tag) {
			byte filter = tag.GetByte("filter");

			if (filter == 0 || filter > (byte)(NetworkType.Items | NetworkType.Fluids | NetworkType.Power))
				throw new IOException("Invalid filter number: " + filter);

			Filter = (NetworkType)filter;

			if (tag.ContainsKey("start") && tag["start"] is Point16 start)
				Recalculate(start);
		}
	}

	public readonly struct NetworkInstanceNode {
		public readonly Point16 location;
		public readonly Point16[] adjacent;

		internal NetworkInstanceNode(Point16 location, Point16[] adjacent) {
			this.location = location;
			this.adjacent = adjacent;
		}

		public override bool Equals(object obj) => obj is NetworkInstanceNode node && location == node.location;

		public override int GetHashCode() => location.GetHashCode();

		public static bool operator ==(NetworkInstanceNode left, NetworkInstanceNode right) => left.Equals(right);

		public static bool operator !=(NetworkInstanceNode left, NetworkInstanceNode right) => !(left == right);
	}
}
