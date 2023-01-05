﻿using SerousEnergyLib.API.Machines;
using System.Text.RegularExpressions;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Upgrades {
	/// <summary>
	/// The base type for an upgrade that can be placed in a machine
	/// </summary>
	public abstract class BaseUpgrade : ModType {
		/// <summary>
		/// The unique ID for this upgrade type
		/// </summary>
		public int Type { get; protected private set; }

		/// <summary>
		/// The translations for the display name of this upgrade.
		/// </summary>
		public ModTranslation DisplayName { get; private set; }

		/// <summary>
		/// The translations for the tooltip of this item.
		/// </summary>
		public ModTranslation Tooltip { get; private set; }

		#pragma warning disable CS1591
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

		/// <summary>
		/// Return a modifier for power conumption in an <see cref="IPoweredMachine"/> here
		/// </summary>
		public virtual StatModifier GetPowerConsumptionMultiplier() => StatModifier.Default;

		/// <summary>
		/// Return a modifier for power generation in an <see cref="IPowerGeneratorMachine"/> here
		/// </summary>
		public virtual StatModifier GetPowerGenerationMultiplier() => StatModifier.Default;

		/// <summary>
		/// Return a modifier for the maximum power capacity in an <see cref="IPoweredMachine"/> here
		/// </summary>
		public virtual StatModifier GetPowerCapacityMultiplier() => StatModifier.Default;

		/// <summary>
		/// Return a modifier for the maximum fluid storage capacity for a fluid storage in an <see cref="IFluidMachine"/> here
		/// </summary>
		/// <returns></returns>
		public virtual StatModifier GetFluidCapacityMultiplier() => StatModifier.Default;
	}
}
