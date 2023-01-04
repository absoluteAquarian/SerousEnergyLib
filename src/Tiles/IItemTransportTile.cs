namespace SerousEnergyLib.Tiles {
	/// <summary>
	/// An interface representing a tile that contains metadata for item transportation pathfinding
	/// </summary>
	public interface IItemTransportTile : IPipedItemDrawingTile {
		/// <summary>
		/// How fast an item can move in this tile, measured in tiles per second
		/// </summary>
		double TransportSpeed { get; }
	}
}
