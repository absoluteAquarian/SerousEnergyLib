using SerousEnergyLib.Pathfinding;
using SerousEnergyLib.Pathfinding.Nodes;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SerousEnergyLib.Systems {
#pragma warning disable CS1591
	/// <summary>
	/// An object representing pathfinding trees in a network of <see cref="BaseNetworkTile"/> tiles
	/// </summary>
	public partial class NetworkInstance : IDisposable {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				nextID = 0;
			}
		}

		/// <summary>
		/// Which type of network tiles this network can detect when performing pathfinding
		/// </summary>
		public NetworkType Filter { get; private set; }

		private Dictionary<Point16, CoarseNode> coarsePath = new();
		private Dictionary<Point16, NetworkInstanceNode> nodes = new();
		private HashSet<Point16> foundJunctions = new();

		public int ID { get; private set; }

		// Used during network removal to get the index in the parent collection
		// This field should not be used otherwise
		internal int networkIndex = -1;

		public bool IsEmpty => nodes.Count == 0;

		public int EntryCount => nodes.Count;

		public Point16 FirstNode => nodes.Count == 0 ? Point16.NegativeOne : nodes.Keys.First();

		internal NetworkInstance(NetworkType filter) {
			Filter = filter;

			innerCoarseNodePathfinder = new AStar<CoarseNodeEntry>(
				CoarseNode.Stride * CoarseNode.Stride,
				CreatePathfindingEntry,
				HasPathfindingEntry,
				CanContinuePath,
				GetWalkableDirections);
		}

		public static NetworkInstance CreateNetwork(NetworkType type) {
			return type switch {
				NetworkType.Items => new ItemNetwork(),
				NetworkType.Fluids => new FluidNetwork(),
				NetworkType.Power => new PowerNetwork(),
				_ => throw new ArgumentException("Type was invalid", nameof(type))
			};
		}

		internal static int nextID;

		internal void ReserveNextID() {
			ID = nextID++;
		}

		/// <summary>
		/// Update the network instance here
		/// </summary>
		public virtual void Update() { }

		internal bool delayCoarsePathCalculationFromCopy;

		internal void CopyFrom(NetworkInstance other) {
			if (disposed)
				throw new ObjectDisposedException("this");

			if (Filter != other.Filter)
				throw new ArgumentException("Network instances had mismatched filters", nameof(other));

			foreach (var (loc, node) in other.nodes) {
				nodes[loc] = node;

				Point16 coarse = loc / CoarseNode.Coarseness;

				if (!coarsePath.ContainsKey(coarse))
					coarsePath[coarse] = new();

				int x = coarse.X;
				int y = coarse.Y;

				if (x < coarseLeft)
					coarseLeft = x;
				if (y < coarseTop)
					coarseTop = y;
				if (x > coarseRight)
					coarseRight = x;
				if (y > coarseBottom)
					coarseBottom = y;
			}

			foreach (var junc in other.foundJunctions)
				foundJunctions.Add(junc);

			// Re-evaluate the coarse paths, even the existing ones, since they could've been affected by the new network
			if (!delayCoarsePathCalculationFromCopy) {
				totalCoarsePaths = 1;

				foreach (var coarseLocation in coarsePath.Keys)
					UpdateCoarseNode(coarseLocation);
			}

			CopyExtraData(other);
		}

		/// <summary>
		/// Copy extra data from <paramref name="source"/> into this network here
		/// </summary>
		protected virtual void CopyExtraData(NetworkInstance source) { }

		// Called in Network.cs when placing an entry
		internal void AddEntry(Point16 location) {
			if (disposed)
				throw new ObjectDisposedException("this");

			if (!recalculating && nodes.ContainsKey(location))
				return;  // Entry already exists

			int x = location.X, y = location.Y;

			Tile tile = Main.tile[location.X, location.Y];

			Span<Point16> adjacent = stackalloc Point16[4];

			int nextIndex = 0;

			var node = CreateNetworkNode(x, y, ref adjacent, ref nextIndex);
			nodes.Add(location, node);

			// If the source tile was a junction, it has no adjacent nodes.  Manually check each direction
			if (TileLoader.GetTile(tile.TileType) is NetworkJunction) {
				adjacent[0] = new Point16(x - 1, y);
				adjacent[1] = new Point16(x, y - 1);
				adjacent[2] = new Point16(x + 1, y);
				adjacent[3] = new Point16(x, y + 1);
				nextIndex = 4;
			}

			// Update adjacent nodes
			Span<Point16> adjacentAdjacent = stackalloc Point16[4];
			int nextAdjacentIndex = 0;
			for (int i = 0; i < nextIndex; i++) {
				var adj = adjacent[i];

				if (HasEntry(adj))
					nodes[adj] = CreateNetworkNode(adj.X, adj.Y, ref adjacentAdjacent, ref nextAdjacentIndex);
			}

			OnEntryAdded(location);

			// Preemptively add a coarse node entry
			Point16 coarse = new Point16(x, y) / CoarseNode.Coarseness;
			if (!coarsePath.ContainsKey(coarse))
				coarsePath[coarse] = new CoarseNode();

			if (!recalculating) {
				// Refresh the coarse node
				UpdateCoarseNode(coarse);
			}
		}

		/// <summary>
		/// This method is called after am entry is added to this network
		/// </summary>
		/// <param name="location">The tile location of the entry</param>
		public virtual void OnEntryAdded(Point16 location) { }

		internal static List<NetworkInstance> RemoveEntry(NetworkInstance orig, int x, int y) {
			Point16 location = new Point16(x, y);
			orig.nodes.Remove(location);

			orig.OnEntryRemoved(location);

			// If no more nodes exist in the source coarse node, then remove it as well
			Point16 coarse = location / CoarseNode.Coarseness;
			Point16 truncated = coarse * CoarseNode.Coarseness;

			bool hasAny = false;
			for (int fineY = 0; fineY < CoarseNode.Stride; fineY++) {
				for (int fineX = 0; fineX < CoarseNode.Stride; fineX++) {
					if (orig.HasEntry(truncated.X + fineX, truncated.Y + fineY)) {
						hasAny = true;
						break;
					}
				}
			}

			if (!hasAny)
				orig.coarsePath.Remove(coarse);
			else
				orig.UpdateCoarseNode(coarse);

			// Check if any of the cardinal directions have a path to any other
			// If they do, mark them as part of the "same network" after splitting
			Point16 left = location + new Point16(-1, 0), up = location + new Point16(0, -1), right = location + new Point16(1, 0), down = location + new Point16(0, 1);
			bool leftUpConnected = false, leftRightConnected = false, leftDownConnected = false,
				upRightConnected = false, upDownConnected = false,
				rightDownConnected = false;

			ignoreTravelTimeWhenPathfinding = true;

			if (orig.GeneratePath(left, up, out _) is not null)
				leftUpConnected = true;
			if (orig.GeneratePath(left, right, out _) is not null)
				leftRightConnected = true;
			if (orig.GeneratePath(left, down, out _) is not null)
				leftDownConnected = true;
			if (orig.GeneratePath(up, right, out _) is not null)
				upRightConnected = true;
			if (orig.GeneratePath(up, down, out _) is not null)
				upDownConnected = true;
			if (orig.GeneratePath(right, down, out _) is not null)
				rightDownConnected = true;

			ignoreTravelTimeWhenPathfinding = false;

			List<NetworkInstance> networks = new();
			bool origIDUsed = false;

			// Generate the "left" network
			NetworkInstance netLeft = null;
			if (orig.HasEntry(left)) {
				netLeft = CloneNetwork(orig, leftUpConnected, up, leftRightConnected, right, leftDownConnected, down);

				if (!netLeft.IsEmpty) {
					networks.Add(netLeft);

					origIDUsed = true;
					netLeft.ID = orig.ID;
				} else {
					netLeft.Dispose();
					netLeft = null;
				}
			}
			
			// Generate the "up" network
			NetworkInstance netUp = null;
			if (orig.HasEntry(up) && !(netLeft?.HasEntry(up) ?? false)) {
				netUp = CloneNetwork(orig, leftUpConnected, left, upRightConnected, right, upDownConnected, down);

				if (!netUp.IsEmpty) {
					networks.Add(netUp);

					if (!origIDUsed) {
						origIDUsed = true;
						netUp.ID = orig.ID;
					} else
						netUp.ReserveNextID();
				} else {
					netUp.Dispose();
					netUp = null;
				}
			}

			// Generate the "right" network
			NetworkInstance netRight = null;
			if (orig.HasEntry(right) && !(netLeft?.HasEntry(right) ?? false) && !(netUp?.HasEntry(right) ?? false)) {
				netRight = CloneNetwork(orig, leftRightConnected, left, upRightConnected, up, rightDownConnected, down);

				if (!netRight.IsEmpty) {
					networks.Add(netRight);

					if (!origIDUsed) {
						origIDUsed = true;
						netRight.ID = orig.ID;
					} else
						netRight.ReserveNextID();
				} else {
					netRight.Dispose();
					netRight = null;
				}
			}

			// Generate the "down" network
			if (orig.HasEntry(down) && !(netLeft?.HasEntry(down) ?? false) && !(netUp?.HasEntry(down) ?? false) && !(netRight?.HasEntry(down) ?? false)) {
				NetworkInstance netDown = CloneNetwork(orig, leftDownConnected, left, upDownConnected, up, rightDownConnected, right);

				if (!netDown.IsEmpty) {
					networks.Add(netDown);

					if (!origIDUsed)
						netDown.ID = orig.ID;
					else
						netDown.ReserveNextID();
				} else
					netDown.Dispose();
			}

			return networks;
		}

		private static NetworkInstance CloneNetwork(NetworkInstance orig, bool dirOneConnected, Point16 dirOne, bool dirTwoConnected, Point16 dirTwo, bool dirThreeConnected, Point16 dirThree) {
			NetworkInstance net = CreateNetwork(orig.Filter);
			net.delayCoarsePathCalculationFromCopy = true;
			net.CopyFrom(orig);
			net.delayCoarsePathCalculationFromCopy = false;
			
			if (!dirOneConnected)
				RemoveUnnecessaryNodes(net, dirOne);
			if (!dirTwoConnected)
				RemoveUnnecessaryNodes(net, dirTwo);
			if (!dirThreeConnected)
				RemoveUnnecessaryNodes(net, dirThree);

			HashSet<Point16> remainingCoarseNodes = new();
			foreach (var loc in net.nodes.Keys)
				remainingCoarseNodes.Add(loc / CoarseNode.Coarseness);

			// Recalculate the coarse path
			net.totalCoarsePaths = 1;
			net.coarsePath.Clear();

			foreach (var loc in remainingCoarseNodes) {
				net.coarsePath[loc] = new CoarseNode();
				net.UpdateCoarseNode(loc);
			}

			net.OnNetworkCloned(orig);

			return net;
		}

		/// <summary>
		/// This method is called after this network was cloned from another network and this network's nodes were updated
		/// </summary>
		protected virtual void OnNetworkCloned(NetworkInstance orig) { }

		private static void RemoveUnnecessaryNodes(NetworkInstance net, Point16 start, bool updateCoarseNodes = true) {
			Queue<Point16> queue = new Queue<Point16>();
			queue.Enqueue(start);

			// Remove this node and its adjacent nodes
			while (queue.TryDequeue(out Point16 pos)) {
				if (!net.TryGetEntry(pos, out NetworkInstanceNode node))
					continue;

				net.nodes.Remove(pos);
				net.foundJunctions.Remove(pos);

				net.OnEntryRemoved(pos);

				foreach (var adj in node.adjacent)
					queue.Enqueue(adj);
			}
		}

		/// <summary>
		/// This method is called after an entry is removed from this network, but before its corresponding tile is destroyed
		/// </summary>
		/// <param name="location">The tile location of the entry to remove</param>
		public virtual void OnEntryRemoved(Point16 location) { }

		private NetworkInstanceNode CreateNetworkNode(int x, int y, ref Span<Point16> adjacent, ref int nextIndex) {
			adjacent.Clear();
			nextIndex = 0;

			Point16 location = new Point16(x, y);

			Tile tile = Main.tile[x, y];
			
			if (TileLoader.GetTile(tile.TileType) is not NetworkJunction) {
				CheckTile(x, y, -1, 0, ref adjacent, ref nextIndex);
				CheckTile(x, y, 0, -1, ref adjacent, ref nextIndex);
				CheckTile(x, y, 1, 0, ref adjacent, ref nextIndex);
				CheckTile(x, y, 0, 1, ref adjacent, ref nextIndex);
			} else {
				// Junctions need to be handled specifically in any pathfinding due to them having unorthodox connection directions
				foundJunctions.Add(location);
			}

			return new NetworkInstanceNode(location, nextIndex == 0 ? Array.Empty<Point16>() : adjacent[..nextIndex].ToArray());
		}

		internal void Reset() {
			nodes.Clear();
			foundJunctions.Clear();
			coarsePath.Clear();
			totalCoarsePaths = 0;

			coarseLeft = -1;
			coarseTop = -1;
			coarseRight = -1;
			coarseBottom = -1;

			OnNetworkReset();
		}

		/// <summary>
		/// This method is called when the network is reset before recalulation of its paths begins
		/// </summary>
		public virtual void OnNetworkReset() { }

		private void CheckTile(int x, int y, int dirX, int dirY, ref Span<Point16> adjacent, ref int nextIndex) {
			// Ignore the "parent" tile
			if (dirX == 0 && dirY == 0)
				return;

			// Ignore ordinal tiles
			if (dirX != 0 && dirY != 0)
				return;

			Point16 orig = new Point16(x, y);
			Point16 pos = new Point16(x + dirX, y + dirY);
			if (IsValidTile(x + dirX, y + dirY) && CanContinuePath(orig, pos)) {
				// If not recalculating, the other node must exist in this network
				if (recalculating || HasEntry(pos)) {
					adjacent[nextIndex++] = pos;

					if (recalculating) {
						queue.Enqueue(pos);

						Tile check = Main.tile[x + dirX, y + dirY];

						// If it's a junction, add the "next" tile that it should redirect to based on this tile's location
						if (TileLoader.GetTile(check.TileType) is NetworkJunction)
							CheckTile_FindJunctionOppositeTile(pos.X, pos.Y, dirX, dirY);
					}
				}
			}
		}

		/// <summary>
		/// Indexed by <c>[mode, direction]</c><br/>
		/// Mode = tile frame X / 18<br/>
		/// Directions:<br/>
		/// Left = 0<br/>
		/// Down = 1<br/>
		/// Right = 2<br/>
		/// Up = 3<br/>
		/// </summary>
		internal static readonly (int offsetX, int offsetY)[,] junctionDirectionRedirect = new (int, int)[3, 4] {
			// Entering from: left, up, right, down
			// Mode 0
			{ (1, 0), (0, 1), (-1, 0), (0, -1) },
			// Mode 1
			{ (0, 1), (1, 0), (0, -1), (-1, 0) },
			// Mode 2
			{ (0, -1), (-1, 0), (0, 1), (1, 0) }
		};

		private void CheckTile_FindJunctionOppositeTile(int x, int y, int dirX, int dirY) {
			Tile check = Main.tile[x, y];

			int mode = check.TileFrameX / 18;

			if (mode > 2)
				return;

			int index;
			if (dirY > 0) {
				// Entering the junction from above
				index = 1;
			} else if (dirY < 0) {
				// Entering the junction from below
				index = 3;
			} else if (dirX > 0) {
				// Entering the junction from the left
				index = 0;
			} else if (dirX < 0) {
				// Entering the junction from the right
				index = 2;
			} else
				return;

			(int offsetX, int offsetY) = junctionDirectionRedirect[mode, index];

			Point16 pos = new Point16(x + offsetX, y + offsetY);
			if (CanContinuePath(new Point16(x, y), pos))
				queue.Enqueue(pos);
		}

		internal bool IsValidTile(int x, int y) {
			if (!WorldGen.InWorld(x, y))
				return false;

			Tile tile = Main.tile[x, y];
			return tile.HasTile && (tile.Get<NetworkInfo>().Type & Filter) != 0;
		}

		#region Implement IDisposable
		private bool disposed;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			if (disposed)
				return;

			disposed = true;

			if (disposing) {
				nodes.Clear();
				coarsePath.Clear();
				foundJunctions.Clear();
				innerCoarseNodePathfinder.Dispose();
			}

			DisposeSelf(disposing);

			nodes = null;
			coarsePath = null;
			foundJunctions = null;
			innerCoarseNodePathfinder = null;
			networkIndex = -1;
		}

		protected virtual void DisposeSelf(bool disposing) { }

		~NetworkInstance() => Dispose(false);
		#endregion
	}
}
