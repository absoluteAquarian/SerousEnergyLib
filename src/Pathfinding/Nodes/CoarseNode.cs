namespace SerousEnergyLib.Pathfinding.Nodes {
	/// <summary>
	/// A node representing a 10x10 tile area in the world
	/// </summary>
	public sealed class CoarseNode {
		// Only the tiles at the edges of this node are considered for pathfinding purposes
		// Furthermore, each tile says which other tiles it is "linked" to and how long it would take for an item to travel between the two
		// (The shortest path is always used)

	}
}
