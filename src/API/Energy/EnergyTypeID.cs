using Terraria.ModLoader;

namespace SerousEnergyLib.API.Energy {
	public abstract class EnergyTypeID : ModType {
		/// <summary>
		/// The ID of this energy type
		/// </summary>
		public int Type { get; private set; }

		/// <summary>
		/// The conversion ratio of this energy type to <see cref="TerraFlux"/><br/>
		/// This property is a <b>multiplier</b>:  terra flux = original quantity * <see cref="ConversionRatioToTerraFlux"/><br/>
		/// Defaults to 1
		/// </summary>
		public double ConversionRatioToTerraFlux { get; } = 1;

		/// <summary>
		/// The full display name for this energy type (e.g. "Terra Flux")
		/// </summary>
		public ModTranslation DisplayName { get; private set; }

		/// <summary>
		/// The short display name for this energy type (e.g. "TF")
		/// </summary>
		public abstract string ShortName { get; }

		protected sealed override void Register() {
			ModTypeLookup<EnergyTypeID>.Register(this);

			DisplayName = LocalizationLoader.GetOrCreateTranslation(Mod, $"EnergyTypeName.{Name}");

			Type = EnergyConversions.Register(this);
		}

		public sealed override void SetupContent() => SetStaticDefaults();
	}
}
