using SerousEnergyLib.Systems;
using SerousEnergyLib.TileData;
using Terraria;

namespace SerousEnergyLib.Tiles {
	public class NetworkJunction : BaseNetworkTile {
		public override string Texture => "SerousEnergyLib/Assets/Tiles/NetworkJunction";

		public override void PlaceInWorld(int i, int j, Item item) {
			base.PlaceInWorld(i, j, item);

			Network.PlaceEntry(i, j, NetworkType.Items | NetworkType.Fluids | NetworkType.Power);
		}
	}
}
