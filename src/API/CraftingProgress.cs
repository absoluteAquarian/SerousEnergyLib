using System;
using System.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

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

		#pragma warning disable CS1591
		public void SaveData(TagCompound tag) {
			tag["current"] = _progress;
			tag["speed"] = new TagCompound() {
				["add"] = SpeedFactor.Additive,
				["mult"] = SpeedFactor.Multiplicative,
				["flat"] = SpeedFactor.Flat,
				["base"] = SpeedFactor.Base
			};
		}

		public void LoadData(TagCompound tag) {
			_progress = tag.GetFloat("current");

			if (tag.GetCompound("speed") is TagCompound speed) {
				float add = speed.GetFloat("add");
				float mult = speed.GetFloat("mult");
				float flat = speed.GetFloat("flat");
				float @base = speed.GetFloat("base");

				SpeedFactor = new StatModifier(add, mult, flat, @base);
			} else
				SpeedFactor = StatModifier.Default;
		}

		public void Send(BinaryWriter writer) {
			var speed = SpeedFactor;

			writer.Write(Progress);
			writer.Write(speed.Additive);
			writer.Write(speed.Multiplicative);
			writer.Write(speed.Flat);
			writer.Write(speed.Base);
		}

		public void Receive(BinaryReader reader) {
			float progress = reader.ReadSingle();
			float speedAdditive = reader.ReadSingle();
			float speedMultiplicative = reader.ReadSingle();
			float speedFlat = reader.ReadSingle();
			float speedBase = reader.ReadSingle();

			Progress = progress;
			SpeedFactor = new StatModifier(speedAdditive, speedMultiplicative, speedFlat, speedBase);
		}
	}
}
