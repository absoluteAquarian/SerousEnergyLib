namespace SerousEnergyLib {
	public static class ArrayFunctions {
		public static T[] Create1DArray<T>(T value, uint length) {
			T[] arr = new T[length];

			for (uint i = 0; i < length; i++)
				arr[i] = value;
			
			return arr;
		}
	}
}
