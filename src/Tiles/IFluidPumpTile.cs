namespace SerousEnergyLib.Tiles {
	/// <summary>
	/// An interface representing a tile that can pump fluids out of fluid storages
	/// </summary>
	public interface IFluidPumpTile : IPumpTile, IFluidTransportTile {
		double IFluidTransportTile.MaxCapacity => 0.4d;

		double IFluidTransportTile.ExportRate => 0.2 / 60d;
	}
}
