namespace SerousEnergyLib.Tiles {
	/// <summary>
	/// An interface representing a tile that contains metadata for item transportation pathfinding
	/// </summary>
	public interface IItemTransportTile {
		/// <summary>
		/// How long it takes for the item to move through this tile, measured in game ticks
		/// </summary>
		double TransportTime { get; }
	}
}
