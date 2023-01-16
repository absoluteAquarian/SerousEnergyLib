using Terraria.DataStructures;

namespace SerousEnergyLib.Systems {
	#pragma warning disable CS1591
	/// <summary>
	/// A structure representing an entry in a <see cref="NetworkInstance"/>
	/// </summary>
	public readonly struct NetworkInstanceNode {
		public readonly Point16 location;
		public readonly Point16[] adjacent;

		internal NetworkInstanceNode(Point16 location, Point16[] adjacent) {
			this.location = location;
			this.adjacent = adjacent;
		}

		public override bool Equals(object obj) => obj is NetworkInstanceNode node && location == node.location;

		public override int GetHashCode() => location.GetHashCode();

		public static bool operator ==(NetworkInstanceNode left, NetworkInstanceNode right) => left.Equals(right);

		public static bool operator !=(NetworkInstanceNode left, NetworkInstanceNode right) => !(left == right);
	}
}
