using Microsoft.Xna.Framework;
using SerousEnergyLib.API.Machines;
using System.Text.RegularExpressions;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Fluid {
	/// <summary>
	/// An ID representing a fluid (liquid/gas) that can be stored within machines and pipe networks
	/// </summary>
	public abstract class FluidTypeID : ModType {
		public int Type { get; private set; }

		/// <summary>
		/// The translations for the display name of this fluid.
		/// </summary>
		public ModTranslation DisplayName { get; private set; }

		/// <summary>
		/// Whether this fluid is a liquid (<see langword="true"/>) or a gas (<see langword="false"/>)
		/// </summary>
		public abstract bool IsLiquid { get; }

		/// <summary>
		/// The colour used to render the fluid in pipes and <see cref="IFluidMachine"/> UIs
		/// </summary>
		public abstract Color FluidColor { get; }

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

		public virtual void SaveData(TagCompound tag) { }

		public virtual void LoadData(TagCompound tag) { }
	}
}
