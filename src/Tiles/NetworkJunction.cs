using SerousEnergyLib.API.Energy;
using SerousEnergyLib.TileData;
using Terraria.ObjectData;

namespace SerousEnergyLib.Tiles {
	public class NetworkJunction : BaseNetworkTile, IItemTransportTile, IFluidTransportTile, IPowerTransportTile {
		public override string Texture => "SerousEnergyLib/Assets/Tiles/NetworkJunction";
		public override NetworkType NetworkTypeToPlace => NetworkType.Items | NetworkType.Fluids | NetworkType.Power;

		public double TransportSpeed => 60d;

		double IFluidTransportTile.MaxCapacity => 1d;
		TerraFlux IPowerTransportTile.MaxCapacity => new TerraFlux(200d);

		public float GetItemSize(int x, int y) => 5.5f;

		protected override void PreRegisterTileObjectData() {
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.StyleWrapLimit = 16;
		}
	}
}
