using Microsoft.Xna.Framework;
using SerousEnergyLib.Items.Materials.Default;
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

		public override int RecipeItemType => ModContent.ItemType<WaterRecipeItem>();
	}

	/// <summary>
	/// A fluid ID representing <see cref="LiquidID.Lava"/>
	/// </summary>
	[Autoload(false)]
	public sealed class LavaFluidID : FluidTypeID {
		public override bool IsLiquid => true;

		public override Color FluidColor => new Color() { PackedValue = 0xff0320fd };

		public override int RecipeItemType => ModContent.ItemType<LavaRecipeItem>();
	}

	/// <summary>
	/// A fluid ID representing <see cref="LiquidID.Honey"/>
	/// </summary>
	[Autoload(false)]
	public sealed class HoneyFluidID : FluidTypeID {
		public override bool IsLiquid => true;

		public override Color FluidColor => new Color() { PackedValue = 0xff14c2fe };

		public override int RecipeItemType => ModContent.ItemType<HoneyRecipeItem>();
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

		// Should not be displayed in recipes, so this property is useless
		public override int RecipeItemType => -1;

		internal UnloadedFluidID Clone(string mod, string name)
			=> new UnloadedFluidID(mod, name) {
				Type = Type,
				DisplayName = DisplayName
			};

		public override string GetPrintedDisplayName() => $"{unloadedMod}:{unloadedName}";
	}
}
