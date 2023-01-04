using SerousEnergyLib.Systems.Networks;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerousEnergyLib.Systems {
	internal class NetworkHandler : ModSystem {
		// Cached collections to reduce runtime from checking for chests every tick
		internal static Dictionary<Point16, int> locationToChest = new();

		public override void Load() {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			TileEntity._UpdateStart += Network.UpdatePowerNetworks;
		}

		public override void Unload() {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			TileEntity._UpdateStart -= Network.UpdatePowerNetworks;
		}

		public override void PreUpdateEntities() {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// Reset net power stat in power networks
			foreach (var net in Network.powerNetworks)
				(net as PowerNetwork).ResetNetStats();
		}

		public override void PreUpdateItems() {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			// Update the location dictionary
			locationToChest.Clear();

			for (int i = 0; i < Main.maxChests; i++) {
				Chest chest = Main.chest[i];

				if (chest is not null) {
					int x = chest.x;
					int y = chest.y;

					locationToChest.Add(new Point16(x, y), i);
					locationToChest.Add(new Point16(x + 1, y), i);
					locationToChest.Add(new Point16(x, y + 1), i);
					locationToChest.Add(new Point16(x + 1, y + 1), i);
				}
			}

			// Update the networks
			Network.UpdateItemNetworks();
			Network.UpdateFluidNetworks();
		}
	}
}
