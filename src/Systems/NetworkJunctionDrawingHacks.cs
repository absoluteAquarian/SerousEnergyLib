using SerousEnergyLib.API;
using SerousEnergyLib.Items;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SerousEnergyLib.Systems {
	internal class PreDrawHeldItem : PlayerDrawLayer {
		public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.HeldItem);

		protected override void Draw(ref PlayerDrawSet drawInfo) {
			var item = drawInfo.drawPlayer.HeldItem;

			if (item.ModItem is not NetworkJunctionItem)
				return;

			// Fool the game into thinking that the junction item has an animation, when it really doesn't
			DrawAnimationHorizontal animation = Main.itemAnimations[ModContent.ItemType<NetworkJunctionItem>()] as DrawAnimationHorizontal;
			animation.Frame = item.placeStyle;
		}
	}

	internal class PostDrawHeldItem : PlayerDrawLayer {
		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.HeldItem);

		protected override void Draw(ref PlayerDrawSet drawInfo) {
			var item = drawInfo.drawPlayer.HeldItem;

			if (item.ModItem is not NetworkJunctionItem)
				return;

			// Reset the frame back to 0
			DrawAnimationHorizontal animation = Main.itemAnimations[ModContent.ItemType<NetworkJunctionItem>()] as DrawAnimationHorizontal;
			animation.Frame = 0;
		}
	}
}
