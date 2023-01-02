using SerousEnergyLib.API.Fluid;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface containing methods used by machines that can store fluids
	/// </summary>
	public interface IFluidMachine : IMachine {
		/// <summary>
		/// The fluid storages within this machine
		/// </summary>
		FluidStorage[] FluidStorage { get; protected set; }

		/// <summary>
		/// Whether the fluid pipe at position (<paramref name="pipeX"/>, <paramref name="pipeY"/>) can merge with this machine's sub-tile at position (<paramref name="machineX"/>, <paramref name="machineY"/>)
		/// </summary>
		/// <param name="pipeX">The tile X-coordinate for the fluid pipe</param>
		/// <param name="pipeY">The tile Y-coordinate for the fluid pipe</param>
		/// <param name="machineX">The tile X-coordinate for the machine sub-tile</param>
		/// <param name="machineY">The tile Y-coordinate for the machine sub-tile</param>
		bool CanMergeWithFluidPipe(int pipeX, int pipeY, int machineX, int machineY);
	}
}
