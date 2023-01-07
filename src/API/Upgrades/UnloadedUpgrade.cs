using SerousEnergyLib.API.Machines;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Upgrades {
	[Autoload(false)]
	internal class UnloadedUpgrade : BaseUpgrade {
		internal readonly string unloadedMod, unloadedName;

		// Purposefully large amount for future-proofing
		public override int MaxUpgradesPerMachine => 10000;

		public UnloadedUpgrade() {
			Type = -1;
		}

		public UnloadedUpgrade(string mod, string name) : this() {
			unloadedMod = mod;
			unloadedName = name;
		}

		// Unloaded upgrades can be removed from, but not placed inside of machines
		public override bool CanApplyTo(IMachine machine) => false;
	}
}
