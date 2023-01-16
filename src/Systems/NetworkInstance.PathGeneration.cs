using SerousEnergyLib.Pathfinding.Nodes;
using SerousEnergyLib.Pathfinding;
using SerousEnergyLib.TileData;
using System.Collections.Generic;
using System;
using Terraria.DataStructures;
using System.Linq;

namespace SerousEnergyLib.Systems {
	partial class NetworkInstance {
		/// <summary>
		/// Attempts to generate a path from <paramref name="start"/> to <paramref name="end"/>
		/// </summary>
		/// <param name="start">The starting tile location</param>
		/// <param name="end">The final tile location</param>
		/// <param name="travelTime">The "travel time" for the path if one was found</param>
		/// <returns>A list of tile coordinates for pathfinding, or <see langword="null"/> if no path was found</returns>
		/// <exception cref="ObjectDisposedException"/>
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

				if (innerPath is not null) {
					travelTime = new CoarseNodePathHeuristic(innerPath.ToArray()).travelTime;
					return innerPath;
				}

				return null;
			}

			// Sanity check
			if (!coarsePath.TryGetValue(startCoarse, out CoarseNode startNode))
				return null;

			PriorityQueue<CoarsePathBuilder> paths = new PriorityQueue<CoarsePathBuilder>(totalCoarsePaths, CoarsePathBuilderComparer.Instance);

			// Get a path from the starting tile to each threshold tile in this coarse node
			foreach (var location in startNode.thresholds.Keys) {
				var dir = PathfindingStartDirection;

				var path = innerCoarseNodePathfinder.GetPath(start, location);

				// Restore the startind direction so that all initial builders can use it
				PathfindingStartDirection = dir;

				// The starting tile could pathfind to the threshold
				if (path is not null) {
					var builder = new CoarsePathBuilder(path, end);
					builder.seenThresholds.Add(startNode.thresholds[location]);
					paths.Push(builder);
				}
			}

			PathfindingStartDirection = Point16.NegativeOne;

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
				if (check.cannotContinuePath || !TryFindNextThresholdTile(threshold, out CoarseNodeThresholdTile nextThreshold)) {
					// Path might be invalid or at the final coarse node
					Point16 coarse = pathEnd / CoarseNode.Coarseness;

					if (coarse == endCoarse && HasEntry(pathEnd)) {
						// The path might exist
						PathfindingStartDirection = check.headingDirection switch {
							ConnectionDirection.Left => new Point16(-1, 0),
							ConnectionDirection.Up => new Point16(0, -1),
							ConnectionDirection.Right => new Point16(1, 0),
							ConnectionDirection.Down => new Point16(0, 1),
							_ => Point16.NegativeOne
						};

						var path = innerCoarseNodePathfinder.GetPath(pathEnd, end);

						if (path is not null) {
							check.Append(new CoarseNodePathHeuristic(path.ToArray()));
							continue;
						}
					}

					// Path wasn't valid
					paths.Pop();
					continue;
				}

				var builders = CoarsePathBuilder.Append(check, nextThreshold, this);

				if (builders is not null) {
					// Remove the old instance
					paths.Pop();

					foreach (var builder in builders) {
						if (TryGetThresholdTile(builder.path[^1], out CoarseNodeThresholdTile endThreshold))
							builder.seenThresholds.Add(endThreshold);
					}

					foreach (var builder in builders)
						paths.Push(builder);
				}
			}

			// Get the quickest path, or null if none exist
			if (completedPaths.Count == 0)
				return null;

			var builtPath = completedPaths.MinBy(b => b.travelTime);
			travelTime = builtPath.travelTime;
			return builtPath.path;
		}

		internal bool TryGetThresholdTile(Point16 location, out CoarseNodeThresholdTile threshold) {
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
	}
}
