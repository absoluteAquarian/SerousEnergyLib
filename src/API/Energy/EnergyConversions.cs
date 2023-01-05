using SerousEnergyLib.API.Energy.Default;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Energy {
	/// <summary>
	/// The central class for loading <see cref="EnergyTypeID"/> classifications and converting between them
	/// </summary>
	public static class EnergyConversions {
		private class Loadable : ILoadable {
			public void Load(Mod mod) { }

			public void Unload() {
				types.Clear();
			}
		}

		private static readonly List<EnergyTypeID> types = new();

		#pragma warning disable CS1591
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
			TerraFlux tf = ConvertToTerraFlux(amount, sourceType);

			return ConvertFromTerraFlux(tf, destinationType);
		}

		/// <summary>
		/// Converts a specified amount of energy from one type to another
		/// </summary>
		/// <typeparam name="TSource">The source type of the energy to convert</typeparam>
		/// <typeparam name="TDestination">The destination type of the converted energy</typeparam>
		/// <param name="amount">The amount of energy represented by <typeparamref name="TSource"/></param>
		/// <returns>The amount of energy represented by <typeparamref name="TDestination"/></returns>
		public static double Convert<TSource, TDestination>(double amount) where TSource : EnergyTypeID where TDestination : EnergyTypeID {
			TerraFlux tf = ConvertToTerraFlux<TSource>(amount);

			return ConvertFromTerraFlux<TDestination>(tf);
		}

		/// <summary>
		/// Converts a specified amount of energy from a type to <see cref="TerraFlux"/>
		/// </summary>
		/// <param name="amount">The amount of energy represented by <paramref name="sourceType"/></param>
		/// <param name="sourceType">The source ID of the energy to convert</param>
		/// <returns>The amount of energy represented by <see cref="TerraFlux"/></returns>
		public static TerraFlux ConvertToTerraFlux(double amount, int sourceType) {
			if (sourceType == SerousMachines.EnergyType<TerraFluxTypeID>())
				return new TerraFlux(amount);

			// Convert to Terra Flux
			double tf = amount * Get(sourceType).ConversionRatioToTerraFlux;

			return new TerraFlux(tf);
		}

		/// <summary>
		/// Converts a specified amount of energy from a type to <see cref="TerraFlux"/>
		/// </summary>
		/// <typeparam name="T">The source type of the energy to convert</typeparam>
		/// <param name="amount">The amount of energy represented by <typeparamref name="T"/></param>
		/// <returns>The amount of energy represented by <see cref="TerraFlux"/></returns>
		public static TerraFlux ConvertToTerraFlux<T>(double amount) where T : EnergyTypeID {
			if (SerousMachines.EnergyType<T>() == SerousMachines.EnergyType<TerraFluxTypeID>())
				return new TerraFlux(amount);

			// Convert to Terra Flux
			double tf = amount * ModContent.GetInstance<T>().ConversionRatioToTerraFlux;

			return new TerraFlux(tf);
		}

		/// <summary>
		/// Converts a specified amount of energy from <see cref="TerraFlux"/> to another type
		/// </summary>
		/// <param name="flux">The amount of <see cref="TerraFlux"/> to convert</param>
		/// <param name="destinationType">Te destination ID of the converted energy</param>
		/// <returns>The amount of energy represented by <paramref name="destinationType"/></returns>
		public static double ConvertFromTerraFlux(TerraFlux flux, int destinationType) {
			if (destinationType == SerousMachines.EnergyType<TerraFluxTypeID>())
				return (double)flux;

			// Convert to the destination type
			double ratio = Get(destinationType).ConversionRatioToTerraFlux;

			// Ensure that the amount is always positive
			double tf = (double)flux;
			return ratio <= 0 || tf <= 0 ? 0 : tf / ratio;
		}

		/// <summary>
		/// Converts a specified amount of energy from <see cref="TerraFlux"/> to another type
		/// </summary>
		/// <typeparam name="T">The destination type of the converted energy</typeparam>
		/// <param name="flux">The amount of <see cref="TerraFlux"/> to convert</param>
		/// <returns>The amount of energy represented by <typeparamref name="T"/></returns>
		public static double ConvertFromTerraFlux<T>(TerraFlux flux) where T : EnergyTypeID {
			if (SerousMachines.EnergyType<T>() == SerousMachines.EnergyType<TerraFluxTypeID>())
				return (double)flux;

			// Convert to the destination type
			double ratio = ModContent.GetInstance<T>().ConversionRatioToTerraFlux;

			// Ensure that the amount is always positive
			double tf = (double)flux;
			return ratio <= 0 || tf <= 0 ? 0 : tf / ratio;
		}
	}
}
