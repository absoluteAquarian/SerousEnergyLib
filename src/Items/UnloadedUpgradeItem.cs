using SerousCommonLib.API;
using SerousEnergyLib.API.Upgrades;
using System.Collections.Generic;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;

namespace SerousEnergyLib.Items {
	internal class UnloadedUpgradeItem : BaseUpgradeItem {
		public readonly UnloadedUpgrade upgrade = new();

		public override BaseUpgrade Upgrade => upgrade;

		public override string Texture => ModContent.GetInstance<UnloadedItem>().Texture;

		internal string unloadedMod;
		internal string unloadedName;

		public override void SetDefaults() {
			Item.CloneDefaults(ModContent.ItemType<UnloadedItem>());
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			TooltipHelper.FindAndModify(tooltips, "<MOD_NAME>", unloadedMod);
			TooltipHelper.FindAndModify(tooltips, "<UPGRADE_NAME>", unloadedName);
		}
	}
}
