using SerousEnergyLib.Pathfinding;
using SerousEnergyLib.Pathfinding.Nodes;
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

		private readonly Dictionary<Point16, CoarseNode> coarsePath = new();
		private readonly Dictionary<Point16, NetworkInstanceNode> nodes = new();
		private readonly HashSet<Point16> foundJunctions = new();
		private int totalCoarsePaths = 0;

		public int ID { get; private set; }

		private readonly AStar<CoarseNodeEntry> innerCoarseNodePathfinder;

		public NetworkInstance(NetworkType filter) {
			Filter = filter;

			innerCoarseNodePathfinder = new AStar<CoarseNodeEntry>(
				CoarseNode.Stride * CoarseNode.Stride,
				CreatePathfindingEntry,
				HasPathfindingEntry,
				CanContinuePath);
		}

		internal static int nextID;

		internal void ReserveNextID() {
			ID = nextID++;
		}

		private readonly Queue<Point16> queue = new();

		public void Recalculate(Point16 start) {
			nodes.Clear();
			foundJunctions.Clear();
			coarsePath.Clear();
			totalCoarsePaths = 0;

			if (!IsValidTile(start.X, start.Y))
				return;

			HashSet<Point16> walked = new();

			queue.Clear();
			queue.Enqueue(start);

			Span<Point16> adjacent = stackalloc Point16[4];

			int left = 65535;
			int right = -1;
			int top = 65535;
			int bottom = -1;

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

				// Preemptively add a coarse node entry
				Point16 coarse = new Point16(x / CoarseNode.Stride, y / CoarseNode.Stride);
				if (!coarsePath.ContainsKey(coarse))
					coarsePath[coarse] = new CoarseNode();

				// Calculate the new area of tiles that contains this entire network
				if (x < left)
					left = x;
				if (y < top)
					top = y;
				if (x > right)
					right = x;
				if (y > bottom)
					bottom = y;
			}

			Recalculate_GeneratePathfinding(left, top, right, bottom);
		}

		#region Pathfinding Recalculation Helpers
		private void Recalculate_GeneratePathfinding(int left, int top, int right, int bottom) {
			// Find the area of coarse tiles that contain the paths
			int coarseLeft = left / CoarseNode.Stride;
			int coarseTop = top / CoarseNode.Stride;
			int coarseRight = right / CoarseNode.Stride;
			int coarseBottom = bottom / CoarseNode.Stride;

			foreach (var (coarse, node) in coarsePath) {
				int coarseX = coarse.X;
				int fineX = coarseX * CoarseNode.Stride;
				int coarseY = coarse.Y;
				int fineY = coarseY * CoarseNode.Stride;

				if (coarseX > coarseLeft) {
					// There exists a node to the left of this one, so this one might have connections to it
					int absX = fineX;
					for (int y = 0; y < CoarseNode.Stride; y++) {
						int absY = y + fineY;

						if (HasEntry(absX, absY) && HasEntry(absX - 1, absY)) {
							// Generate paths within the node that go to this tile
							Recalculate_GeneratePathfinding_GeneratePaths(node, absX, absY, fineX, fineY, ConnectionDirection.Left);
						}
					}
				}

				if (coarseY > coarseTop) {
					// There exists a node above this one, so this one might have connections to it
					int absY = fineY;
					for (int x= 0; x < CoarseNode.Stride; x++) {
						int absX = x + fineX;

						if (HasEntry(absX, absY) && HasEntry(absX, absY - 1)) {
							// Generate paths within the node that go to this tile
							Recalculate_GeneratePathfinding_GeneratePaths(node, absX, absY, fineX, fineY, ConnectionDirection.Up);
						}
					}
				}

				if (coarseX < coarseRight) {
					// There exists a node to the right of this one, so this one might have connections to it
					int absX = fineX + CoarseNode.Stride - 1;
					for (int y = 0; y < CoarseNode.Stride; y++) {
						int absY = y + fineY;

						if (HasEntry(absX, absY) && HasEntry(absX - 1, absY)) {
							// Generate paths within the node that go to this tile
							Recalculate_GeneratePathfinding_GeneratePaths(node, absX, absY, fineX, fineY, ConnectionDirection.Left);
						}
					}
				}

				if (coarseY < coarseBottom) {
					// There exists a node below this one, so this one might have connections to it
					int absY = fineY;
					for (int x= 0; x < CoarseNode.Stride; x++) {
						int absX = x + fineX;

						if (HasEntry(absX, absY) && HasEntry(absX, absY + 1)) {
							// Generate paths within the node that go to this tile
							Recalculate_GeneratePathfinding_GeneratePaths(node, absX, absY, fineX, fineY, ConnectionDirection.Down);
						}
					}
				}
			}
		}

		private void Recalculate_GeneratePathfinding_GeneratePaths(CoarseNode node, int x, int y, int nodeX, int nodeY, ConnectionDirection direction) {
			Point16 end = new Point16(x, y);

			CoarseNodeThresholdTile threshold = new CoarseNodeThresholdTile(end, direction);

			List<CoarseNodePathHeuristic> pathList = new();

			foreach (Point16 start in Recalculate_GeneratePathfinding_GeneratePaths_GetValidSources(nodeX, nodeY, direction)) {
				// Threshold should not pathfind to itself
				if (start == end)
					continue;

				var path = innerCoarseNodePathfinder.GetPath(start, end);

				// If the path is null, then there isn't a connection with the target threshold and the source threshold
				if (path is not null)
					pathList.Add(new CoarseNodePathHeuristic(path.ToArray()));
			}

			threshold.paths = pathList.ToArray();

			node.thresholds.Add(end, threshold);

			totalCoarsePaths += threshold.paths.Length;
		}

		private IEnumerable<Point16> Recalculate_GeneratePathfinding_GeneratePaths_GetValidSources(int nodeX, int nodeY, ConnectionDirection direction) {
			foreach (var node in Recalculate_GeneratePathfinding_GeneratePaths_GetValidSources_IterateLeftEdge(nodeX, nodeY))
				yield return node;
			foreach (var node in Recalculate_GeneratePathfinding_GeneratePaths_GetValidSources_IterateTopEdge(nodeX, nodeY))
				yield return node;
			foreach (var node in Recalculate_GeneratePathfinding_GeneratePaths_GetValidSources_IterateRightEdge(nodeX, nodeY))
				yield return node;
			foreach (var node in Recalculate_GeneratePathfinding_GeneratePaths_GetValidSources_IterateBottomEdge(nodeX, nodeY))
				yield return node;
		}

		private IEnumerable<Point16> Recalculate_GeneratePathfinding_GeneratePaths_GetValidSources_IterateLeftEdge(int nodeX, int nodeY) {
			int targetX = nodeX - 1;

			for (int y = 0; y < CoarseNode.Stride; y++) {
				Point16 possible = new Point16(targetX, nodeY + y);

				if (HasEntry(possible))
					yield return possible;
			}
		}

		private IEnumerable<Point16> Recalculate_GeneratePathfinding_GeneratePaths_GetValidSources_IterateTopEdge(int nodeX, int nodeY) {
			int targetY = nodeY - 1;

			for (int x = 0; x < CoarseNode.Stride; x++) {
				Point16 possible = new Point16(nodeX + x, targetY);

				if (HasEntry(possible))
					yield return possible;
			}
		}

		private IEnumerable<Point16> Recalculate_GeneratePathfinding_GeneratePaths_GetValidSources_IterateRightEdge(int nodeX, int nodeY) {
			int targetX = nodeX + CoarseNode.Stride;

			for (int y = 0; y < CoarseNode.Stride; y++) {
				Point16 possible = new Point16(targetX, nodeY + y);

				if (HasEntry(possible))
					yield return possible;
			}
		}

		private IEnumerable<Point16> Recalculate_GeneratePathfinding_GeneratePaths_GetValidSources_IterateBottomEdge(int nodeX, int nodeY) {
			int targetY = nodeY + CoarseNode.Stride;

			for (int x = 0; x < CoarseNode.Stride; x++) {
				Point16 possible = new Point16(nodeX + x, targetY);

				if (HasEntry(possible))
					yield return possible;
			}
		}
		#endregion

		#region A* Methods
		private static CoarseNodeEntry CreatePathfindingEntry(Point16 location, Point16 headingFrom) {
			return new CoarseNodeEntry(location) {
				TravelTime = TileLoader.GetTile(Main.tile[location.X, location.Y].TileType) is IItemTransportTile transport ? transport.TransportTime : double.PositiveInfinity
			};
		}

		private bool HasPathfindingEntry(Point16 location) {
			Tile tile = Main.tile[location.X, location.Y];

			return tile.HasTile && HasEntry(location);
		}

		private bool CanContinuePath(Point16 from, Point16 to) {
			// TODO: pump direction blocking
			return true;
		}
		#endregion

		public List<Point16> GeneratePath(Point16 start, Point16 end) {
			if (!HasEntry(start) || !HasEntry(end))
				return null;

			if (start == end)
				return new List<Point16>() { end };

			Point16 coarseness = new Point16(CoarseNode.Stride);
			Point16 startCoarse = start / coarseness;
			Point16 endCoarse = end / coarseness;

			// If the start end end coarse nodes are the same, then just perform A* pathfinding within the node since it would still be fast
			if (startCoarse == endCoarse)
				return innerCoarseNodePathfinder.GetPath(start, end);

			// Sanity check
			if (!coarsePath.TryGetValue(startCoarse, out CoarseNode startNode))
				return null;

			PriorityQueue<CoarsePathBuilder> paths = new PriorityQueue<CoarsePathBuilder>(totalCoarsePaths, CoarsePathBuilderComparer.Instance);

			// Get a path from the starting tile to each threshold tile in this coarse node
			foreach (var location in startNode.thresholds.Keys) {
				var path = innerCoarseNodePathfinder.GetPath(start, location);

				// The starting tile could pathfind to the threshold
				if (path is not null)
					paths.Push(new CoarsePathBuilder(path, end));
			}

			// Keep generating paths until they reach the target or run out of tiles to pathfind
			List<CoarsePathBuilder> completedPaths = new();
			double quickestKnownTime = double.PositiveInfinity;

			while (paths.Count > 0) {
				CoarsePathBuilder check = paths.Top;
				Point16 pathEnd = check.path[^1];

				// The path is taking longer than the shortest known path.  Remove it since it wouldn't be used anyway
				if (check.travelTime > quickestKnownTime) {
					paths.Pop();
					continue;
				}

				if (pathEnd == end) {
					// Path has completed.  Remove it from the queue
					if (check.travelTime < quickestKnownTime)
						quickestKnownTime = check.travelTime;

					completedPaths.Add(check);
					paths.Pop();
					continue;
				}

				// Ensure that the current end of the path is actually a threshold
				if (!TryGetThresholdTile(pathEnd, out CoarseNodeThresholdTile threshold)) {
					// Path wasn't valid
					paths.Pop();
					continue;
				}

				// Next threshold must exist for pathfinding to continue
				if (!TryFindNextThresholdTile(threshold, out CoarseNodeThresholdTile nextThreshold)) {
					// Path might be invalid or at the final coarse node
					Point16 possible = GetNextPossibleThresholdLocation(threshold);
					Point16 coarse = possible / coarseness;

					if (coarse == endCoarse && HasEntry(possible)) {
						// The path might exist
						var path = innerCoarseNodePathfinder.GetPath(possible, end);

						if (path is not null) {
							check.Append(new CoarseNodePathHeuristic(path.ToArray()));
							continue;
						}
					}

					// Path wasn't valid
					paths.Pop();
					continue;
				}

				var builders = CoarsePathBuilder.Append(check, nextThreshold);

				foreach (var builder in builders) {
					if (TryGetThresholdTile(builder.path[^1], out CoarseNodeThresholdTile endThreshold))
						builder.seenThresholds.Add(endThreshold);
				}

				foreach (var builder in builders.Skip(1))
					paths.Push(builder);
			}

			// Get the quickest path, or null if none exist
			if (completedPaths.Count == 0)
				return null;

			return completedPaths.MinBy(b => b.travelTime).path;
		}

		#region Coarse Pathfinding Helpers
		private bool TryGetThresholdTile(Point16 location, out CoarseNodeThresholdTile threshold) {
			threshold = default;
			Point16 coarse = location / new Point16(CoarseNode.Stride);

			if (!HasEntry(location))
				return false;

			return coarsePath.TryGetValue(coarse, out CoarseNode node) && node.thresholds.TryGetValue(location, out threshold);
		}

		private bool TryFindNextThresholdTile(CoarseNodeThresholdTile threshold, out CoarseNodeThresholdTile nextThreshold) {
			Point16 next = GetNextPossibleThresholdLocation(threshold);

			return TryGetThresholdTile(next, out nextThreshold);
		}

		private static Point16 GetNextPossibleThresholdLocation(CoarseNodeThresholdTile threshold) {
			Point16 offset = threshold.edge switch {
				ConnectionDirection.Left => new Point16(-1, 0),
				ConnectionDirection.Up => new Point16(0, -1),
				ConnectionDirection.Right => new Point16(1, 0),
				ConnectionDirection.Down => new Point16(0, 1),
				_ => throw new ArgumentException("Threshold had an invalid edge value: " + threshold.edge)
			};

			return threshold.location + offset;
		}
		#endregion

		public bool HasEntry(Point16 location) => nodes.ContainsKey(location);

		public bool HasEntry(int x, int y) => nodes.ContainsKey(new Point16(x, y));

		public bool TryGetEntry(Point16 location, out NetworkInstanceNode result) {
			if (nodes.TryGetValue(location, out NetworkInstanceNode value)) {
				result = value;
				return true;
			}

			result = default;
			return false;
		}

		public bool HasKnownJunction(Point16 location) => foundJunctions.Contains(location);

		#region Helpers
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

				Tile check = Main.tile[x + dirX, y + dirY];

				// If it's a junction, add the "next" tile that it should redirect to based on this tile's location
				if (TileLoader.GetTile(check.TileType) is NetworkJunction)
					CheckTile_FindJunctionOppositeTile(pos.X, pos.Y, dirX, dirY);
			}
		}

		private static readonly (int offsetX, int offsetY)[,] junctionDirectionRedirect = new (int, int)[3, 4] {
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

			queue.Enqueue(new Point16(x + offsetX, y + offsetY));
		}

		internal bool IsValidTile(int x, int y) {
			if (!WorldGen.InWorld(x, y))
				return false;

			Tile tile = Main.tile[x, y];
			return tile.HasTile && (tile.Get<NetworkInfo>().Type & Filter) != 0;
		}
		#endregion

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
