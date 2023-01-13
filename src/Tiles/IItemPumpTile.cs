namespace SerousEnergyLib.Tiles {
	/// <summary>
	/// An interface representing a tile that can pump items out of inventories
	/// </summary>
	public interface IItemPumpTile : IPumpTile, IItemTransportTile {
		double IItemTransportTile.TransportSpeed => 1d;

		/// <summary>
		/// How many items can be extracted from an inventory per pump cycle
		/// </summary>
		int StackPerExtraction { get; }
	}
}
