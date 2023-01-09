namespace SerousEnergyLib.API {
	/// <summary>
	/// A structure representing a duration of time, measured in game ticks
	/// </summary>
	public readonly struct Ticks {
		#pragma warning disable CS1591
		public readonly int ticks;

		public Ticks(int ticks) {
			this.ticks = ticks;
		}

		public static Ticks FromSeconds(int seconds) => new(seconds * 60);

		public static Ticks FromSeconds(float seconds) => new((int)(seconds * 60));

		public static Ticks FromSeconds(double seconds) => new((int)(seconds * 60));
	}
}
