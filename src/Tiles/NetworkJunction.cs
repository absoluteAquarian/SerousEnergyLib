using SerousEnergyLib.API.Energy;
using SerousEnergyLib.TileData;
using Terraria.ObjectData;

namespace SerousEnergyLib.Tiles {
	/// <summary>
	/// A tile representing a junction between two networks
	/// </summary>
	public class NetworkJunction : BaseNetworkTile, IItemTransportTile, IFluidTransportTile, IPowerTransportTile {
		#pragma warning disable CS1591
		public override string Texture => "SerousEnergyLib/Assets/Tiles/NetworkJunction";
		public override NetworkType NetworkTypeToPlace => NetworkType.Items | NetworkType.Fluids | NetworkType.Power;

		public double TransportSpeed => 1d;

		double IFluidTransportTile.MaxCapacity => 1d;

		double IFluidTransportTile.ExportRate => 0.4 / 60d;

		TerraFlux IPowerTransportTile.MaxCapacity => new TerraFlux(200d);

		TerraFlux IPowerTransportTile.TransferRate => new TerraFlux(500d / 60d);

		public float GetItemSize(int x, int y) => 5.5f;

		protected override void PreRegisterTileObjectData() {
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.StyleWrapLimit = 16;
		}
	}
}
