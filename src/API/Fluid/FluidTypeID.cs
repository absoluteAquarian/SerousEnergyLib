using Microsoft.Xna.Framework;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Items.Materials;
using System.Text.RegularExpressions;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Fluid {
	/// <summary>
	/// An ID representing a fluid (liquid/gas) that can be stored within machines and pipe networks
	/// </summary>
	public abstract class FluidTypeID : ModType {
		/// <summary>
		/// An invalid ID representing "no fluid"
		/// </summary>
		public const int None = -1;

		/// <summary>
		/// The ID assigned to this fluid type
		/// </summary>
		public int Type { get; protected private set; } = None;

		/// <summary>
		/// The translations for the display name of this fluid.
		/// </summary>
		public ModTranslation DisplayName { get; protected private set; }

		/// <summary>
		/// Whether this fluid is a liquid (<see langword="true"/>) or a gas (<see langword="false"/>)
		/// </summary>
		public abstract bool IsLiquid { get; }

		/// <summary>
		/// The colour used to render the fluid in pipes and <see cref="IFluidMachine"/> UIs
		/// </summary>
		public abstract Color FluidColor { get; }

		/// <summary>
		/// The item ID to use when displaying this fluid type in a recipe
		/// </summary>
		public abstract int RecipeItemType { get; }

		#pragma warning disable CS1591
		protected sealed override void Register() {
			ModTypeLookup<FluidTypeID>.Register(this);

			DisplayName = LocalizationLoader.GetOrCreateTranslation(Mod, $"FluidName.{Name}");

			Type = FluidLoader.Register(this);
		}

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
		/// Gets the display name for this fluid ID
		/// </summary>
		public virtual string GetPrintedDisplayName() => DisplayName.GetTranslation(Language.ActiveCulture);
	}
}
