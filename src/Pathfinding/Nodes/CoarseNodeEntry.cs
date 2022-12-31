using Terraria.DataStructures;

namespace SerousEnergyLib.Pathfinding.Nodes {
	internal struct CoarseNodeEntry : IAStarEntry {
		public readonly Point16 location;

		Point16 IAStarEntry.Location => location;

		public double TravelTime { get; set; }

		public double Heuristic => TileDistance + TravelTime;

		public int TileDistance { get; set; }

		IAStarEntry IAStarEntry.Parent { get; set; }

		public CoarseNodeEntry(Point16 location) {
			this.location = location;
		}

		public override bool Equals(object obj) => obj is CoarseNodeEntry entry && location == entry.location;

		public override int GetHashCode() => location.GetHashCode();

		public static bool operator ==(CoarseNodeEntry left, CoarseNodeEntry right) => left.Equals(right);

		public static bool operator !=(CoarseNodeEntry left, CoarseNodeEntry right) => !(left == right);

		public override string ToString() => $"Heuristic: {Heuristic}, Location: (X: {location.X}, Y: {location.Y})";
	}
}
