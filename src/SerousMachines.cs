using SerousEnergyLib.API.Energy;
using SerousEnergyLib.API.Fluid;
using SerousEnergyLib.API.Upgrades;
using SerousEnergyLib.Systems;
using System.IO;
using Terraria.ModLoader;

namespace SerousEnergyLib {
	#pragma warning disable CS1591
	public class SerousMachines : Mod {
		public static SerousMachines Instance => ModContent.GetInstance<SerousMachines>();

		public static int EnergyType<T>() where T : EnergyTypeID => ModContent.GetInstance<T>()?.Type ?? -1;

		public static int FluidType<T>() where T : FluidTypeID => ModContent.GetInstance<T>()?.Type ?? -1;

		public static int UpgradeType<T>() where T : BaseUpgrade => ModContent.GetInstance<T>()?.Type ?? -1;

		public override void HandlePacket(BinaryReader reader, int whoAmI) => Netcode.HandlePacket(reader, whoAmI);
	}
}