using SerousEnergyLib.API;
using SerousEnergyLib.API.Fluid.Default;
using Terraria;
using Terraria.ID;

namespace SerousEnergyLib.Items.Materials.Default {
	#pragma warning disable CS1591
	public class WaterRecipeItem : FluidRecipeItem {
		public override int FluidType => SerousMachines.FluidType<WaterFluidID>();

		public override string Texture => "Terraria/Images/Liquid_0";

		public override void SetStaticDefaults() {
			Main.RegisterItemAnimation(Type, new DrawAnimationHorizontal(8, 17));
		}

		public override void SetDefaults() {
			Item.width = 16;
			Item.height = 16;
		}
	}

	public class LavaRecipeItem : FluidRecipeItem {
		public override int FluidType => SerousMachines.FluidType<LavaFluidID>();

		public override string Texture => "Terraria/Images/Liquid_1";

		public override void SetDefaults() {
			Item.width = 16;
			Item.height = 16;
		}
	}

	public class HoneyRecipeItem : FluidRecipeItem {
		public override int FluidType => SerousMachines.FluidType<HoneyFluidID>();

		public override string Texture => "Terraria/Images/Liquid_11";

		public override void SetDefaults() {
			Item.width = 16;
			Item.height = 16;
		}
	}
}
