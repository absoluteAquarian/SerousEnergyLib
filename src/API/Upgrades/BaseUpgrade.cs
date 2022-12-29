using SerousEnergyLib.API.Machines;
using System.Text.RegularExpressions;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Upgrades {
	/// <summary>
	/// The base type for an upgrade that can be placed in a machine
	/// </summary>
	public abstract class BaseUpgrade : ModType {
		public int Type { get; protected private set; }

		/// <summary>
		/// The translations for the display name of this upgrade.
		/// </summary>
		public ModTranslation DisplayName { get; private set; }

		/// <summary>
		/// The translations for the tooltip of this item.
		/// </summary>
		public ModTranslation Tooltip { get; private set; }

		protected sealed override void Register() {
			ModTypeLookup<BaseUpgrade>.Register(this);

			DisplayName = LocalizationLoader.GetOrCreateTranslation(Mod, $"MachineUpgradeName.{Name}");
			Tooltip = LocalizationLoader.GetOrCreateTranslation(Mod, $"MachineUpgradeTooltip.{Name}", true);

			Type = UpgradeLoader.Register(this);
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

		/// <summary>
		/// Whether this upgrade can be applied to the specified machine
		/// </summary>
		public abstract bool CanApplyTo(IMachine machine);

		public virtual double GetPowerConsumptionMultiplier() => 1d;

		public virtual double GetPowerGenerationMultiplier() => 1d;

		public virtual double GetPowerCapacityMultiplier() => 1d;
	}
}
