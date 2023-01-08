using System;
using Terraria.ModLoader;

namespace SerousEnergyLib.API {
	/// <summary>
	/// An object representing the progress toward completing an operation in a machine
	/// </summary>
	public sealed class CraftingProgress {
		private float _progress;

		/// <summary>
		/// The current progress.  This property will never be negative
		/// </summary>
		public float Progress {
			get => _progress;
			set => _progress = Math.Max(0, value);
		}

		/// <summary>
		/// How quickly <see cref="Step(float)"/> should affect <see cref="Progress"/>
		/// </summary>
		public StatModifier SpeedFactor = StatModifier.Default;

		/// <summary>
		/// Attempts to increase <see cref="Progress"/> by <paramref name="increment"/> amount.<br/>
		/// If <see cref="Progress"/> reaches 1 or higher, it is reset to 0.
		/// </summary>
		/// <returns>Whether the progress reached 100%</returns>
		public bool Step(float increment) {
			Progress += SpeedFactor.ApplyTo(increment);

			bool finished = _progress >= 1;

			if (finished)
				_progress = 0;

			return finished;
		}
	}
}
