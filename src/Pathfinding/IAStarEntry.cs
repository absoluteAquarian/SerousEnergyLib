using System.Collections.Generic;
using Terraria.DataStructures;

namespace SerousEnergyLib.Pathfinding {
	/// <summary>
	/// An interface representing an entry in an <see cref="AStar{TEntry}"/> object
	/// </summary>
	public interface IAStarEntry {
		/// <summary>
		/// The tile location of the entry
		/// </summary>
		Point16 Location { get; }

		/// <summary>
		/// The entry that this entry was walked from
		/// </summary>
		IAStarEntry Parent { get; set; }

		/// <summary>
		/// The heuristic of the entry
		/// </summary>
		double Heuristic { get; }

		/// <summary>
		/// How far away this entry is from the target
		/// </summary>
		int TileDistance { get; set; }

		/// <summary>
		/// The "travel time" for the entry from the start of the path
		/// </summary>
		double TravelTime { get; set; }
	}

	internal class AStarEntryComparer<T> : IComparer<T> where T : IAStarEntry {
		public static readonly AStarEntryComparer<T> Instance = new AStarEntryComparer<T>();

		public int Compare(T x, T y) => x.Heuristic.CompareTo(y.Heuristic);
	}
}
