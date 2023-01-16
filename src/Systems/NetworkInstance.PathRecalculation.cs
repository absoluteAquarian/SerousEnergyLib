using SerousEnergyLib.Pathfinding.Nodes;
using System.Collections.Generic;
using System;
using Terraria.DataStructures;

namespace SerousEnergyLib.Systems {
	partial class NetworkInstance {
		private int totalCoarsePaths = 0;
		private int coarseLeft = -1, coarseTop = -1, coarseRight = -1, coarseBottom = -1;

		private readonly Queue<Point16> queue = new();

		private bool recalculating;

		/// <summary>
		/// Completely recalculates the paths for this network instance.<br/>
		/// Calling this method is <b>NOT</b> recommended when adding/removing entries from the network.  Use the appropriate methods for those instead.
		/// </summary>
		/// <param name="start">The first tile to process when recalculating the paths</param>
		public void Recalculate(Point16 start) {
			if (disposed)
				throw new ObjectDisposedException("this");

			Reset();

			if (!IsValidTile(start.X, start.Y))
				return;

			HashSet<Point16> walked = new();

			queue.Clear();
			queue.Enqueue(start);

			int left = 65535;
			int right = -1;
			int top = 65535;
			int bottom = -1;

			recalculating = true;

			while (queue.TryDequeue(out Point16 location)) {
				if (!walked.Add(location))
					continue;

				int x = location.X, y = location.Y;

				AddEntry(location);

				// Calculate the new area of tiles that contains this entire network
				if (x < left)
					left = x;
				if (y < top)
					top = y;
				if (x > right)
					right = x;
				if (y > bottom)
					bottom = y;
			}

			coarseLeft = left / CoarseNode.Stride;
			coarseTop = top / CoarseNode.Stride;
			coarseRight = right / CoarseNode.Stride;
			coarseBottom = bottom / CoarseNode.Stride;

			totalCoarsePaths = 1;

			foreach (var coarse in coarsePath.Keys)
				UpdateCoarseNode(coarse);

			OnRecalculate(nodes);

			recalculating = false;
		}

		/// <summary>
		/// This method is called after the network has recalculated its paths
		/// </summary>
		/// <param name="nodes">The collection of entries in the network, indexed by tile position</param>
		public virtual void OnRecalculate(IReadOnlyDictionary<Point16, NetworkInstanceNode> nodes) { }
	}
}
