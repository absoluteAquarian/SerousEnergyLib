using System;
using System.Collections.Generic;
using Terraria.DataStructures;

namespace SerousEnergyLib.Pathfinding.Nodes {
	internal struct CoarseNodeEntry {
		public readonly Point16 location;

		public double travelTime;
		public int tileDistance;

		public double Heuristic => tileDistance + travelTime;

		public CoarseNodeEntry(Point16 location) {
			this.location = location;
			travelTime = 0;
			tileDistance = 0;
		}

		public void SetDistance(Point16 target) {
			// How many tiles need to be iterated over to reach the target
			tileDistance = Math.Abs(target.X - location.X) + Math.Abs(target.Y - location.Y);
		}

		public override bool Equals(object obj) => obj is CoarseNodeEntry entry && location == entry.location;

		public override int GetHashCode() => location.GetHashCode();

		public static bool operator ==(CoarseNodeEntry left, CoarseNodeEntry right) => left.Equals(right);

		public static bool operator !=(CoarseNodeEntry left, CoarseNodeEntry right) => !(left == right);

		public override string ToString() => $"Heuristic: {Heuristic}, Location: (X: {location.X}, Y: {location.Y})";
	}

	internal class EntryComparer : IComparer<CoarseNodeEntry>{
		public static readonly EntryComparer Instance = new EntryComparer();

		public int Compare(CoarseNodeEntry x, CoarseNodeEntry y) => x.Heuristic.CompareTo(y.Heuristic);
	}
}
