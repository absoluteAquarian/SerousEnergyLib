using SerousEnergyLib.API.Fluid.Default;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Fluid {
	public static class FluidLoader {
		private class Loadable : ILoadable {
			public void Load(Mod mod) {
				mod.AddContent(new WaterFluidID());
				mod.AddContent(new LavaFluidID());
				mod.AddContent(new HoneyWaterFluidID());
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
