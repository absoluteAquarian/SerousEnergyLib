namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface representing an <see cref="IPoweredMachine"/> that cannot consume power
	/// </summary>
	public interface IPowerStorageMachine : IPoweredMachine {
		double IPoweredMachine.GetPowerConsumption(double ticks) => 0;
	}
}
