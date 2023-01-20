using Microsoft.Xna.Framework;
using SerousEnergyLib.Items.Materials.Default;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Energy.Default {
	/// <summary>
	/// The default energy type, Terra Flux
	/// </summary>
	public sealed class TerraFluxTypeID : EnergyTypeID {
		#pragma warning disable CS1591
		public override string ShortName { get; } = "TF";

		public override int RecipeItemType => ModContent.ItemType<TerraFluxRecipeItem>();

		public override Color Color => Color.Red;
	}
}
