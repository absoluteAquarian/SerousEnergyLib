using SerousEnergyLib.Tiles;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace SerousEnergyLib.Systems {
	internal class NetworkTileHacks : ModSystem {
		private static List<ushort> typesValidForSolidSwapping;

		public override void PostSetupContent() {
			// Get a list of every type that should be considered for Main.tileSolid[] abuse
			typesValidForSolidSwapping = ModContent.GetContent<BaseNetworkTile>().Select(b => b.Type).ToList();
		}

		public override void Unload() {
			typesValidForSolidSwapping = null;
		}

		internal static void SetNetworkTilesToSolid(bool solid) {
			foreach (ushort type in typesValidForSolidSwapping)
				Main.tileSolid[type] = solid;
		}

		public override void PreUpdateEntities() {
			// ModSystem.PreUpdateEntities() is called before WorldGen.UpdateWorld(), which updates the tile entities
            // So this is a good place to have the tile stuff update

            // Sanity check
            SetNetworkTilesToSolid(solid: false);

			// TODO: reset wire network export counts
		}

		public override void PreUpdateItems() {
			// TODO: update item and fluid networks
		}
	}
}
