using Newtonsoft.Json;
using System.ComponentModel;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace SerousEnergyLib.Configs {
	/// <summary>
	/// The configuration for handling machines and their upgrades
	/// </summary>
	[Label("$Mods.SerousEnergyLib.Config.MachineConfig.Label")]
	public sealed class MachineConfig : ModConfig {
		#pragma warning disable CS1591
		public override ConfigScope Mode => ConfigScope.ServerSide;

		public static MachineConfig Instance => ModContent.GetInstance<MachineConfig>();

		[JsonIgnore]
		public static float UpgradeFactor => Instance.upgradeFactor;

		[Label("$Mods.SerousEnergyLib.Config.MachineConfig.upgradeFactor.Label")]
		[Tooltip("$Mods.SerousEnergyLib.Config.MachineConfig.upgradeFactor.Tooltip")]
		[Range(1, 10)]
		[Increment(0.01f)]
		[DefaultValue(10)]
		public float upgradeFactor;

		public override void OnChanged() {
			upgradeFactor = Utils.Clamp(upgradeFactor, 1, 10);
		}
	}
}
