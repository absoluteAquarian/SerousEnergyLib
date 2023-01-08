using System.IO;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface representing a set of functions called when performing a reduced sync of a machine
	/// </summary>
	public interface IReducedNetcodeMachine : IMachine {
		/// <summary>
		/// Write data that will typically be synced very frequently here
		/// </summary>
		void ReducedNetSend(BinaryWriter writer);

		/// <summary>
		/// Read data that is synced very frequently here
		/// </summary>
		void ReducedNetReceive(BinaryReader reader);
	}
}
