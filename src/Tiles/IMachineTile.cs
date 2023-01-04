using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace SerousEnergyLib.Tiles {
	/// <summary>
	/// The base interface used by all machine tiles
	/// </summary>
	public interface IMachineTile {
		/// <summary>
		/// Return the dimensions of this machine, measured in tiles
		/// </summary>
		/// <param name="width">The width in tiles</param>
		/// <param name="height">The height in tiles</param>
		void GetMachineDimensions(out uint width, out uint height);

		/// <summary>
		/// Return the name that should appear when hovering over the machine on the minimap, or <see langword="null"/> to use the names from loaded HJSON files.
		/// </summary>
		string GetMachineMapEntryName();

		/// <summary>
		/// Modifies <see cref="TileObjectData.newTile"/> and other properties in <paramref name="machine"/> to contain the default values for machines.<br/>
		/// <see cref="TileObjectData.addTile(int)"/> is purosefully not called so that <see cref="TileObjectData.newTile"/> can be further modified.
		/// </summary>
		/// <param name="machine"></param>
		public static void MultitileDefaults(IMachineTile machine, Color mapColor) {
			if (machine is not ModTile tile)
				throw new ArgumentException("IMachineTile parameter was not a ModTile", nameof(machine));

			machine.GetMachineDimensions(out uint width, out uint height);

			Main.tileNoAttach[tile.Type] = true;
			Main.tileFrameImportant[tile.Type] = true;

			TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidBottom, (int)width, 0);
			TileObjectData.newTile.CoordinateHeights = ArrayFunctions.Create1DArray(16, height);
			TileObjectData.newTile.CoordinateWidth = 16;
			TileObjectData.newTile.Height = (int)height;
			TileObjectData.newTile.Width = (int)width;
			TileObjectData.newTile.CoordinatePadding = 2;
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.newTile.WaterDeath = false;
			TileObjectData.newTile.LavaPlacement = LiquidPlacement.NotAllowed;
			TileObjectData.newTile.WaterPlacement = LiquidPlacement.NotAllowed;
			TileObjectData.newTile.Origin = new Point16((int)width / 2, (int)height - 1);
			TileObjectData.newTile.UsesCustomCanPlace = true;

			tile.AddMapEntry(mapColor, tile.CreateMapEntryName(machine.GetMachineMapEntryName()));

			tile.MineResist = 3f;
			// Metal sound
			tile.HitSound = SoundID.Tink;
		}
	}
}
