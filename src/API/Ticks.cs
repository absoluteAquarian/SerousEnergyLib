using System;

namespace SerousEnergyLib.API {
	/// <summary>
	/// A structure representing a duration of time, measured in game ticks
	/// </summary>
	public readonly struct Ticks : IEquatable<Ticks>, IComparable<Ticks> {
		#pragma warning disable CS1591
		public readonly int ticks;

		public static readonly Ticks Zero = new Ticks(0);

		public Ticks(int ticks) {
			this.ticks = ticks;
		}

		public static Ticks FromSeconds(int seconds) => new(seconds * 60);

		public static Ticks FromSeconds(float seconds) => new((int)(seconds * 60));

		public static Ticks FromSeconds(double seconds) => new((int)(seconds * 60));

		public override bool Equals(object obj) => obj is Ticks ticks && Equals(ticks);

		public bool Equals(Ticks other) => ticks == other.ticks;

		public override int GetHashCode() => ticks;

		public int CompareTo(Ticks other) => ticks.CompareTo(other.ticks);

		public static bool operator ==(Ticks left, Ticks right) => left.Equals(right);

		public static bool operator !=(Ticks left, Ticks right) => !(left == right);

		public static bool operator <(Ticks left, Ticks right) => left.ticks < right.ticks;

		public static bool operator <=(Ticks left, Ticks right) => left.ticks <= right.ticks;

		public static bool operator >(Ticks left, Ticks right) => left.ticks > right.ticks;

		public static bool operator >=(Ticks left, Ticks right) => left.ticks >= right.ticks;

		public static bool operator <(Ticks left, int right) => left.ticks < right;

		public static bool operator <=(Ticks left, int right) => left.ticks <= right;

		public static bool operator >(Ticks left, int right) => left.ticks > right;

		public static bool operator >=(Ticks left, int right) => left.ticks >= right;

		public static bool operator <(int left, Ticks right) => left < right.ticks;

		public static bool operator <=(int left, Ticks right) => left <= right.ticks;

		public static bool operator >(int left, Ticks right) => left > right.ticks;

		public static bool operator >=(int left, Ticks right) => left >= right.ticks;
	}
}
