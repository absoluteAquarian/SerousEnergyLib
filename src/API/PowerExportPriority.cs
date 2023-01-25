using SerousEnergyLib.API.Machines;

namespace SerousEnergyLib.API {
	/// <summary>
	/// A collection of constants dicating how an <see cref="IPowerGeneratorMachine"/> or <see cref="IPowerStorageMachine"/> should export power to adjacent networks
	/// </summary>
	public enum PowerExportPriority {
		/// <summary>
		/// Whichever network ends up first in the "adjacent networks" enumeration gets power exported to it first
		/// </summary>
		FirstComeFirstServe = 0,
		/// <summary>
		/// Whichever network ends up last in the "adjacent networks" enumeration gets power exported to it first
		/// </summary>
		LastComeFirstServe = 1,
		/// <summary>
		/// Whichever network has the lowest current capacity in the "adjacent networks" enumeration gets power exported to it first
		/// </summary>
		LowestPower = 2,
		/// <summary>
		/// Whichever network has the highest current capacity in the "adjacent networks" enumeration gets power exported to it first
		/// </summary>
		HighestPower = 3,
		/// <summary>
		/// The current capacity in the machine is split for each network in the "adjacent networks" enumeration<br/>
		/// If a network doesn't completely consume its slice of the current capacity, the leftovers are split amongst the remaining networks until all networks in the "adjacent networks" enumeration are processed
		/// </summary>
		SplitEvenly = 4
	}
}
