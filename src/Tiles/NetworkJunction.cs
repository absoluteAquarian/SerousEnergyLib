using SerousEnergyLib.TileData;
using Terraria.ObjectData;

namespace SerousEnergyLib.Tiles {
	public class NetworkJunction : BaseNetworkTile, IItemTransportTile {
		public override string Texture => "SerousEnergyLib/Assets/Tiles/NetworkJunction";

		public double TransportTime { get; } = 60d;

		public override NetworkType NetworkTypeToPlace { get; } = NetworkType.Items | NetworkType.Fluids | NetworkType.Power;

		protected override void PreRegisterTileObjectData() {
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.StyleWrapLimit = 16;
		}
	}
}
