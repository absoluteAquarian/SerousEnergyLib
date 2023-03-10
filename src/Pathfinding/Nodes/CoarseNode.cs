using System.Collections.Generic;
using Terraria.DataStructures;

namespace SerousEnergyLib.Pathfinding.Nodes {
	/// <summary>
	/// A node representing a 10x10 tile area in the world
	/// </summary>
	public sealed class CoarseNode {
		/// <summary>
		/// How large each coarse square of tiles is in one dimension
		/// </summary>
		public const int Stride = 10;

		#pragma warning disable CS1591
		public static readonly Point16 Coarseness = new Point16(Stride);

		internal readonly Dictionary<Point16, CoarseNodeThresholdTile> thresholds = new();
	}
}
