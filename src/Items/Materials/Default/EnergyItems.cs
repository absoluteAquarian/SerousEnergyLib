using SerousEnergyLib.API.Energy.Default;

namespace SerousEnergyLib.Items.Materials.Default {
	#pragma warning disable CS1591
	public class TerraFluxRecipeItem : EnergyRecipeItem {
		public override int EnergyType => SerousMachines.EnergyType<TerraFluxTypeID>();

		public override string Texture => "SerousMachines/Assets/Items/TerraFluxRecipeItem";

		public override void SetDefaults() {
			Item.width = 32;
			Item.height = 32;
		}
	}
}
