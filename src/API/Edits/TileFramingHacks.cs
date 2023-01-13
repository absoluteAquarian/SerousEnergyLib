using SerousCommonLib.API;
using SerousEnergyLib.Systems;

namespace SerousEnergyLib.API.Edits {
	internal class TileFramingHacks : Edit {
		public override void LoadEdits() {
			// These hacks are needed to make multitiles (e.g. chests and machines) be placeable on network tiles
			On.Terraria.WorldGen.TileFrame += Hook_WorldGen_TileFrame;
			On.Terraria.Player.PlaceThing += Hoook_Player_PlaceThing;
		}

		public override void UnloadEdits() {
			On.Terraria.WorldGen.TileFrame -= Hook_WorldGen_TileFrame;
			On.Terraria.Player.PlaceThing -= Hoook_Player_PlaceThing;
		}

		private static void Hook_WorldGen_TileFrame(On.Terraria.WorldGen.orig_TileFrame orig, int i, int j, bool resetFrame, bool noBreak) {
			// Further bullshit to make the game think that network tiles are solid when they really aren't
			NetworkTileHacks.SetNetworkTilesToSolid(solid: true);

			try {
				orig(i, j, resetFrame, noBreak);
			} finally {
				NetworkTileHacks.SetNetworkTilesToSolid(solid: false);
			}
		}

		private static void Hoook_Player_PlaceThing(On.Terraria.Player.orig_PlaceThing orig, Terraria.Player self) {
			// Further bullshit to make the game think that network tiles are solid when they really aren't
			NetworkTileHacks.SetNetworkTilesToSolid(solid: true);

			try {
				orig(self);
			} finally {
				NetworkTileHacks.SetNetworkTilesToSolid(solid: false);
			}
		}
	}
}
