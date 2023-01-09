using SerousEnergyLib.API;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SerousEnergyLib.Items.Materials {
	/// <summary>
	/// An item representing a time requirement for a <see cref="MachineRecipe"/>
	/// </summary>
	public sealed class TimeRecipeItem : ModItem {
		#pragma warning disable CS1591
		public override string Texture => "Terraria/Images/Item_" + ItemID.GoldWatch;

		public override void SetDefaults() {
			Item.maxStack = 99999;
			Item.rare = ItemRarityID.Yellow;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			// Modify the name line to change something like "N/A Seconds" to "10 seconds (600 ticks)"
			int index = tooltips.FindIndex(t => t.Mod == "Terraria" && t.Name == "ItemName");
			if (index != -1) {
				ref string name = ref tooltips[index].Text;

				name = Language.GetTextValue("Mods.SerousEnergyLib.TimeItemDuration", Item.stack / 60d, Item.stack);
			}
		}
	}

	/// <summary>
	/// An item representing a time requirement for a <see cref="MachineRecipe"/>
	/// </summary>
	public sealed class TimeMinimumRangeRecipeItem : ModItem {
		#pragma warning disable CS1591
		public override string Texture => "Terraria/Images/Item_" + ItemID.GoldWatch;

		public override void SetDefaults() {
			Item.maxStack = 99999;
			Item.rare = ItemRarityID.Yellow;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			// Modify the name line to change something like "N/A Seconds" to "Minimum: 10 seconds (600 ticks)"
			int index = tooltips.FindIndex(t => t.Mod == "Terraria" && t.Name == "ItemName");
			if (index != -1) {
				ref string name = ref tooltips[index].Text;

				name = Language.GetTextValue("Mods.SerousEnergyLib.TimeItemMiniumumDuration", Item.stack / 60d, Item.stack);
			}
		}
	}

	/// <summary>
	/// An item representing a time requirement for a <see cref="MachineRecipe"/>
	/// </summary>
	public sealed class TimeMaximumRangeRecipeItem : ModItem {
		#pragma warning disable CS1591
		public override string Texture => "Terraria/Images/Item_" + ItemID.GoldWatch;

		public override void SetDefaults() {
			Item.maxStack = 99999;
			Item.rare = ItemRarityID.Yellow;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			// Modify the name line to change something like "N/A Seconds" to "Maximum: 10 seconds (600 ticks)"
			int index = tooltips.FindIndex(t => t.Mod == "Terraria" && t.Name == "ItemName");
			if (index != -1) {
				ref string name = ref tooltips[index].Text;

				name = Language.GetTextValue("Mods.SerousEnergyLib.TimeItemMaxiumumDuration", Item.stack / 60d, Item.stack);
			}
		}
	}
}
