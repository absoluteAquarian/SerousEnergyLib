using SerousEnergyLib.API.Energy;
using SerousEnergyLib.TileData;
using Terraria.ObjectData;

namespace SerousEnergyLib.Tiles {
	public class NetworkJunction : BaseNetworkTile, IItemTransportTile, IFluidTransportTile, IPowerTransportTile {
		public override string Texture => "SerousEnergyLib/Assets/Tiles/NetworkJunction";
		public override NetworkType NetworkTypeToPlace => NetworkType.Items | NetworkType.Fluids | NetworkType.Power;

		public double TransportTime => 60d;

		double IFluidTransportTile.MaxCapacity => 1d;
		TerraFlux IPowerTransportTile.MaxCapacity => new TerraFlux(200d);

		protected override void PreRegisterTileObjectData() {
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.StyleWrapLimit = 16;
		}
	}
}
