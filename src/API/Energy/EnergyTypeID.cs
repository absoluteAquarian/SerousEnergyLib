using System.Text.RegularExpressions;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Energy {
	/// <summary>
	/// A classification of power that can be stored in machines
	/// </summary>
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
		/// The translations for the display name of this energy type.
		/// </summary>
		public ModTranslation DisplayName { get; private set; }

		/// <summary>
		/// The short display name for this energy type (e.g. "TF")
		/// </summary>
		public abstract string ShortName { get; }

		/// <summary>
		/// The item ID to use when displaying this energy type in a recipe
		/// </summary>
		public abstract int RecipeItemType { get; }

		#pragma warning disable CS1591
		protected sealed override void Register() {
			ModTypeLookup<EnergyTypeID>.Register(this);

			DisplayName = LocalizationLoader.GetOrCreateTranslation(Mod, $"EnergyTypeName.{Name}");

			Type = EnergyConversions.Register(this);
		}

		/// <inheritdoc cref="ModType.SetupContent"/>
		public sealed override void SetupContent() {
			AutoStaticDefaults();
			SetStaticDefaults();
		}

		/// <summary>
		/// Automatically sets certain static defaults. Override this if you do not want the properties to be set for you.
		/// </summary>
		public virtual void AutoStaticDefaults() {
			if (DisplayName.IsDefault())
				DisplayName.SetDefault(Regex.Replace(Name, "([A-Z])", " $1").Trim());
		}

		/// <summary>
		/// Gets the display name for this energy ID
		/// </summary>
		public virtual string GetPrintedDisplayName() => DisplayName.GetTranslation(Language.ActiveCulture);
	}
}
