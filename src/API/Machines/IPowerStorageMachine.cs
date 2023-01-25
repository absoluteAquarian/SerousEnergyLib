namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface representing an <see cref="IPoweredMachine"/> that can export and import power, but does not consume power by default<br/>
	/// <b>NOTE:</b> This interface should NOT be used with <see cref="IPowerGeneratorMachine"/>
	/// </summary>
	public interface IPowerStorageMachine : IPoweredMachine {
		double IPoweredMachine.GetPowerConsumption(double ticks) => 0;

		/// <summary>
		/// The mode used when attempting to export power from this machine
		/// </summary>
		PowerExportPriority StorageExportMode { get; set; }
	}
}
