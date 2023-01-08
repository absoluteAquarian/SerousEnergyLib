using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using SerousEnergyLib.API.Sounds;
using Terraria.Audio;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface representing a machine that can emit sounds
	/// </summary>
	public interface ISoundEmittingMachine : IMachine {
		/// <summary>
		/// This method runs when this machine receives a sound packet on a multiplayer client which orders it to play a sound
		/// </summary>
		/// <param name="soundSlot">The return value from calling <see cref="SoundEngine.PlaySound(in SoundStyle, Vector2?)"/></param>
		/// <param name="id">The identifier for the sound in <see cref="MachineSounds"/></param>
		void OnSoundPlayingPacketRecieved(in SlotId soundSlot, int id);

		/// <summary>
		/// This method runs when this machine receives a sound packet on a multiplayer client which orders it to stop playing a sound
		/// </summary>
		/// <param name="id">The registered sound ID for the sound to stop</param>
		/// <param name="extraInformation">Extra information sent by the sound packet</param>
		void OnSoundStopPacketReceived(int id, int extraInformation);
	}
}
