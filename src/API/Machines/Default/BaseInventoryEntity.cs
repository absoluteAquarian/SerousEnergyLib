using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.API.Upgrades;
using SerousEnergyLib.Items;
using SerousEnergyLib.Systems.Networks;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Machines.Default {
	/// <summary>
	/// A default implementation of <see cref="IMachine"/> and <see cref="IInventoryMachine"/>
	/// </summary>
	public abstract class BaseInventoryEntity : ModTileEntity, IMachine, IInventoryMachine {
		#pragma warning disable CS1591
		public abstract int MachineTile { get; }

		public abstract BaseMachineUI MachineUI { get; }
		
		/// <inheritdoc cref="IMachine.Upgrades"/>
		public List<BaseUpgradeItem> Upgrades { get; set; }

		/// <inheritdoc cref="IInventoryMachine.Inventory"/>
		public Item[] Inventory { get; set; }

		public abstract int DefaultInventoryCapacity { get; }

		/// <summary>
		/// Whether this entity instance is a clone used for item tooltips
		/// </summary>
		public bool IsDummyInstance => ID == -1;

		/// <inheritdoc cref="IMachine.CanUpgradeApply(BaseUpgrade)"/>
		public virtual bool CanUpgradeApply(BaseUpgrade upgrade) => true;

		public override void Update() {
			IMachine.Update(this);
			IInventoryMachine.Update(this);
		}

		public override bool IsTileValidForEntity(int x, int y) => IMachine.IsTileValid(this, x, y);

		/// <inheritdoc cref="IInventoryMachine.CanMergeWithItemPipe(int, int, int, int)"/>
		public virtual bool CanMergeWithItemPipe(int pipeX, int pipeY, int machineX, int machineY) => true;

		public abstract int[] GetExportSlots();

		/// <inheritdoc cref="IInventoryMachine.CanExportItemAtSlot(int, Point16)"/>
		public virtual bool CanExportItemAtSlot(int slot, Point16 subtile) => true;

		/// <inheritdoc cref="IInventoryMachine.ExportItemAtSlot(ItemNetwork, int, Point16, ref int, bool, out InventoryExtractionResult)"/>
		public virtual bool ExportItemAtSlot(ItemNetwork network, int slot, Point16 pathfindingStart, ref int extractCount, bool simulation, out InventoryExtractionResult result)
			=> IInventoryMachine.DefaultExportItemAtSlot(this, network, slot, pathfindingStart, ref extractCount, simulation, out result);

		public abstract int[] GetInputSlots();

		/// <inheritdoc cref="CanImportItemAtSlot(Item, Point16, int, out int)"/>
		public virtual bool CanImportItemAtSlot(Item import, Point16 subtile, int slot, out int stackImported)
			=> IInventoryMachine.DefaultCanImportItemAtSlot(this, import, subtile, slot, out stackImported);

		/// <inheritdoc cref="ImportItemAtSlot(Item, int)"/>
		public virtual void ImportItemAtSlot(Item import, int slot) => IInventoryMachine.DefaultImportItemAtSlot(this, import, slot);

		public abstract int[] GetInputSlotsForRecipes();

		public override void SaveData(TagCompound tag) {
			IMachine.SaveData(this, tag);
			IInventoryMachine.SaveData(this, tag);
		}

		public override void LoadData(TagCompound tag) {
			IMachine.LoadData(this, tag);
			IInventoryMachine.LoadData(this, tag);
		}

		public override void NetSend(BinaryWriter writer) {
			IMachine.NetSend(this, writer);
			IInventoryMachine.NetSend(this, writer);
		}

		public override void NetReceive(BinaryReader reader) {
			IMachine.NetReceive(this, reader);
			IInventoryMachine.NetReceive(this, reader);
		}
	}
}
