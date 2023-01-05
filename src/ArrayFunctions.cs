namespace SerousEnergyLib {
	/// <summary>
	/// A helper class for array objects
	/// </summary>
	public static class ArrayFunctions {
		/// <summary>
		/// Creates a 1D array whose elements are all set to <paramref name="value"/>
		/// </summary>
		/// <param name="value">The value</param>
		/// <param name="length">The length of the array</param>
		public static T[] Create1DArray<T>(T value, uint length) {
			T[] arr = new T[length];

			for (uint i = 0; i < length; i++)
				arr[i] = value;
			
			return arr;
		}
	}
}
