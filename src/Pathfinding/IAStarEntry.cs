using System;
using System.Collections.Generic;
using Terraria.DataStructures;

namespace SerousEnergyLib.Pathfinding {
	public interface IAStarEntry {
		Point16 Location { get; }

		IAStarEntry Parent { get; set; }

		double Heuristic { get; }

		int TileDistance { get; set; }

		double TravelTime { get; set; }
	}

	internal class AStarEntryComparer<T> : IComparer<T> where T : IAStarEntry {
		public static readonly AStarEntryComparer<T> Instance = new AStarEntryComparer<T>();

		public int Compare(T x, T y) => x.Heuristic.CompareTo(y.Heuristic);
	}
}
