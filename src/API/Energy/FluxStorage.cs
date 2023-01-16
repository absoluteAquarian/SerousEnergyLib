using System.IO;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Energy {
	/// <summary>
	/// An object representing storage of power
	/// </summary>
	public sealed class FluxStorage {
		/// <summary>
		/// The current amount of <see cref="TerraFlux"/> that's stored
		/// </summary>
		public TerraFlux CurrentCapacity;

		/// <summary>
		/// The maximum amount of <see cref="TerraFlux"/> that can be stored
		/// </summary>
		public TerraFlux MaxCapacity;

		/// <summary>
		/// The default maximum amount of <see cref="TerraFlux"/> that can be stored<br/>
		/// This property can be used to reset <see cref="MaxCapacity"/> when components that increase it are removed
		/// </summary>
		public TerraFlux BaseMaxCapacity { get; private set; }

		#pragma warning disable CS1591
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

		/// <summary>
		/// Imports at most <paramref name="amount"/> power into this storage.  Any leftover power will be contained in <paramref name="amount"/>
		/// </summary>
		/// <param name="amount">The amount of power to import</param>
		public void Import(ref TerraFlux amount) {
			if (amount <= TerraFlux.Zero)
				return;

			// Cannot import power
			if (IsFull)
				return;

			TerraFlux import = amount;

			if (CurrentCapacity + import > MaxCapacity)
				import = MaxCapacity - CurrentCapacity;

			CurrentCapacity += import;
			amount -= import;
		}

		/// <summary>
		/// Imports at most <paramref name="import"/> power from <paramref name="source"/> into this storage.
		/// </summary>
		/// <param name="source">The storage to export power from</param>
		/// <param name="import">The amount of power to import</param>
		public void ImportFrom(FluxStorage source, TerraFlux import) => Transfer(source, this, import);

		/// <summary>
		/// Exports at most <paramref name="amount"/> power from this storage.  If there isn't enough power to fully export <paramref name="amount"/>, it will be reduced accordingly.
		/// </summary>
		/// <param name="amount">The amount of power to export</param>
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

		/// <summary>
		/// Exports at most <paramref name="export"/> power from this storage into <paramref name="destination"/>.
		/// </summary>
		/// <param name="destination">The storage to import power into</param>
		/// <param name="export">The amount of power to export</param>
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

		public void Send(BinaryWriter writer) {
			writer.Write(CurrentCapacity);
			writer.Write(MaxCapacity);
			writer.Write(BaseMaxCapacity);
		}

		public void Receive(BinaryReader reader) {
			CurrentCapacity = reader.ReadFlux();
			MaxCapacity = reader.ReadFlux();
			BaseMaxCapacity = reader.ReadFlux();
		}
	}
}
