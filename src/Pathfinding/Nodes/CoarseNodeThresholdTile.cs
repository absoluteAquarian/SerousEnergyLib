using SerousEnergyLib.TileData;
using Terraria.DataStructures;

namespace SerousEnergyLib.Pathfinding.Nodes {
	public struct CoarseNodeThresholdTile {
		public readonly Point16 location;
		public readonly ConnectionDirection edge;
		public CoarseNodePathHeuristic[] paths;

		public CoarseNodeThresholdTile(Point16 location, ConnectionDirection edge) {
			this.location = location;
			this.edge = edge;
		}

		public override bool Equals(object obj) => obj is CoarseNodeThresholdTile tile && location == tile.location;

		public override int GetHashCode() => location.GetHashCode();

		public static bool operator ==(CoarseNodeThresholdTile left, CoarseNodeThresholdTile right) => left.Equals(right);

		public static bool operator !=(CoarseNodeThresholdTile left, CoarseNodeThresholdTile right) => !(left == right);
	}
}
