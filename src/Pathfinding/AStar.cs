using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.DataStructures;

namespace SerousEnergyLib.Pathfinding {
	public delegate TEntry GenerateEntryDelegate<TEntry>(Point16 location, Point16 headingFrom) where TEntry : IAStarEntry;
	public delegate bool DoesEntryExistDelegate(Point16 location);
	public delegate bool IsInterEntryPathValid(Point16 from, Point16 to);

	/// <summary>
	/// Performs an A* pathfinding algorithm
	/// </summary>
	public class AStar<TEntry> where TEntry : IAStarEntry {
		private readonly PriorityQueue<TEntry> activeMaze;

		private readonly GenerateEntryDelegate<TEntry> Generate;
		private readonly DoesEntryExistDelegate EntryExists;
		private readonly IsInterEntryPathValid PathToNextTileIsValid;

		public AStar(int capacity, GenerateEntryDelegate<TEntry> generateEntry, DoesEntryExistDelegate entryExists, IsInterEntryPathValid pathIsValid) {
			ArgumentNullException.ThrowIfNull(generateEntry);
			ArgumentNullException.ThrowIfNull(entryExists);
			ArgumentNullException.ThrowIfNull(pathIsValid);

			activeMaze = new PriorityQueue<TEntry>(capacity, AStarEntryComparer<TEntry>.Instance);

			Generate = generateEntry;
			EntryExists = entryExists;
			PathToNextTileIsValid = pathIsValid;
		}

		public List<Point16> GetPath(Point16 start, Point16 end) {
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

			TryGenerateWalkableEntry(parent, visited, possible, new Point16(-1, 0));
			TryGenerateWalkableEntry(parent, visited, possible, new Point16(0, -1));
			TryGenerateWalkableEntry(parent, visited, possible, new Point16(1, 0));
			TryGenerateWalkableEntry(parent, visited, possible, new Point16(0, 1));

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
	}
}
