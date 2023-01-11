using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Systems;
using SerousEnergyLib.Tiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerousEnergyLib.Players {
	internal class UIExclusionAndRangeHandler : ModPlayer {
		public override void UpdateDead() {
			if (Player.whoAmI == Main.myPlayer)
				UIHandler.CloseUI();
		}

		public override void ResetEffects() {
			// Only clientside logic should be handled here
			if (Player.whoAmI != Main.myPlayer)
				return;

			if (UIHandler.ActiveMachine is not null) {
				if (Player.chest != -1 || !Main.playerInventory || Player.sign > -1 || Player.talkNPC > -1) {
					// A vanilla UI was opened
					UIHandler.CloseUI();
					Recipe.FindRecipes();
				} else if (UIHandler.ActiveMachine is ModTileEntity entity) {
					// Player must be close to the machine
					int playerX = (int)(Player.Center.X / 16f);
					int playerY = (int)(Player.Center.Y / 16f);

					if (!IMachine.TryFindMachine(new Point16(playerX, playerY), out IMachine machine) || !object.ReferenceEquals(UIHandler.ActiveMachine, machine)) {
						// Player's center is not within the machine
						if (TileLoader.GetTile(Main.tile[entity.Position.X, entity.Position.Y].TileType) is IMachineTile machineTile) {
							// Get the dimensions, then check if the player can reach the machine
							machineTile.GetMachineDimensions(out uint width, out uint height);

							int left = entity.Position.X;
							int top = entity.Position.Y;
							int right = (int)(entity.Position.X + width);
							int bottom = (int)(entity.Position.Y + height);

							if (playerX < left - Player.tileRangeX || playerX > right + Player.tileRangeX || playerY < top - Player.tileRangeY || playerY > bottom + Player.tileRangeY) {
								// Player is too far away
								SoundEngine.PlaySound(SoundID.MenuClose);
								UIHandler.CloseUI();
								Recipe.FindRecipes();
							}
						} else {
							// Invalid entity state
							SoundEngine.PlaySound(SoundID.MenuClose);
							UIHandler.CloseUI();
							Recipe.FindRecipes();
						}
					}
				}
			}
		}
	}
}
