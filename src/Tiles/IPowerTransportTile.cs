using SerousEnergyLib.API.Energy;

namespace SerousEnergyLib.Tiles {
	/// <summary>
	/// An interface representing a tile that contains metadata for power transportation
	/// </summary>
	public interface IPowerTransportTile {
		/// <summary>
		/// How much power in Terra Flux (TF) can be stored in this tile.<br/>
		/// To convert from a custom power type to Terra Flux, use <see cref="EnergyConversions.ConvertToTerraFlux(double, int)"/> or <see cref="EnergyConversions.ConvertToTerraFlux{T}(double)"/>
		/// </summary>
		TerraFlux MaxCapacity { get; }
	}
}
