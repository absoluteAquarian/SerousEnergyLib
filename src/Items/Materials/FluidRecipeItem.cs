using SerousEnergyLib.API.Fluid;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerousEnergyLib.Items.Materials {
	/// <summary>
	/// An item representing a <see cref="FluidTypeID"/> ingredient for a recipe
	/// </summary>
	public abstract class FluidRecipeItem : ModItem {
		/// <summary>
		/// The <see cref="FluidTypeID"/> that this item represents
		/// </summary>
		public abstract int FluidType { get; }

#pragma warning disable CS1591
		public override void SetDefaults() {
			Item.maxStack = 99999;
			Item.rare = ItemRarityID.Blue;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			// Modify the name line to change something like "Water (575)" to "Water (575mL)"
			int index = tooltips.FindIndex(t => t.Mod == "Terraria" && t.Name == "ItemName");
			if (index != -1) {
				ref string name = ref tooltips[index].Text;

				if (Item.stack == 1)
					name += " (1 mL)";
				else if (SerousMachines.itemStackRegex.IsMatch(name))
					name = SerousMachines.itemStackRegex.Replace(name, $"({Item.stack} mL)", 1);
			}
		}
	}
}
