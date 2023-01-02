using SerousEnergyLib.API.Energy;
using SerousEnergyLib.TileData;

namespace SerousEnergyLib.Systems.Networks {
	public sealed class PowerNetwork : NetworkInstance {
		public FluxStorage Storage = new FluxStorage(TerraFlux.Zero);

		internal PowerNetwork() : base(NetworkType.Power) { }
	}
}
