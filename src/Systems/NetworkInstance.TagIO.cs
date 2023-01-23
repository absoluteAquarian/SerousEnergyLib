using SerousEnergyLib.TileData;
using SerousEnergyLib.Tiles;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Terraria.ModLoader.IO;
using Terraria.ModLoader;
using Terraria;
using Terraria.DataStructures;
using SerousEnergyLib.Systems.Networks;
using SerousEnergyLib.Pathfinding.Nodes;

namespace SerousEnergyLib.Systems {
	#pragma warning disable CS1591
	partial class NetworkInstance {
		public const int SAVE_VERSION_Recalculate = 0;
		public const int SAVE_VERSION_FullNetwork = 1;

		public void SaveData(TagCompound tag) {
			if (disposed)
				throw new ObjectDisposedException("this");

			tag["filter"] = (byte)Filter;

			// Fluid networks must save their entire networks, due to the stored fluids making them potentially not mergeable
			// Node calculation code can't properly detect when this is the case
			if (this is not FluidNetwork) {
				tag["version"] = SAVE_VERSION_Recalculate;

				if (nodes.Count > 0)
					tag["start"] = FirstNode;
			} else {
				tag["version"] = SAVE_VERSION_FullNetwork;

				if (nodes.Count > 0) {
					// Nodes need to be saved, since they aren't going to be recalculated
					List<TagCompound> nodeTags = new();

					foreach (var node in nodes.Values) {
						TagCompound nodeTag = new() {
							["x"] = node.location.X,
							["y"] = node.location.Y,
							["adj"] = node.adjacent
						};

						nodeTags.Add(nodeTag);
					}

					tag["nodes"] = nodeTags;
				}
			}

			// Save the junction frames
			if (foundJunctions.Count > 0) {
				tag["junctions"] = foundJunctions
					.Select(static p => new TagCompound() {
						["x"] = p.X,
						["y"] = p.Y,
						["mode"] = Main.tile[p.X, p.Y].TileFrameX / 18
					})
					.ToList();
			}

			TagCompound extra = new();
			SaveExtraData(extra);
			tag["extra"] = extra;
		}

		/// <summary>
		/// Save additional data to this network here
		/// </summary>
		protected virtual void SaveExtraData(TagCompound tag) { }

		public void LoadData(TagCompound tag) {
			if (disposed)
				throw new ObjectDisposedException("this");

			byte filter = tag.GetByte("filter");

			if (filter != (byte)NetworkType.Items && filter != (byte)NetworkType.Fluids && filter != (byte)NetworkType.Power)
				throw new IOException("Invalid filter number: " + filter);

			Filter = (NetworkType)filter;

			// Junctions must be loaded before recalculating the path
			// This is to make sure that the tile frames load properly, etc.
			if (tag.TryGet("junctions", out List<TagCompound> junctionTags)) {
				foreach (var junctionTag in junctionTags) {
					if (!junctionTag.TryGet("x", out short x))
						continue;

					if (!junctionTag.TryGet("y", out short y))
						continue;

					if (!junctionTag.TryGet("mode", out int mode))
						continue;

					Tile tile = Main.tile[x, y];

					if (TileLoader.GetTile(tile.TileType) is not NetworkJunction)
						continue;

					foundJunctions.Add(new Point16(x, y));

					tile.TileFrameX = (short)(mode * 18);
					tile.TileFrameY = 0;
				}
			}

			int version = tag.GetInt("version");

			if (version == SAVE_VERSION_Recalculate) {
				if (tag.TryGet("start", out Point16 start))
					Recalculate(start);
			} else if (version == SAVE_VERSION_FullNetwork) {
				if (tag.TryGet("nodes", out List<TagCompound> list)) {
					nodes.Clear();
					coarsePath.Clear();

					foreach (var nodeTag in list) {
						if (!nodeTag.TryGet("x", out short x))
							continue;

						if (!nodeTag.TryGet("y", out short y))
							continue;

						if (!nodeTag.TryGet("adj", out Point16[] adjacent))
							continue;

						Point16 loc = new Point16(x, y);

						nodes[loc] = new NetworkInstanceNode(loc, adjacent);

						OnEntryAdded(loc);

						Point16 coarseLoc = loc / CoarseNode.Coarseness;

						if (!coarsePath.ContainsKey(coarseLoc))
							coarsePath[coarseLoc] = new CoarseNode();
					}

					// Recalculate the coarse path
					foreach (var coarse in coarsePath.Keys)
						UpdateCoarseNode(coarse);
				}
			}

			if (tag.GetCompound("extra") is TagCompound extra)
				LoadExtraData(extra);
		}

		/// <summary>
		/// Load additional data from <paramref name="tag"/> to this network here
		/// </summary>
		protected virtual void LoadExtraData(TagCompound tag) { }
	}
}
