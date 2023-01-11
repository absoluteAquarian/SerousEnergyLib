using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using SerousEnergyLib.API.Sounds;
using SerousEnergyLib.Systems;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

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
		/// <param name="extraInformation">Extra information sent by the sound packet</param>
		void OnSoundPlayingPacketReceived(in SlotId soundSlot, int id, int extraInformation);

		/// <summary>
		/// This method runs when this machine receives a sound packet on a multipleyr client which orders it to update an existing sound
		/// </summary>
		/// <param name="id">The registered sound ID for the sound to update</param>
		/// <param name="data">The sound information sent by the sound packet</param>
		/// <param name="mode">The mode used when sending the sound to this client</param>
		/// <param name="location">The world coordinates where to sound is playing</param>
		/// <param name="extraInformation">Extra information sent by the sound packet</param>
		void OnSoundUpdatePacketReceived(int id, SoundStyle data, NetcodeSoundMode mode, Vector2? location, int extraInformation);

		/// <summary>
		/// This method runs when this machine receives a sound packet on a multiplayer client which orders it to stop playing a sound
		/// </summary>
		/// <param name="id">The registered sound ID for the sound to stop</param>
		/// <param name="extraInformation">Extra information sent by the sound packet</param>
		void OnSoundStopPacketReceived(int id, int extraInformation);

		/// <summary>
		/// This method plays/updates a sound (when playing singleplayer) or sends the appropriate packets to clients (when playing multiplayer)
		/// </summary>
		/// <param name="emitter">The machine that emitted the sound</param>
		/// <param name="style">The sound information to play/update or send to clients</param>
		/// <param name="mode">The mode used when sending the sound to clients</param>
		/// <param name="clientSoundSlot">The variable used to store the played sound on clients</param>
		/// <param name="serverPlayingFlag">The variable used to track if the sound is playing on the server</param>
		/// <param name="location">The location to play the sound at.  Defaults to <see langword="null"/>, which indicates a "directionless" sound</param>
		/// <param name="extraInformation">Extra information to sent to clients</param>
		/// <param name="allowClientsideSoundMuting">Whether clients should be able to mute the sound while their game window is inactive</param>
		public static void EmitSound<T>(T emitter, SoundStyle style, NetcodeSoundMode mode, ref SlotId clientSoundSlot, ref bool serverPlayingFlag, Vector2? location = null, int extraInformation = 0, bool allowClientsideSoundMuting = true) where T : ModTileEntity, IMachine, ISoundEmittingMachine {
			if (!Main.dedServ) {
				style = AdjustSoundForMuting(style, allowClientsideSoundMuting);

				if (!clientSoundSlot.IsValid || !SoundEngine.TryGetActiveSound(clientSoundSlot, out var activeSound))
					clientSoundSlot = SoundEngine.PlaySound(style, location);
				else
					activeSound.Position = location;
			} else {
				if (!serverPlayingFlag) {
					serverPlayingFlag = true;

					Netcode.SendSoundToClients(emitter, style, mode, location, extraInformation, allowClientsideSoundMuting);
				} else
					Netcode.SendSoundUpdateToClients(emitter, style, mode, location, extraInformation, allowClientsideSoundMuting);
			}
		}

		internal static SoundStyle AdjustSoundForMuting(SoundStyle style, bool allowClientsideSoundMuting) {
			// Allow singleplayer to mute the sound when the game window isn't active
			float volumeAdjustment = Main.dedServ || !allowClientsideSoundMuting || Main.instance.IsActive ? 1f : 0f;

			return style.WithVolumeScale(volumeAdjustment);
		}

		/// <summary>
		/// This method stops a sound (when playing singlplayer) or sends the appropriate packet to clients (when playing multiplayer)
		/// </summary>
		/// <param name="emitter">The machine that emitted the sound</param>
		/// <param name="soundID">The registered sound ID created by <see cref="MachineSounds.RegisterSound(in SoundStyle)"/></param>
		/// <param name="clientSoundSlot">The variable used to store the played sound on clients</param>
		/// <param name="serverPlayingFlag">The variable used to track if the sound is playing on the server</param>
		/// <param name="extraInformation">Extra information to sent to clients</param>
		public static void StopSound<T>(T emitter, int soundID, ref SlotId clientSoundSlot, ref bool serverPlayingFlag, int extraInformation = 0) where T : ModTileEntity, IMachine, ISoundEmittingMachine {
			if (!Main.dedServ && SoundEngine.TryGetActiveSound(clientSoundSlot, out var activeSound)) {
				activeSound.Stop();
				clientSoundSlot = SlotId.Invalid;
			} else {
				Netcode.SendSoundStopToClients(emitter, soundID, extraInformation);
				serverPlayingFlag = false;
			}
		}
	}
}
