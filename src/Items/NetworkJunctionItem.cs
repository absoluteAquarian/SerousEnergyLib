using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SerousCommonLib.API;
using SerousEnergyLib.API;
using SerousEnergyLib.Tiles;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SerousEnergyLib.Items {
	#pragma warning disable CS1591
	public sealed class NetworkJunctionItem : ModItem {
		public override string Texture => "SerousEnergyLib/Assets/Tiles/NetworkJunction";

		public override void SetStaticDefaults() {
			// Frame gets overwritten in custom PlayerDrawLayers
			Main.RegisterItemAnimation(Type, new DrawAnimationHorizontal(1000, 3) {
				NotActuallyAnimating = true
			});
		}

		public override void SetDefaults() {
			Item.width = 16;
			Item.height = 16;
			Item.scale = 16f / 14f;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 15;
			Item.useAnimation = 15;
			Item.useTurn = true;
			Item.autoReuse = true;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.buyPrice(copper: 50);
			Item.consumable = true;
			Item.maxStack = 999;
			Item.createTile = -1;
		}

		public override bool AltFunctionUse(Player player) => true;

		private bool switchingMode;

		public override bool? UseItem(Player player) {
			if (player.altFunctionUse == 2) {
				switchingMode = Item.createTile != -1;

				// Cycle to the next style and prevent placement
				if (!switchingMode)
					Item.placeStyle = ++Item.placeStyle % 3;

				Item.createTile = -1;
				Item.useStyle = ItemUseStyleID.HoldUp;
			} else {
				switchingMode = Item.createTile == -1;

				// Allow placement of the tile
				Item.createTile = ModContent.TileType<NetworkJunction>();
				Item.useStyle = ItemUseStyleID.Swing;
			}

			return true;
		}

		public override bool ConsumeItem(Player player) {
			// Right click should not consume the item, only rotate it
			return player.altFunctionUse != 2 && !switchingMode;
		}

		public override void AddRecipes() {
			CreateRecipe(16)
				.AddIngredient(RecipeGroupID.IronBar, 4)
				.AddRecipeGroup(RecipeGroupID.Wood, 10)
				.AddTile(TileID.WorkBenches)
				.Register();
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			TooltipHelper.FindAndInsertLines(Mod, tooltips,
				"<JUNCTION_MODE>",
				static i => "JunctionDesc_" + i,
				Language.GetTextValue("Mods.SerousEnergyLib.JunctionTooltips." + (Item.createTile == -1 ? "Orientation" : "Placing")));
		}

		public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
			// Fool the game into thinking that the junction item has an animation, when it really doesn't
			DrawAnimationHorizontal animation = Main.itemAnimations[ModContent.ItemType<NetworkJunctionItem>()] as DrawAnimationHorizontal;
			animation.Frame = 0;

			var texture = TextureAssets.Item[Type].Value;
			frame = texture.Frame(3, 1, Item.placeStyle, 0);
			spriteBatch.Draw(texture, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0);
			return false;
		}

		public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
			// Fool the game into thinking that the junction item has an animation, when it really doesn't
			DrawAnimationHorizontal animation = Main.itemAnimations[ModContent.ItemType<NetworkJunctionItem>()] as DrawAnimationHorizontal;
			animation.Frame = 0;

			var texture = TextureAssets.Item[Type].Value;
			Rectangle frame = texture.Frame(3, 1, Item.placeStyle, 0);
			spriteBatch.Draw(texture, Item.Center - Main.screenPosition, frame, lightColor, rotation, frame.Size() / 2f, scale, SpriteEffects.None, 0);
			return false;
		}
	}
}
