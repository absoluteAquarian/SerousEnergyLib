namespace SerousEnergyLib.Pathfinding {
	/// <summary>
	/// An interface representing an entry in a <see cref="PriorityQueue{T}"/>
	/// </summary>
	public interface IPriorityNode<T> {
		/// <summary>
		/// Called when a <see cref="PriorityQueue{T}"/> updates an existing node entry.
		/// Update the data in <paramref name="replacement"/> according to the data in <paramref name="existing"/> here.
		/// </summary>
		/// <param name="existing">The existing node in the queue</param>
		/// <param name="replacement">The new node that will replace <paramref name="existing"/></param>
		void OnNodeUpdate(T existing, ref T replacement) { }
	}
}
