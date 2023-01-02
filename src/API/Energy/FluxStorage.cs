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
			if (amount <= TerraFlux.Zero)
				return;

			// Cannot import power
			if (IsFull)
				return;

			TerraFlux import = amount;

			if (CurrentCapacity + import > MaxCapacity)
				import = MaxCapacity - import;

			CurrentCapacity += import;
			amount -= import;
		}

		public void ImportFrom(FluxStorage source, TerraFlux import) => Transfer(source, this, import);

		public void Export(ref TerraFlux amount) {
			if (amount <= TerraFlux.Zero)
				return;

			// Cannot export power
			if (IsEmpty) {
				amount = TerraFlux.Zero;
				return;
			}

			if (amount > CurrentCapacity)
				amount = CurrentCapacity;

			CurrentCapacity -= amount;
		}

		public void ExportTo(FluxStorage destination, TerraFlux export) => Transfer(this, destination, export);

		private static void Transfer(FluxStorage source, FluxStorage destination, TerraFlux transfer) {
			if (transfer <= TerraFlux.Zero)
				return;

			// No power to export
			if (source.IsEmpty)
				return;

			// Cannot import power
			if (destination.IsFull)
				return;

			// Export the power
			source.Export(ref transfer);
			destination.Import(ref transfer);

			// Import any leftovers
			source.Import(ref transfer);
		}
	}
}
