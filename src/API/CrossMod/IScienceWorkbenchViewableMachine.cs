namespace SerousEnergyLib.API.CrossMod {
	/// <summary>
	/// An interface representing a machine that can have its information be displayed in the Machine Workbench machine from Terran Automation
	/// </summary>
	public interface IScienceWorkbenchViewableMachine {
		/// <summary>
		/// Return an instance of this machine type's display for the Machine Workbench here<br/>
		/// The registry instance is treated as a singleton
		/// </summary>
		MachineWorkbenchRegistry GetRegistry();
	}
}
