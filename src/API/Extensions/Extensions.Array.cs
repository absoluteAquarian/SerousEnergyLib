using System;

namespace SerousEnergyLib.API {
	partial class Extensions {
		/// <summary>
		/// Fills <paramref name="array"/> with <paramref name="defaultValue"/>, then returns it
		/// </summary>
		public static T[] Populate<T>(this T[] array, T defaultValue) where T : struct {
			if (array is not { Length: > 0 })
				return array;

			for (int i = 0; i < array.Length; i++)
				array[i] = defaultValue;

			return array;
		}

		/// <summary>
		/// Fills <paramref name="array"/> with the value returned by <paramref name="defaultValue"/>, then returns it
		/// </summary>
		public static T[] Populate<T>(this T[] array, Func<T> defaultValue) {
			if (array is not { Length: > 0 })
				return array;

			for (int i = 0; i < array.Length; i++)
				array[i] = defaultValue();

			return array;
		}

		/// <summary>
		/// Fills <paramref name="array"/> with the value returned by <paramref name="defaultValue"/>, then returns it
		/// </summary>
		public static T[] Populate<T>(this T[] array, Func<int, T> defaultValue) {
			if (array is not { Length: > 0 })
				return array;

			for (int i = 0; i < array.Length; i++)
				array[i] = defaultValue(i);

			return array;
		}
	}
}
