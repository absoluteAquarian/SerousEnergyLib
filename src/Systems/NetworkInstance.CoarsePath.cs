using SerousEnergyLib.Pathfinding.Nodes;
using SerousEnergyLib.TileData;
using System.Collections.Generic;
using Terraria.DataStructures;

namespace SerousEnergyLib.Systems {
	partial class NetworkInstance {
		/// <summary>
		/// Updates the paths within a coarse node at <paramref name="coarseLocation"/>
		/// </summary>
		/// <param name="coarseLocation">The "coarse location" for the node to update.  Nodes take up square areas of length <see cref="CoarseNode.Stride"/></param>
		public void UpdateCoarseNode(Point16 coarseLocation) {
			if (!coarsePath.TryGetValue(coarseLocation, out CoarseNode node))
				return;

			if (node.thresholds.Count > 0)
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

		private void CheckCoarseNodeHorizontalEdge(CoarseNode node, int fineX, int fineY, ConnectionDirection direction) {
			int absY = direction == ConnectionDirection.Up ? fineY : fineY + CoarseNode.Stride - 1;
			int offset = direction == ConnectionDirection.Up ? -1 : 1;

			for (int x = 0; x < CoarseNode.Stride; x++) {
				int absX = x + fineX;

				if (HasEntry(absX, absY) && HasEntry(absX, absY + offset)) {
					// Generate paths within the node that go to this tile
					GenerateThresholdPaths(node, absX, absY, fineX, fineY, direction);
				}
			}
		}

		private void CheckCoarseNodeVerticalEdge(CoarseNode node, int fineX, int fineY, ConnectionDirection direction) {
			int absX = direction == ConnectionDirection.Left ? fineX : fineX + CoarseNode.Stride - 1;
			int offset = direction == ConnectionDirection.Left ? -1 : 1;

			for (int y = 0; y < CoarseNode.Stride; y++) {
				int absY = y + fineY;

				if (HasEntry(absX, absY) && HasEntry(absX + offset, absY)) {
					// Generate paths within the node that go to this tile
					GenerateThresholdPaths(node, absX, absY, fineX, fineY, direction);
				}
			}
		}

		private void GenerateThresholdPaths(CoarseNode node, int x, int y, int nodeX, int nodeY, ConnectionDirection direction) {
			Point16 start = new Point16(x, y);
			Point16 pathfindingDirection = direction switch {
				ConnectionDirection.Left => new Point16(-1, 0),
				ConnectionDirection.Up => new Point16(0, -1),
				ConnectionDirection.Right => new Point16(1, 0),
				ConnectionDirection.Down => new Point16(0, 1),
				_ => Point16.NegativeOne
			};

			CoarseNodeThresholdTile threshold = new CoarseNodeThresholdTile(start, direction);

			List<CoarseNodePathHeuristic> pathList = new();

			foreach (Point16 adjacentNodeThreshold in GetCoarseNodeValidThresholds(nodeX, nodeY)) {
				// In case the threshold node in this coarse node is a junction, set the initial direction
				PathfindingStartDirection = pathfindingDirection;

				var path = innerCoarseNodePathfinder.GetPath(start, adjacentNodeThreshold);

				// If the path is null, then there isn't a connection with the target threshold and the source threshold
				if (path is not null) {
					// Threshold should not pathfind to itself
					if (path.FindLastIndex(p => p == start) > 0)
						continue;

					pathList.Add(new CoarseNodePathHeuristic(path.ToArray()));
				}
			}

			threshold.paths = pathList.ToArray();

			// Future-proof against possible memory leaking
			node.thresholds[start] = threshold;

			totalCoarsePaths += threshold.paths.Length - 1;
		}

		private IEnumerable<Point16> GetCoarseNodeValidThresholds(int nodeX, int nodeY) {
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

				if (HasEntry(targetX + 1, nodeY + y) && HasEntry(possible))
					yield return possible;
			}
		}

		private IEnumerable<Point16> GetCoarseNodeValidThresholds_IterateTopEdge(int nodeX, int nodeY) {
			int targetY = nodeY - 1;

			for (int x = 0; x < CoarseNode.Stride; x++) {
				Point16 possible = new Point16(nodeX + x, targetY);

				if (HasEntry(nodeX + x, targetY + 1) && HasEntry(possible))
					yield return possible;
			}
		}

		private IEnumerable<Point16> GetCoarseNodeValidThresholds_IterateRightEdge(int nodeX, int nodeY) {
			int targetX = nodeX + CoarseNode.Stride;

			for (int y = 0; y < CoarseNode.Stride; y++) {
				Point16 possible = new Point16(targetX, nodeY + y);

				if (HasEntry(targetX - 1, nodeY + y) && HasEntry(possible))
					yield return possible;
			}
		}

		private IEnumerable<Point16> GetCoarseNodeValidThresholds_IterateBottomEdge(int nodeX, int nodeY) {
			int targetY = nodeY + CoarseNode.Stride;

			for (int x = 0; x < CoarseNode.Stride; x++) {
				Point16 possible = new Point16(nodeX + x, targetY);

				if (HasEntry(nodeX + x, targetY - 1) && HasEntry(possible))
					yield return possible;
			}
		}
	}
}
