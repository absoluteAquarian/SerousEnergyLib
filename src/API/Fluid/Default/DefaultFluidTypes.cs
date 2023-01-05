using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Fluid.Default {
	#pragma warning disable CS1591
	/// <summary>
	/// A fluid ID representing <see cref="LiquidID.Water"/>
	/// </summary>
	[Autoload(false)]
	public sealed class WaterFluidID : FluidTypeID {
		public override bool IsLiquid => true;

		public override Color FluidColor => new Color() { PackedValue = 0xfff96b33 };
	}

	/// <summary>
	/// A fluid ID representing <see cref="LiquidID.Lava"/>
	/// </summary>
	[Autoload(false)]
	public sealed class LavaFluidID : FluidTypeID {
		public override bool IsLiquid => true;

		public override Color FluidColor => new Color() { PackedValue = 0xff0320fd };
	}

	/// <summary>
	/// A fluid ID representing <see cref="LiquidID.Honey"/>
	/// </summary>
	[Autoload(false)]
	public sealed class HoneyFluidID : FluidTypeID {
		public override bool IsLiquid => true;

		public override Color FluidColor => new Color() { PackedValue = 0xff14c2fe };
	}

	/// <summary>
	/// A fluid ID representing a fluid that is no longer loaded.  All <see cref="UnloadedFluidID"/> instances share the same type, but their data may not be the same.
	/// </summary>
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
