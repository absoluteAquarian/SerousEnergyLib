using Terraria.ModLoader;

namespace SerousEnergyLib.API {
	partial class Extensions {
		/// <inheritdoc cref="StatModifier.ApplyTo(float)"/>
		public static double ApplyTo(this StatModifier stat, double baseValue) 
			=> (baseValue + stat.Base) * stat.Additive * stat.Multiplicative + stat.Flat;
	}
}
