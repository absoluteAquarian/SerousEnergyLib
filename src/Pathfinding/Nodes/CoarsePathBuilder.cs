using SerousEnergyLib.TileData;
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

		private ConnectionDirection headingDirection;
		internal bool cannotContinuePath;

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

			foreach (var node in copy.seenThresholds)
				seenThresholds.Add(node);
		}

		public static List<CoarsePathBuilder> Append(CoarsePathBuilder orig, CoarseNodeThresholdTile threshold) {
			if (!orig.seenThresholds.Add(threshold)) {
				// Path has already seen this threshold
				orig.cannotContinuePath = true;
				return null;
			}

			ConnectionDirection opposite = threshold.edge switch {
				ConnectionDirection.Left => ConnectionDirection.Right,
				ConnectionDirection.Up => ConnectionDirection.Down,
				ConnectionDirection.Right => ConnectionDirection.Left,
				ConnectionDirection.Down => ConnectionDirection.Up,
				_ => ConnectionDirection.None
			};

			if (opposite == ConnectionDirection.None) {
				orig.cannotContinuePath = true;
				return null;
			}

			List<CoarsePathBuilder> builders = new();

			int numPaths = 0;
			foreach (var path in threshold.paths) {
				CoarsePathBuilder builder = new(orig);
				builder.headingDirection = opposite;
				
				builder.Append(path);

				builder.seenThresholds.Add(threshold);
				
				builders.Add(builder);
				numPaths++;
			}

			return builders;
		}

		public void Append(CoarseNodePathHeuristic heuristic) {
			if (heuristic.path.Length <= 1) {
				cannotContinuePath = true;
				return;
			}

			Point16[] newPath = heuristic.path;
			double travel = heuristic.travelTime;

			if (path.Count > 0) {
				Point16 diff = heuristic.path[1] - heuristic.path[0];

				// Prohibit moving backwards
				Point16 reversedHeading = headingDirection switch {
					ConnectionDirection.Left => new Point16(1, 0),
					ConnectionDirection.Up => new Point16(0, 1),
					ConnectionDirection.Right => new Point16(-1, 0),
					ConnectionDirection.Down => new Point16(0, -1),
					_ => Point16.Zero
				};

				if (reversedHeading == Point16.Zero || diff == reversedHeading) {
					cannotContinuePath = true;
					
					// Add the new threshold's node so that the "finalize path" logic is correct
					Point16 final = newPath[0];

					path.Add(final);
					travelTime += new CoarseNodePathHeuristic(new Point16[] { final }).travelTime;

					tileDistance = Math.Abs(target.X - final.X) + Math.Abs(target.Y - final.Y);
					return;
				}

				// First entry will be a duplicate
				newPath = newPath[1..];

				travel = new CoarseNodePathHeuristic(newPath).travelTime;
			}

			path.AddRange(newPath);
			travelTime += travel;

			Point16 end = newPath[^1];
			tileDistance = Math.Abs(target.X - end.X) + Math.Abs(target.Y - end.Y);
		}
	}

	internal class CoarsePathBuilderComparer : IComparer<CoarsePathBuilder> {
		public static readonly CoarsePathBuilderComparer Instance = new CoarsePathBuilderComparer();

		public int Compare(CoarsePathBuilder x, CoarsePathBuilder y) => x.Heuristic.CompareTo(y.Heuristic);
	}
}
