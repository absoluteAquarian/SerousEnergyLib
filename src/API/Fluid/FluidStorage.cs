namespace SerousEnergyLib.API.Fluid {
	public class FluidStorage {
		/// <summary>
		/// The <see cref="FluidTypeID"/> that this storage contains, or nothing when this property is set to <c>-1</c>
		/// </summary>
		public int FluidID = -1;

		public double CurrentCapacity;

		public double MaxCapacity;

		public double BaseMaxCapacity { get; private set; }

		public readonly int[] allowedFluidTypes;

		public FluidStorage(double max) {
			BaseMaxCapacity = MaxCapacity = max;

			allowedFluidTypes = null;
		}

		public FluidStorage(double max, params int[] allowedFluidTypes) {
			BaseMaxCapacity = MaxCapacity = max;

			this.allowedFluidTypes = allowedFluidTypes;
		}
	}
}
