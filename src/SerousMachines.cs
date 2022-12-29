using SerousEnergyLib.API.Energy;
using SerousEnergyLib.API.Fluid;
using Terraria.ModLoader;

namespace SerousEnergyLib {
	public class SerousMachines : Mod {
		public static SerousMachines Instance => ModContent.GetInstance<SerousMachines>();

		public static int EnergyType<T>() where T : EnergyTypeID => ModContent.GetInstance<T>()?.Type ?? -1;

		public static int FluidType<T>() where T : FluidTypeID => ModContent.GetInstance<T>()?.Type ?? -1;
	}
}