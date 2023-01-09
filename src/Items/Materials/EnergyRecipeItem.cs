using SerousEnergyLib.API;
using SerousEnergyLib.API.Energy;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerousEnergyLib.Items.Materials {
	/// <summary>
	/// An item representing an <see cref="EnergyTypeID"/> ingredient for a <see cref="MachineRecipe"/>
	/// </summary>
	public abstract class EnergyRecipeItem : ModItem {
		/// <summary>
		/// The <see cref="EnergyTypeID"/> that this item represents
		/// </summary>
		public abstract int EnergyType { get; }

#pragma warning disable CS1591
		public override void SetDefaults() {
			Item.maxStack = 99999;
			Item.rare = ItemRarityID.Orange;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			// Modify the name line to change something like "Terra Flux (575)" to "575 Terra Flux"
			int index = tooltips.FindIndex(t => t.Mod == "Terraria" && t.Name == "ItemName");
			if (index != -1) {
				ref string name = ref tooltips[index].Text;

				if (Item.stack == 1)
					name = "1 " + name;
				else if (SerousMachines.itemStackRegex.IsMatch(name)) {
					string stack = SerousMachines.itemStackRegex.Match(name).Groups[1].Value[1..^1];

					name = stack + " " + name[0..name.LastIndexOf('(')];
				}
			}
		}
	}
}
