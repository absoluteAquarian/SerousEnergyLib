using SerousEnergyLib.API.Machines;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Upgrades {
	[Autoload(false)]
	internal class UnloadedUpgrade : BaseUpgrade {
		private readonly string unloadedMod, unloadedName;

		private readonly TagCompound unloadedData;

		// Purposefully large amount for future-proofing
		public override int MaxUpgradesPerMachine => 10000;

		public UnloadedUpgrade() {
			Type = -1;
		}

		public UnloadedUpgrade(string mod, string name, TagCompound data) : this() {
			unloadedMod = mod;
			unloadedName = name;
			unloadedData = data;
		}

		// Unloaded upgrades can be removed from, but not placed inside of machines
		public override bool CanApplyTo(IMachine machine) => false;

		public override void SaveData(TagCompound tag) {
			tag["mod"] = unloadedMod;
			tag["name"] = unloadedName;
			tag["data"] = unloadedData;
		}
	}
}
