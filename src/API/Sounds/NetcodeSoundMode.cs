using System;

namespace SerousEnergyLib.API.Sounds {
	/// <summary>
	/// A set of identifiers for specifing what kind of data should be sent when sending an order to play a sound to a client
	/// </summary>
	[Flags]
	public enum NetcodeSoundMode : byte {
		/// <summary>
		/// Only the registered ID for the sound is sent
		/// </summary>
		None = 0,
		/// <summary>
		/// The emit location for the sound is sent
		/// </summary>
		SendPosition = 0x1,
		/// <summary>
		/// The volume for the sound is sent
		/// </summary>
		SendVolume = 0x2,
		/// <summary>
		/// The pitch and pitch variance are sent
		/// </summary>
		SendPitch = 0x4
	}
}
