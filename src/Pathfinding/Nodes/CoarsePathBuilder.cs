using System;
using System.Collections.Generic;
using Terraria.DataStructures;

namespace SerousEnergyLib.Pathfinding.Nodes {
	internal class CoarsePathBuilder {
		public readonly List<Point16> path;
		public double travelTime;
		public int tileDistance;
		public readonly Point16 target;

		public readonly HashSet<CoarseNodeThresholdTile> seenThresholds = new();

		public double Heuristic => travelTime + tileDistance;

		public CoarsePathBuilder(List<Point16> start, Point16 target) {
			if (start.Count < 1)
				throw new ArgumentException("Starting path was too short");

			this.target = target;

			path = new();

			CoarseNodePathHeuristic heuristic = new CoarseNodePathHeuristic(start.ToArray());
			Append(heuristic);
		}

		private CoarsePathBuilder(CoarsePathBuilder copy) {
			path = new();
			path.AddRange(copy.path);
			travelTime = copy.travelTime;
			tileDistance = copy.tileDistance;
			target = copy.target;
		}

		public static List<CoarsePathBuilder> Append(CoarsePathBuilder orig, CoarseNodeThresholdTile threshold) {
			if (!orig.seenThresholds.Add(threshold)) {
				// Path has already seen this threshold
				return null;
			}

			List<CoarsePathBuilder> builders = new();

			int numPaths = 0;
			foreach (var path in threshold.paths) {
				CoarsePathBuilder builder = numPaths == 0 ? orig : new(orig);
				
				builder.Append(path);
				
				builders.Add(builder);
				numPaths++;
			}

			return builders;
		}

		public void Append(CoarseNodePathHeuristic heuristic) {
			path.AddRange(heuristic.path);
			travelTime += heuristic.travelTime;

			Point16 end = heuristic.path[^1];
			tileDistance = Math.Abs(target.X - end.X) + Math.Abs(target.Y - end.Y);
		}
	}

	internal class CoarsePathBuilderComparer : IComparer<CoarsePathBuilder> {
		public static readonly CoarsePathBuilderComparer Instance = new CoarsePathBuilderComparer();

		public int Compare(CoarsePathBuilder x, CoarsePathBuilder y) => x.Heuristic.CompareTo(y.Heuristic);
	}
}
