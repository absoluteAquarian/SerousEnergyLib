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

namespace SerousEnergyLib.Systems {
	#pragma warning disable CS1591
	partial class NetworkInstance {
		public void SaveData(TagCompound tag) {
			if (disposed)
				throw new ObjectDisposedException("this");

			tag["filter"] = (byte)Filter;

			// Save the "start" of the network so that the logic is forced to recalculate it when loading the world
			if (nodes.Count > 0)
				tag["start"] = FirstNode;

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

			if (tag.TryGet("start", out Point16 start))
				Recalculate(start);

			if (tag.GetCompound("extra") is TagCompound extra)
				LoadExtraData(extra);
		}

		/// <summary>
		/// Load additional data from <paramref name="tag"/> to this network here
		/// </summary>
		protected virtual void LoadExtraData(TagCompound tag) { }
	}
}
