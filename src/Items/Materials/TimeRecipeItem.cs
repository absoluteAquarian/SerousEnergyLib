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
		public override void SetDefaults() {
			Item.maxStack = 99999;
			Item.rare = ItemRarityID.Yellow;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			// Modify the name line to change something like "Water (575)" to "Water (575mL)"
			int index = tooltips.FindIndex(t => t.Mod == "Terraria" && t.Name == "ItemName");
			if (index != -1) {
				ref string name = ref tooltips[index].Text;

				name = Language.GetTextValue("Mods.SerousEnergyLib.TimeItemDuration", Item.stack / 60d, Item.stack);
			}
		}
	}
}
