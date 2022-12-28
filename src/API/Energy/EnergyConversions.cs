using SerousEnergyLib.API.Energy.Default;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Energy {
	public static class EnergyConversions {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				types.Clear();
			}
		}

		private static readonly List<EnergyTypeID> types = new();

		public static int Count => types.Count;

		internal static int Register(EnergyTypeID type) {
			types.Add(type);
			return Count - 1;
		}

		public static EnergyTypeID Get(int index) => index < 0 || index >= Count ? null : types[index];

		/// <summary>
		/// Converts a specified amount of energy from one type to another
		/// </summary>
		/// <param name="amount">The amount of energy represented by <paramref name="sourceType"/></param>
		/// <param name="sourceType">The source ID of the energy to convert</param>
		/// <param name="destinationType">The destination ID of the converted energy</param>
		/// <returns>The amount of energy represented by <paramref name="destinationType"/></returns>
		public static double Convert(double amount, int sourceType, int destinationType) {
			// Convert to Terra Flux
			double tf = amount * Get(sourceType).ConversionRatioToTerraFlux;

			if (destinationType == ModContent.GetInstance<TerraFluxTypeID>().Type)
				return tf;

			// Convert to the destination type
			double ratio = Get(destinationType).ConversionRatioToTerraFlux;

			// Ensure that the amount is always positive
			return ratio <= 0 || tf <= 0 ? 0 : tf / ratio;
		}
	}
}
