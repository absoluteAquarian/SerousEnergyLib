using Terraria.DataStructures;

namespace SerousEnergyLib.Pathfinding.Nodes {
	public struct CoarseNodePathHeuristic {
		public readonly Point16[] path;
		public readonly float heuristic;

		public Point16 Start => path?[0] ?? Point16.Zero;

		public Point16 End => path?[^1] ?? Point16.Zero;

		public static bool CalculatePath(Point16 start, Point16 end, out CoarseNodePathHeuristic node) {
			PriorityQueue<CoarseNodeEntry> activeMaze = new();
		}
	}
}
