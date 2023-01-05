using Microsoft.Xna.Framework;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Items;
using SerousEnergyLib.Systems;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
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
		/// Return an instance of a <see cref="ModTileEntity"/> that inherits from <see cref="IMachine"/> here
		/// </summary>
		IMachine GetMachineEntity();

		/// <summary>
		/// The ID of the item to drop when killing this machine
		/// </summary>
		int MachineItem { get; }

		/// <summary>
		/// Modifies <see cref="TileObjectData.newTile"/> and other properties in <paramref name="machine"/> to contain the default values for machines.<br/>
		/// <see cref="TileObjectData.addTile(int)"/> is purosefully not called so that <see cref="TileObjectData.newTile"/> can be further modified.
		/// </summary>
		/// <param name="machine">The machine to retrieve information from</param>
		/// <param name="mapColor">The color of this machine's tiles on the minimap</param>
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

		/// <summary>
		/// This method executes the standard logic for placing a machine's tile entity in the world
		/// </summary>
		/// <param name="machine">The machine to retrieve information from</param>
		/// <param name="i">The tile X-coordinate that the machine is being placed at</param>
		/// <param name="j">The tile Y=coordinate that the machine is being placed at</param>
		/// <param name="item">The item used to place the machine</param>
		public static void DefaultPlaceInWorld(IMachineTile machine, int i, int j, Item item) {
			if (machine is not ModTile)
				throw new ArgumentException("IMachineTile parameter was not a ModTile", nameof(machine));

			if (item.ModItem is not BaseMachineItem mItem)
				throw new ArgumentException("Item instance was not a BaseMachineItem", nameof(item));

			Point16 entityLocation = TileFunctions.GetTopLeftTileInMultitile(i, j);

			var entity = IMachine.PlaceInWorld(machine.GetMachineEntity(), entityLocation);

			if (mItem.MachineData is not null)
				entity.LoadData(mItem.MachineData);

			Netcode.SyncMachinePlacement(entity.Type, entityLocation);
		}

		/// <summary>
		/// This method executes the standard logic for destroying a machine multitile, which encompasses dropping its item and destroying its tile entity
		/// </summary>
		/// <param name="machine"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="dropItem"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		public static void DefaultKillMultitile(IMachineTile machine, int x, int y, bool dropItem = true) {
			if (machine is not ModTile)
				throw new ArgumentException("IMachineTile parameter was not a ModTile", nameof(machine));
			
			Point16 location = new Point16(x, y);

			TagCompound machineData = IMachine.RemoveFromWorld(location);

			machine.GetMachineDimensions(out uint width, out uint height);

			int drop = Item.NewItem(new EntitySource_TileBreak(x, y), x * 16, y * 16, (int)(width * 16), (int)(height * 16), machine.MachineItem, noBroadcast: true);
			if (drop < Main.maxItems) {
				// Save data onto the item
				BaseMachineItem machineItem = Main.item[drop].ModItem as BaseMachineItem
					?? throw new InvalidOperationException("IMachineTile.MachineItem did not refer to a valid BaseMachineItem item ID");

				machineItem.MachineData = machineData;

				// Sync the item for mp
				if (Main.netMode != NetmodeID.SinglePlayer)
					NetMessage.SendData(MessageID.SyncItem, ignoreClient: Main.netMode == NetmodeID.MultiplayerClient ? Main.myPlayer : -1, number: drop, number2: 1);
			}
		}
	}
}
