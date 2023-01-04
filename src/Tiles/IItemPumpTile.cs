namespace SerousEnergyLib.Tiles {
	/// <summary>
	/// An interface representing a tile that can pump items out of inventories
	/// </summary>
	public interface IItemPumpTile : IPumpTile, IItemTransportTile {
		double IItemTransportTile.TransportSpeed => 45d / 60d;
	}
}
