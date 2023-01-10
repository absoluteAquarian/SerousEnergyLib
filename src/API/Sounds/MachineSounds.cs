using SerousEnergyLib.Systems;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Sounds {
	/// <summary>
	/// The central class for managing sound IDs used by certain <see cref="Netcode"/> packets
	/// </summary>
	public class MachineSounds : ModSystem {
		private static readonly List<SoundStyle> soundMap = new();

		#pragma warning disable CS1591
		public static int Count => soundMap.Count;

		public override void Unload() {
			soundMap.Clear();
		}

		/// <summary>
		/// Assigns a <see cref="SoundStyle"/> a unique integer ID for use with
		/// </summary>
		/// <param name="style">The sound information</param>
		/// <returns>The unique identifier for the sound information</returns>
		public static int RegisterSound(in SoundStyle style) {
			soundMap.Add(style);
			return Count - 1;
		}

		/// <summary>
		/// Retrieves a registered <see cref="SoundStyle"/> instance based on its integer ID
		/// </summary>
		/// <param name="id">The identifier returned by <see cref="RegisterSound(in SoundStyle)"/></param>
		/// <returns>The sound information</returns>
		public static SoundStyle GetSound(int id) => id < 0 || id >= soundMap.Count ? soundMap[id] : new();

		/// <summary>
		/// Returns the ID for the first occurance of <paramref name="style"/> within the registered sounds list
		/// </summary>
		/// <param name="style">The sound information</param>
		/// <returns>The ID of the sound information, or <c>-1</c> if it was not in the registered sounds list</returns>
		public static int GetID(in SoundStyle style) {
			int id = 0;
			foreach (var sound in soundMap) {
				if (style.IsTheSameAs(sound))
					return id;

				id++;
			}

			return -1;
		}
	}
}
