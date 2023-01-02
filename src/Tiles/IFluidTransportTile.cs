namespace SerousEnergyLib.Tiles {
	/// <summary>
	/// An interface representing a tile that contains metadata for fluid transportation
	/// </summary>
	public interface IFluidTransportTile {
		/// <summary>
		/// How many Liters (L) of fluid can be stored in this tile
		/// </summary>
		double MaxCapacity { get; }
	}
}
