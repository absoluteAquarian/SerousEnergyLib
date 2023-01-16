using SerousEnergyLib.API.Fluid.Default;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Fluid {
	/// <summary>
	/// The central class for loading <see cref="FluidTypeID"/> instances
	/// </summary>
	public static class FluidLoader {
		#pragma warning disable CS1591
		private class Loadable : ILoadable {
			public void Load(Mod mod) {
				mod.AddContent(new WaterFluidID());
				mod.AddContent(new LavaFluidID());
				mod.AddContent(new HoneyFluidID());
				mod.AddContent(new UnloadedFluidID(null, null));
			}

			public void Unload() {
				fluids.Clear();
			}
		}

		private static readonly List<FluidTypeID> fluids = new();

		public static int Count => fluids.Count;

		internal static int Register(FluidTypeID fluidType) {
			fluids.Add(fluidType);
			return Count - 1;
		}

		public static FluidTypeID Get(int index) => index < 0 || index >= Count ? null : fluids[index];
	}
}
