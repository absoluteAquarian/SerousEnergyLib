using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Fluid.Default {
	[Autoload(false)]
	public class WaterFluidID : FluidTypeID {
		public override bool IsLiquid => true;

		public override Color FluidColor => new Color() { PackedValue = 0xfff96b33 };
	}

	[Autoload(false)]
	public class LavaFluidID : FluidTypeID {
		public override bool IsLiquid => true;

		public override Color FluidColor => new Color() { PackedValue = 0xff0320fd };
	}

	[Autoload(false)]
	public class HoneyWaterFluidID : FluidTypeID {
		public override bool IsLiquid => true;

		public override Color FluidColor => new Color() { PackedValue = 0xff14c2fe };
	}
}
