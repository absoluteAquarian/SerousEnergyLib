using SerousEnergyLib.API;
using SerousEnergyLib.Pathfinding;
using SerousEnergyLib.Pathfinding.Nodes;
using SerousEnergyLib.Systems.Networks;
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
	public class NetworkInstance : IDisposable {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				nextID = 0;
			}
		}

		public NetworkType Filter { get; private set; }

		private Dictionary<Point16, CoarseNode> coarsePath = new();
		private Dictionary<Point16, NetworkInstanceNode> nodes = new();
		private HashSet<Point16> foundJunctions = new();
		private int totalCoarsePaths = 0;
		private int coarseLeft = -1, coarseTop = -1, coarseRight = -1, coarseBottom = -1;

		public int ID { get; private set; }

		private AStar<CoarseNodeEntry> innerCoarseNodePathfinder;
		internal static bool ignoreTravelTimeWhenPathfinding;

		public bool IsEmpty => nodes.Count == 0;

		public int EntryCount => nodes.Count;

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

		private readonly Queue<Point16> queue = new();

		private bool recalculating;
		internal bool delayCoarsePathCalculationFromCopy;

		internal void CopyFrom(NetworkInstance other) {
			if (disposed)
				throw new ObjectDisposedException("this");

			if (Filter != other.Filter)
				throw new ArgumentException("Network instances had mismatched filters", nameof(other));

			foreach (var (loc, node) in other.nodes) {
				nodes.Add(loc, node);

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
		}

		public bool HasEntry(Point16 location) {
			if (disposed)
				throw new ObjectDisposedException("this");

			return nodes.ContainsKey(location);
		}

		public bool HasEntry(int x, int y) {
			if (disposed)
				throw new ObjectDisposedException("this");

			return nodes.ContainsKey(new Point16(x, y));
		}

		public bool TryGetEntry(Point16 location, out NetworkInstanceNode result) {
			if (disposed)
				throw new ObjectDisposedException("this");

			if (nodes.TryGetValue(location, out NetworkInstanceNode value)) {
				result = value;
				return true;
			}

			result = default;
			return false;
		}

		public bool HasKnownJunction(Point16 location) {
			if (disposed)
				throw new ObjectDisposedException("this");

			return foundJunctions.Contains(location);
		}

		public bool HasPump(Point16 location, out PumpDirection direction) {
			if (disposed)
				throw new ObjectDisposedException("this");

			direction = PumpDirection.Left;

			if (!HasEntry(location))
				return false;

			Tile tile = Main.tile[location.X, location.Y];
			NetworkInfo info = tile.Get<NetworkInfo>();
			NetworkTaggedInfo tags = tile.Get<NetworkTaggedInfo>();

			bool pump = info.IsPump;

			if (pump)
				direction = tags.PumpDirection;

			return pump;
		}

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

			OnEntryAdded(location);

			// Preemptively add a coarse node entry
			Point16 coarse = new Point16(x, y) / CoarseNode.Coarseness;
			if (!coarsePath.ContainsKey(coarse))
				coarsePath[coarse] = new CoarseNode();

			if (!recalculating) {
				// Refresh the coarse node
				UpdateCoarseNode(coarse);

				// Update the adjacent nodes
				for (int i = 0; i < node.adjacent.Length; i++) {
					Point16 adj = node.adjacent[i];

					if (nodes.TryGetValue(adj, out var adjNode)) {
						node = CreateNetworkNode(adj.X, adj.Y, ref adjacent, ref nextIndex);
						nodes[adj] = node;
					}
				}
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
				networks.Add(netLeft);

				origIDUsed = true;
				netLeft.ID = orig.ID;
			}
			
			// Generate the "up" network
			NetworkInstance netUp = null;
			if (orig.HasEntry(right) && !(netLeft?.HasEntry(up) ?? false)) {
				netUp = CloneNetwork(orig, leftUpConnected, left, upRightConnected, right, upDownConnected, down);
				networks.Add(netUp);

				if (!origIDUsed) {
					origIDUsed = true;
					netUp.ID = orig.ID;
				} else
					netUp.ReserveNextID();
			}

			// Generate the "right" network
			NetworkInstance netRight = null;
			if (orig.HasEntry(right) && !(netLeft?.HasEntry(right) ?? false) && !(netUp?.HasEntry(right) ?? false)) {
				netRight = CloneNetwork(orig, leftRightConnected, left, upRightConnected, up, rightDownConnected, down);
				networks.Add(netRight);

				if (!origIDUsed) {
					origIDUsed = true;
					netRight.ID = orig.ID;
				} else
					netRight.ReserveNextID();
			}

			// Generate the "down" network
			if (orig.HasEntry(down) && !(netLeft?.HasEntry(down) ?? false) && !(netUp?.HasEntry(down) ?? false) && !(netRight?.HasEntry(down) ?? false)) {
				NetworkInstance netDown = CloneNetwork(orig, leftDownConnected, left, upDownConnected, up, rightDownConnected, right);
				networks.Add(netDown);

				if (!origIDUsed)
					netDown.ID = orig.ID;
				else
					netDown.ReserveNextID();
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

			return net;
		}

		/// <summary>
		/// This method is called after this network was cloned from another network and this network's nodes were updated
		/// </summary>
		protected virtual void OnNetworkCloned() { }

		private static void RemoveUnnecessaryNodes(NetworkInstance net, Point16 start, bool updateCoarseNodes = true) {
			Queue<Point16> queue = new Queue<Point16>();
			queue.Enqueue(start);

			// Remove this node and its adjacent nodes
			while (queue.Count > 0) {
				Point16 pos = queue.Dequeue();

				if (!net.TryGetEntry(pos, out NetworkInstanceNode node))
					continue;

				net.nodes.Remove(pos);
				net.foundJunctions.Remove(pos);

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

			return new NetworkInstanceNode(location, nextIndex == 0 ? Array.Empty<Point16>() : adjacent[..(nextIndex - 1)].ToArray());
		}

		/// <summary>
		/// Completely recalculates the paths for this network instance.<br/>
		/// Calling this method is <b>NOT</b> recommended when adding/removing entries from the network.  Use the appropriate methods for those instead.
		/// </summary>
		/// <param name="start">The first tile to process when recalculating the paths</param>
		public void Recalculate(Point16 start) {
			if (disposed)
				throw new ObjectDisposedException("this");

			Reset();

			if (!IsValidTile(start.X, start.Y))
				return;

			HashSet<Point16> walked = new();

			queue.Clear();
			queue.Enqueue(start);

			int left = 65535;
			int right = -1;
			int top = 65535;
			int bottom = -1;

			recalculating = true;

			while (queue.TryDequeue(out Point16 location)) {
				if (!walked.Add(location))
					continue;

				int x = location.X, y = location.Y;

				AddEntry(location);

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

			coarseLeft = left / CoarseNode.Stride;
			coarseTop = top / CoarseNode.Stride;
			coarseRight = right / CoarseNode.Stride;
			coarseBottom = bottom / CoarseNode.Stride;

			totalCoarsePaths = 1;

			foreach (var coarse in coarsePath.Keys)
				UpdateCoarseNode(coarse);

			OnRecalculate(nodes);

			recalculating = false;
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

		/// <summary>
		/// This method is called after the network has recalculated its paths
		/// </summary>
		/// <param name="nodes">The collection of entries in the network, indexed by tile position</param>
		public virtual void OnRecalculate(IReadOnlyDictionary<Point16, NetworkInstanceNode> nodes) { }

		public void UpdateCoarseNode(Point16 coarseLocation) {
			if (!coarsePath.TryGetValue(coarseLocation, out CoarseNode node))
				return;

			totalCoarsePaths -= node.thresholds.Count - 1;
			node.thresholds.Clear();

			int coarseX = coarseLocation.X;
			int fineX = coarseX * CoarseNode.Stride;
			int coarseY = coarseLocation.Y;
			int fineY = coarseY * CoarseNode.Stride;

			if (coarseX > coarseLeft) {
				// There exists a node to the left of this one, so this one might have connections to it
				CheckCoarseNodeVerticalEdge(node, fineX, fineY, ConnectionDirection.Left);
			}

			if (coarseY > coarseTop) {
				// There exists a node above this one, so this one might have connections to it
				CheckCoarseNodeHorizontalEdge(node, fineX, fineY, ConnectionDirection.Up);
			}

			if (coarseX < coarseRight) {
				// There exists a node to the right of this one, so this one might have connections to it
				CheckCoarseNodeVerticalEdge(node, fineX, fineY, ConnectionDirection.Right);
			}

			if (coarseY < coarseBottom) {
				// There exists a node below this one, so this one might have connections to it
				CheckCoarseNodeHorizontalEdge(node, fineX, fineY, ConnectionDirection.Down);
			}
		}

		#region Pathfinding Recalculation Helpers
		private void CheckCoarseNodeHorizontalEdge(CoarseNode node, int fineX, int fineY, ConnectionDirection direction) {
			int absY = direction == ConnectionDirection.Up ? fineY : fineY + CoarseNode.Stride - 1;

			for (int x = 0; x < CoarseNode.Stride; x++) {
				int absX = x + fineX;

				if (HasEntry(absX, absY) && HasEntry(absX, absY + 1)) {
					// Generate paths within the node that go to this tile
					GenerateThresholdPaths(node, absX, absY, fineX, fineY, ConnectionDirection.Down);
				}
			}
		}

		private void CheckCoarseNodeVerticalEdge(CoarseNode node, int fineX, int fineY, ConnectionDirection direction) {
			int absX = direction == ConnectionDirection.Left ? fineX : fineX + CoarseNode.Stride - 1;
			for (int y = 0; y < CoarseNode.Stride; y++) {
				int absY = y + fineY;

				if (HasEntry(absX, absY) && HasEntry(absX - 1, absY)) {
					// Generate paths within the node that go to this tile
					GenerateThresholdPaths(node, absX, absY, fineX, fineY, ConnectionDirection.Left);
				}
			}
		}

		private void GenerateThresholdPaths(CoarseNode node, int x, int y, int nodeX, int nodeY, ConnectionDirection direction) {
			Point16 end = new Point16(x, y);

			CoarseNodeThresholdTile threshold = new CoarseNodeThresholdTile(end, direction);

			List<CoarseNodePathHeuristic> pathList = new();

			foreach (Point16 start in GetCoarseNodeValidThresholds(nodeX, nodeY, direction)) {
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

			totalCoarsePaths += threshold.paths.Length - 1;
		}

		private IEnumerable<Point16> GetCoarseNodeValidThresholds(int nodeX, int nodeY, ConnectionDirection direction) {
			foreach (var node in GetCoarseNodeValidThresholds_IterateLeftEdge(nodeX, nodeY))
				yield return node;
			foreach (var node in GetCoarseNodeValidThresholds_IterateTopEdge(nodeX, nodeY))
				yield return node;
			foreach (var node in GetCoarseNodeValidThresholds_IterateRightEdge(nodeX, nodeY))
				yield return node;
			foreach (var node in GetCoarseNodeValidThresholds_IterateBottomEdge(nodeX, nodeY))
				yield return node;
		}

		private IEnumerable<Point16> GetCoarseNodeValidThresholds_IterateLeftEdge(int nodeX, int nodeY) {
			int targetX = nodeX - 1;

			for (int y = 0; y < CoarseNode.Stride; y++) {
				Point16 possible = new Point16(targetX, nodeY + y);

				if (HasEntry(possible))
					yield return possible;
			}
		}

		private IEnumerable<Point16> GetCoarseNodeValidThresholds_IterateTopEdge(int nodeX, int nodeY) {
			int targetY = nodeY - 1;

			for (int x = 0; x < CoarseNode.Stride; x++) {
				Point16 possible = new Point16(nodeX + x, targetY);

				if (HasEntry(possible))
					yield return possible;
			}
		}

		private IEnumerable<Point16> GetCoarseNodeValidThresholds_IterateRightEdge(int nodeX, int nodeY) {
			int targetX = nodeX + CoarseNode.Stride;

			for (int y = 0; y < CoarseNode.Stride; y++) {
				Point16 possible = new Point16(targetX, nodeY + y);

				if (HasEntry(possible))
					yield return possible;
			}
		}

		private IEnumerable<Point16> GetCoarseNodeValidThresholds_IterateBottomEdge(int nodeX, int nodeY) {
			int targetY = nodeY + CoarseNode.Stride;

			for (int x = 0; x < CoarseNode.Stride; x++) {
				Point16 possible = new Point16(nodeX + x, targetY);

				if (HasEntry(possible))
					yield return possible;
			}
		}
		#endregion

		#region A* Methods
		private CoarseNodeEntry CreatePathfindingEntry(Point16 location, Point16 headingFrom) {
			double time = 0;
			if (!ignoreTravelTimeWhenPathfinding) {
				if (Filter == NetworkType.Items) {
					time = TileLoader.GetTile(Main.tile[location.X, location.Y].TileType) is IItemTransportTile transport
						? transport.TransportTime
						: double.PositiveInfinity;
				}

				// TODO: fluid pathfinding?
			}

			return new CoarseNodeEntry(location) {
				TravelTime = time
			};
		}

		private bool HasPathfindingEntry(Point16 location) {
			Tile tile = Main.tile[location.X, location.Y];

			return tile.HasTile && HasEntry(location);
		}

		private bool CanContinuePath(Point16 from, Point16 to) {
			Tile fromTile = Main.tile[from.X, from.Y];
			Tile toTile = Main.tile[to.X, to.Y];

			NetworkTaggedInfo fromTags = fromTile.Get<NetworkTaggedInfo>();
			NetworkTaggedInfo toTags = toTile.Get<NetworkTaggedInfo>();

			if (!NetworkTaggedInfo.CanMergeColors(fromTags, toTags))
				return false;

			NetworkInfo fromInfo = fromTile.Get<NetworkInfo>();
			NetworkInfo toInfo = toTile.Get<NetworkInfo>();

			// Pumps cannot merge with each other
			if (fromInfo.IsPump && toInfo.IsPump)
				return false;

			if (fromInfo.IsPump && NetworkTaggedInfo.DoesOrientationMatchPumpDirection(to - from, fromTags.PumpDirection))
				return true;

			if (toInfo.IsPump && NetworkTaggedInfo.DoesOrientationMatchPumpDirection(from - to, toTags.PumpDirection))
				return true;

			return true;
		}

		private List<Point16> GetWalkableDirections(Point16 center, Point16 previous) {
			if (!nodes.TryGetValue(center, out var node))
				return new List<Point16>();

			Tile tile = Main.tile[center.X, center.Y];

			if (TileLoader.GetTile(tile.TileType) is NetworkJunction) {
				if (previous == Point16.NegativeOne)
					return new List<Point16>();  // Pathfinding is not permitted to start at a junction

				// Only one valid direction is allowed
				Point16 diff = center - previous;

				// Failsafe
				if ((diff.X == 0 && diff.Y == 0) || (diff.X != 0 && diff.Y != 0))
					return new List<Point16>();

				int mode = tile.TileFrameX / 18;

				if (mode == 0)
					return new List<Point16>() { new Point16(-diff.X, -diff.Y) };  // Left -> Right, Up -> Down
				else if (mode == 1)
					return new List<Point16>() { new Point16(-diff.Y, -diff.X) };  // Left -> Down, Up -> Right
				else if (mode == 2)
					return new List<Point16>() { new Point16(diff.Y, diff.X) };    // Left -> Up, Right -> Down
				else
					return new List<Point16>();  // Failsafe
			}

			// All four directions are allowed
			return node.adjacent.ToList();
		}
		#endregion

		public List<Point16> GeneratePath(Point16 start, Point16 end, out double travelTime) {
			if (disposed)
				throw new ObjectDisposedException("this");

			travelTime = 0;

			if (!HasEntry(start) || !HasEntry(end))
				return null;

			if (start == end)
				return new List<Point16>() { end };

			Point16 startCoarse = start / CoarseNode.Coarseness;
			Point16 endCoarse = end / CoarseNode.Coarseness;

			// If the start end end coarse nodes are the same, then just perform A* pathfinding within the node since it would still be fast
			if (startCoarse == endCoarse) {
				var innerPath = innerCoarseNodePathfinder.GetPath(start, end);
				travelTime = new CoarseNodePathHeuristic(innerPath.ToArray()).travelTime;
				return innerPath;
			}

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
					Point16 coarse = possible / CoarseNode.Coarseness;

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

			var builtPath = completedPaths.MinBy(b => b.travelTime);
			travelTime = builtPath.travelTime;
			return builtPath.path;
		}

		#region Coarse Pathfinding Helpers
		private bool TryGetThresholdTile(Point16 location, out CoarseNodeThresholdTile threshold) {
			threshold = default;
			Point16 coarse = location / CoarseNode.Coarseness;

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

		#region Helpers
		private void CheckTile(int x, int y, int dirX, int dirY, ref Span<Point16> adjacent, ref int nextIndex) {
			// Ignore the "parent" tile
			if (dirX == 0 && dirY == 0)
				return;

			// Ignore ordinal tiles
			if (dirX != 0 && dirY != 0)
				return;

			Point16 pos = new Point16(x + dirX, y + dirY);
			if (IsValidTile(x + dirX, y + dirY) && CanContinuePath(new Point16(x, y), pos)) {
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
		#endregion

		public void SaveData(TagCompound tag) {
			if (disposed)
				throw new ObjectDisposedException("this");

			tag["filter"] = (byte)Filter;

			// Save the "start" of the network so that the logic is forced to recalculate it when loading the world
			if (nodes.Count > 0)
				tag["start"] = nodes.Keys.First();

			TagCompound extra = new();
			SaveExtraData(extra);
			tag["extra"] = extra;
		}

		protected virtual void SaveExtraData(TagCompound tag) { }

		public void LoadData(TagCompound tag) {
			if (disposed)
				throw new ObjectDisposedException("this");

			byte filter = tag.GetByte("filter");

			if (filter != (byte)NetworkType.Items && filter != (byte)NetworkType.Fluids && filter != (byte)NetworkType.Power)
				throw new IOException("Invalid filter number: " + filter);

			Filter = (NetworkType)filter;

			if (tag.ContainsKey("start") && tag["start"] is Point16 start)
				Recalculate(start);

			if (tag.GetCompound("extra") is TagCompound extra)
				LoadExtraData(extra);
		}

		protected virtual void LoadExtraData(TagCompound tag) { }

		internal void SendNetworkData(int toClient = -1) {
			// Packet #1
			var packet = Netcode.GetPacket(NetcodeMessage.SyncNetwork0_ResetNetwork);
			Netcode.WriteNetworkInstanceToPacket(packet, this);
			packet.Send(toClient);

			// Packet #2
			packet = Netcode.GetPacket(NetcodeMessage.SyncNetwork1_Nodes);
			Netcode.WriteNetworkInstanceToPacket(packet, this);

			using (var compression = new CompressionStream()) {
				var writer = compression.writer;

				writer.Write(nodes.Count);

				foreach (var (loc, node) in nodes) {
					writer.Write(loc);
					writer.Write(node);
				}

				compression.WriteToStream(packet);
			}

			packet.Send(toClient);

			// Packet #3
			packet = Netcode.GetPacket(NetcodeMessage.SyncNetwork2_CoarsePath);
			Netcode.WriteNetworkInstanceToPacket(packet, this);

			using (var compression = new CompressionStream()) {
				var writer = compression.writer;

				writer.Write(coarsePath.Count);

				foreach (var (loc, coarse) in coarsePath) {
					writer.Write(loc);
					writer.Write(coarse);
				}

				compression.WriteToStream(packet);
			}

			packet.Send(toClient);

			// Packet #4
			packet = Netcode.GetPacket(NetcodeMessage.SyncNetwork3_CoarseInfo);
			Netcode.WriteNetworkInstanceToPacket(packet, this);
			packet.Write(totalCoarsePaths);
			packet.Write((short)coarseLeft);
			packet.Write((short)coarseTop);
			packet.Write((short)coarseRight);
			packet.Write((short)coarseBottom);
			packet.Send(toClient);

			// Packet #5
			packet = Netcode.GetPacket(NetcodeMessage.SyncNetwork4_Junctions);
			Netcode.WriteNetworkInstanceToPacket(packet, this);

			using (var compression = new CompressionStream()) {
				var writer = compression.writer;

				writer.Write(foundJunctions.Count);

				foreach (var junction in foundJunctions)
					writer.Write(junction);

				compression.WriteToStream(packet);
			}

			packet.Send(toClient);

			// Packet #6
			packet = Netcode.GetPacket(NetcodeMessage.SyncNetwork5_ExtraInfo);
			Netcode.WriteNetworkInstanceToPacket(packet, this);

			using (var compression = new CompressionStream()) {
				var writer = compression.writer;

				SendExtraData(writer);

				compression.WriteToStream(packet);
			}

			packet.Send(toClient);
		}

		internal void ReceiveNetworkData_1_Nodes(BinaryReader reader) {
			// DecompressionStream ctor automatically reads the compressed data and decompresses it
			using var decompression = new DecompressionStream(reader);
			var decompressedReader = decompression.reader;

			int nodeCount = decompressedReader.ReadInt32();

			for (int i = 0; i < nodeCount; i++) {
				Point16 loc = decompressedReader.ReadPoint16();
				NetworkInstanceNode node = decompressedReader.ReadNetworkInstanceNode();

				nodes[loc] = node;
			}
		}

		internal void ReceiveNetworkData_2_CoarsePath(BinaryReader reader) {
			// DecompressionStream ctor automatically reads the compressed data and decompresses it
			using var decompression = new DecompressionStream(reader);
			var decompressedReader = decompression.reader;

			int pathCount = decompressedReader.ReadInt32();

			for (int i = 0; i < pathCount; i++) {
				Point16 loc = decompressedReader.ReadPoint16();
				CoarseNode node = decompressedReader.ReadCoarseNode();

				coarsePath[loc] = node;
			}
		}

		internal void ReceiveNetworkData_3_CoarseInfo(BinaryReader reader) {
			totalCoarsePaths = reader.ReadInt32();
			coarseLeft = reader.ReadInt16();
			coarseTop = reader.ReadInt16();
			coarseRight = reader.ReadInt16();
			coarseBottom = reader.ReadInt16();
		}

		internal void ReceiveNetworkData_4_Junctions(BinaryReader reader) {
			// DecompressionStream ctor automatically reads the compressed data and decompresses it
			using var decompression = new DecompressionStream(reader);
			var decompressedReader = decompression.reader;

			int count = reader.ReadInt32();

			for (int i = 0; i < count; i++) {
				Point16 junction = reader.ReadPoint16();

				foundJunctions.Add(junction);
			}
		}

		internal void ReceiveNetworkData_5_ExtraInfo(BinaryReader reader) {
			// DecompressionStream ctor automatically reads the compressed data and decompresses it
			using var decompression = new DecompressionStream(reader);
			var decompressedReader = decompression.reader;

			ReceiveExtraData(decompressedReader);
		}

		/// <summary>
		/// This method is called when a network is going to be synced to a client
		/// </summary>
		/// <param name="writer">The outgoing data stream</param>
		public virtual void SendExtraData(BinaryWriter writer) { }

		/// <summary>
		/// This method is called when a network sync is being received by a client
		/// </summary>
		/// <param name="reader">The incoming data stream</param>
		public virtual void ReceiveExtraData(BinaryReader reader) { }

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
		}

		protected virtual void DisposeSelf(bool disposing) { }

		~NetworkInstance() => Dispose(false);
		#endregion
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
