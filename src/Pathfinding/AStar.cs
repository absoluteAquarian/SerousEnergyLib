using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.DataStructures;

namespace SerousEnergyLib.Pathfinding {
	#pragma warning disable CS1591
	public delegate TEntry GenerateEntryDelegate<TEntry>(Point16 location, Point16 headingFrom) where TEntry : IAStarEntry;
	public delegate bool DoesEntryExistDelegate(Point16 location);
	public delegate bool IsInterEntryPathValidDelegate(Point16 from, Point16 to);
	public delegate List<Point16> GetWalkableDirectionsDelegate(Point16 center, Point16 previous);

	/// <summary>
	/// Performs an A* pathfinding algorithm
	/// </summary>
	public class AStar<TEntry> : IDisposable where TEntry : IAStarEntry {
		private PriorityQueue<TEntry> activeMaze;

		private GenerateEntryDelegate<TEntry> Generate;
		private DoesEntryExistDelegate EntryExists;
		private IsInterEntryPathValidDelegate PathToNextTileIsValid;
		private GetWalkableDirectionsDelegate GetWalkableDirections;

		public AStar(int capacity, GenerateEntryDelegate<TEntry> generateEntry, DoesEntryExistDelegate entryExists, IsInterEntryPathValidDelegate pathIsValid, GetWalkableDirectionsDelegate getWalkableDirections) {
			ArgumentNullException.ThrowIfNull(generateEntry);
			ArgumentNullException.ThrowIfNull(entryExists);
			ArgumentNullException.ThrowIfNull(pathIsValid);
			ArgumentNullException.ThrowIfNull(getWalkableDirections);

			activeMaze = new PriorityQueue<TEntry>(capacity, AStarEntryComparer<TEntry>.Instance);

			Generate = generateEntry;
			EntryExists = entryExists;
			PathToNextTileIsValid = pathIsValid;
			GetWalkableDirections = getWalkableDirections;
		}

		/// <summary>
		/// Attempts to generate a path from <paramref name="start"/> to <paramref name="end"/>
		/// </summary>
		/// <param name="start">The starting tile coordinate</param>
		/// <param name="end">The final tile coordinate</param>
		/// <returns>A list of tile positions to traverse, or <see langword="null"/> if no path could be found</returns>
		/// <exception cref="ObjectDisposedException"/>
		public List<Point16> GetPath(Point16 start, Point16 end) {
			if (disposed)
				throw new ObjectDisposedException("this");

			if (!EntryExists(start) || !EntryExists(end))
				return null;

			if (start == end)
				return new List<Point16>() { start };

			HashSet<Point16> visitedMaze = new HashSet<Point16>();

			activeMaze.Clear();
			activeMaze.Push(Generate(start, Point16.NegativeOne));

			// Keep looping while there's still entries to check
			while (activeMaze.Count > 0) {
				TEntry check = activeMaze.Top;

				if (check.Location == end) {
					// Path found; construct it based on the entry parents
					List<TEntry> path = new() { check };

					while (check.Parent is TEntry parent) {
						path.Add(parent);
						check = parent;
					}

					return path.Select(e => e.Location).ToList();
				}

				// Path not found yet.  Check the surrounding entries
				visitedMaze.Add(check.Location);
				activeMaze.Pop();

				var walkables = GetWalkableEntries(visitedMaze, check, end);

				foreach (TEntry walkable in walkables) {
					// If this walkable entry is already in the active list, but the existing entry has a worse heuristic, replace it
					// Otherwise, just add the walkable entry to the active list
					int index = activeMaze.FindIndex(walkable);

					if (index >= 0) {
						//"activeCheck" will have the same position as "walkable", but it may not have the same heuristic
						TEntry activeCheck = activeMaze.GetHeapValueAt(index);

						if (activeCheck.Heuristic > walkable.Heuristic)
							activeMaze.UpdateElement(walkable);
					} else
						activeMaze.Push(walkable);
				}
			}

			// Could not find a path
			return null;
		}

		private List<TEntry> GetWalkableEntries(HashSet<Point16> visited, TEntry parent, Point16 target) {
			List<TEntry> possible = new ();

			var dirs = GetWalkableDirections(parent.Location, parent.Parent?.Location ?? Point16.NegativeOne);

			foreach (var dir in dirs)
				TryGenerateWalkableEntry(parent, visited, possible, dir);

			for (int i = 0; i < possible.Count; i++) {
				TEntry entry = possible[i];
				entry.TravelTime += parent.TravelTime;
				// How many tiles need to be iterated over to reach the target
				Point16 loc = entry.Location;
				entry.TileDistance = Math.Abs(target.X - loc.X) + Math.Abs(target.Y - loc.Y);
				possible[i] = entry;
			}

			return possible;
		}

		private void TryGenerateWalkableEntry(TEntry parent, HashSet<Point16> visited, List<TEntry> possible, Point16 offset) {
			Point16 orig = parent.Location;
			Point16 target = orig + offset;

			if (!visited.Contains(target) && EntryExists(target) && PathToNextTileIsValid(orig, target)) {
				TEntry entry = Generate(parent.Location + offset, parent.Location);
				entry.Parent = parent;
				possible.Add(entry);
			}
		}

		#region Implement IDisposable
		private bool disposed;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			if (disposed)
				return;

			disposed = true;

			if (disposing)
				activeMaze.Clear();

			activeMaze = null;
			Generate = null;
			EntryExists = null;
			PathToNextTileIsValid = null;
			GetWalkableDirections = null;
		}

		~AStar() => Dispose(false);
		#endregion
	}
}
