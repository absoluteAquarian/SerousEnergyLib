using Newtonsoft.Json;
using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace SerousEnergyLib.Common.Configs {
	/// <summary>
	/// The configuration for handling rendering in this mod
	/// </summary>
	[Label("$Mods.SerousEnergyLib.Config.RenderingConfig.Label")]
	public sealed class RenderingConfig : ModConfig {
		#pragma warning disable CS1591
		public override ConfigScope Mode => ConfigScope.ClientSide;

		public static RenderingConfig Instance => ModContent.GetInstance<RenderingConfig>();

		/// <summary>
		/// Whether item/fluid pump tiles should animate
		/// </summary>
		[JsonIgnore]
		public static bool PlayPumpAnimations => Instance.animatePumps;

		/// <summary>
		/// Whether items moving in item networks should be rendered
		/// </summary>
		[JsonIgnore]
		public static bool RenderItemsInPipes => Instance.drawItems;


		[Label("$Mods.SerousEnergyLib.Config.RenderingConfig.animatePumps.Label")]
		[Tooltip("$Mods.SerousEnergyLib.Config.RenderingConfig.animatePumps.Tooltip")]
		[DefaultValue(true)]
		public bool animatePumps;

		[Label("$Mods.SerousEnergyLib.Config.RenderingConfig.drawItems.Label")]
		[Tooltip("$Mods.SerousEnergyLib.Config.RenderingConfig.drawItems.Tooltip")]
		[DefaultValue(true)]
		public bool drawItems;
	}
}
