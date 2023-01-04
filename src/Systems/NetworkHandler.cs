using SerousEnergyLib.Systems.Networks;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SerousEnergyLib.Systems {
	internal class NetworkHandler : ModSystem {
		public override void Load() {
			TileEntity._UpdateStart += Network.UpdatePowerNetworks;
		}

		public override void Unload() {
			TileEntity._UpdateStart -= Network.UpdatePowerNetworks;
		}

		public override void PreUpdateEntities() {
			// Reset net power stat in power networks
			foreach (var net in Network.powerNetworks)
				(net as PowerNetwork).ResetNetStats();
		}

		public override void PreUpdateItems() {
			// Update the networks
			Network.UpdateItemNetworks();
			Network.UpdateFluidNetworks();
		}
	}
}
