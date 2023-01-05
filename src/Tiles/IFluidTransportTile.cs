namespace SerousEnergyLib.Tiles {
	/// <summary>
	/// An interface representing a tile that contains metadata for fluid transportation
	/// </summary>
	public interface IFluidTransportTile {
		/// <summary>
		/// How many Liters (L) of fluid can be stored in this tile<br/>
		/// For <see cref="IFluidPumpTile"/>, this also dictates the max amount of fluid that can be pumped out per cycle
		/// </summary>
		double MaxCapacity { get; }

		/// <summary>
		/// How many Liters (L) can be exported from this tile's network at its location, per game tick
		/// </summary>
		double ExportRate { get; }
	}
}
