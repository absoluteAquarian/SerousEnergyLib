using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Fluid.Default {
	[Autoload(false)]
	public sealed class WaterFluidID : FluidTypeID {
		public override bool IsLiquid => true;

		public override Color FluidColor => new Color() { PackedValue = 0xfff96b33 };
	}

	[Autoload(false)]
	public sealed class LavaFluidID : FluidTypeID {
		public override bool IsLiquid => true;

		public override Color FluidColor => new Color() { PackedValue = 0xff0320fd };
	}

	[Autoload(false)]
	public sealed class HoneyWaterFluidID : FluidTypeID {
		public override bool IsLiquid => true;

		public override Color FluidColor => new Color() { PackedValue = 0xff14c2fe };
	}

	[Autoload(false)]
	public sealed class UnloadedFluidID : FluidTypeID {
		public readonly string unloadedMod, unloadedName;

		public UnloadedFluidID(string mod, string name) {
			unloadedMod = mod;
			unloadedName = name;
		}

		public override bool IsLiquid => false;

		public override Color FluidColor => Color.HotPink;

		internal UnloadedFluidID Clone(string mod, string name)
			=> new UnloadedFluidID(mod, name) {
				Type = Type,
				DisplayName = DisplayName
			};

		public override string GetPrintedDisplayName() => $"{unloadedMod}:{unloadedName}";
	}
}
