using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Energy {
	public sealed class FluxStorage {
		public TerraFlux CurrentCapacity;

		public TerraFlux MaxCapacity;

		public TerraFlux BaseMaxCapacity { get; private set; }

		public bool IsEmpty => CurrentCapacity <= TerraFlux.Zero;

		public bool IsFull => CurrentCapacity >= MaxCapacity;

		public FluxStorage(TerraFlux max) {
			BaseMaxCapacity = MaxCapacity = max;
		}

		public FluxStorage(double max, int energyType) {
			BaseMaxCapacity = MaxCapacity = EnergyConversions.ConvertToTerraFlux(max, energyType);
		}

		public void SaveData(TagCompound tag) {
			tag["current"] = (double)CurrentCapacity;
			tag["max"] = (double)MaxCapacity;
			tag["base"] = (double)BaseMaxCapacity;
		}

		public void LoadData(TagCompound tag) {
			CurrentCapacity = new TerraFlux(tag.GetDouble("current"));
			MaxCapacity = new TerraFlux(tag.GetDouble("max"));
			BaseMaxCapacity = new TerraFlux(tag.GetDouble("base"));
		}

		public void Import(ref TerraFlux amount) {
			TerraFlux import = amount;

			if (CurrentCapacity + import > MaxCapacity)
				import = MaxCapacity - import;

			CurrentCapacity += import;
			amount -= import;
		}

		public void ImportFrom(FluxStorage source, TerraFlux import) {
			// No power to import
			if (source.IsEmpty)
				return;

			// Cannot import power
			if (this.IsFull)
				return;

			// Export the power
			source.Export(ref import);
			this.Import(ref import);

			// Import any leftovers
			source.Import(ref import);
		}

		public void Export(ref TerraFlux amount) {
			if (amount > CurrentCapacity)
				amount = CurrentCapacity;

			CurrentCapacity -= amount;
		}

		public void ExportTo(FluxStorage destination, TerraFlux export) {
			// No power to export
			if (this.IsEmpty)
				return;

			// Cannot export power
			if (destination.IsFull)
				return;

			// Import the power
			this.Export(ref export);
			destination.Import(ref export);

			// Import any leftovers
			this.Import(ref export);
		}
	}
}
