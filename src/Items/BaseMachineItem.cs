using SerousCommonLib.API;
using SerousEnergyLib.API.Energy;
using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Tiles;
using System.Collections.Generic;
using System.Reflection.Emit;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.Items {
	/// <summary>
	/// The base implementation for an item that can place a machine
	/// </summary>
	public abstract class BaseMachineItem : ModItem {
		private TagCompound machineData;
		/// <summary>
		/// The stored machine data in this item.<br/>
		/// <b>NOTE:</b> Writing to this property will recalulate <see cref="DataHash"/>
		/// </summary>
		public TagCompound MachineData {
			get => machineData;
			set {
				machineData = value;
				DataHash = IOHelper.ComputeDataHash(machineData);
			}
		}

		/// <summary>
		/// The data hash for <see cref="MachineData"/><br/>
		/// This property serves no practical purpose and is just for aesthetics
		/// </summary>
		public int DataHash { get; private set; }

		/// <summary>
		/// The tile ID of the machine to place
		/// </summary>
		public abstract int MachineTile { get; }

		/// <summary>
		/// If this item's machine is an <see cref="IPoweredMachine"/>, this property indicates whether the tooltip line should be "per game tick" or "per operation".<br/>
		/// Defaults to <see langword="true"/>.
		/// </summary>
		public virtual bool MachineUsesEnergyPerTick => true;

		/// <summary>
		/// If this item's machine is an <see cref="IPowerGeneratorMachine"/>, this property indicates whether the tooltip line should be "per game tick" or "per operation".<br/>
		/// Defaults to <see langword="true"/>.
		/// </summary>
		public virtual bool MachineGeneratesEnergyPerTick => true;

		#pragma warning disable CS1591
		public sealed override void SetDefaults() {
			Item.maxStack = 1;
			SafeSetDefaults();
			Item.DamageType = DamageClass.Default;
			Item.useTime = 10;
			Item.useAnimation = 15;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.createTile = MachineTile;
			Item.consumable = true;
			Item.autoReuse = true;
			Item.useTurn = true;
		}

		/// <inheritdoc cref="SetDefaults"/>
		public virtual void SafeSetDefaults() { }

		/// <summary>
		/// Gets the energy usage as a string
		/// </summary>
		/// <param name="perGameTick">Whether the rates are displayed as "X/gt" and "X/s" (<see langword="true"/>) or "X/operation" (<see langword="false"/>)</param>
		protected string GetEnergyUsageString(bool perGameTick) {
			if (TileLoader.GetTile(MachineTile) is not IMachineTile machine)
				return null;

			IMachine singleton = machine.GetMachineEntity();

			if (singleton is not IPoweredMachine || singleton is not ModTileEntity entity)
				return null;

			// Make a dummy instance
			var clone = ModTileEntity.ConstructFromBase(entity);
			var powered = clone as IPoweredMachine;

			if (EnergyConversions.Get(powered.EnergyID) is not EnergyTypeID energyType)
				return null;

			IMachine.Update(powered);
			IPoweredMachine.Update(powered);

			if (powered is IInventoryMachine inventory)
				IInventoryMachine.Update(inventory);

			if (MachineData is not null)
				clone.LoadData(MachineData);

			// Inform the dummy object that it is indeed a dummy instance
			// Things that rely on position (e.g. solar panels) can use this to ignore the position
			clone.ID = -1;

			var consumed = IPoweredMachine.GetPowerConsumptionWithUpgrades(powered, 1);

			string shortUnit = energyType.ShortName;

			return perGameTick
				? Language.GetTextValue("Mods.SerousEnergyLib.PowerUsageTooltips.PerGameTick", consumed, shortUnit, consumed * 60)
				: Language.GetTextValue("Mods.SerousEnergyLib.PowerUsageTooltips.PerOperation", consumed, shortUnit);
		}

		/// <summary>
		/// Gets the energy generation as a string
		/// </summary>
		/// <param name="perGameTick">Whether the rates are displayed as "X/gt" and "X/s" (<see langword="true"/>) or "X/operation" (<see langword="false"/>)</param>
		protected string GetEnergyGenerationString(bool perGameTick) {
			if (TileLoader.GetTile(MachineTile) is not IMachineTile machine)
				return null;

			IMachine singleton = machine.GetMachineEntity();

			if (singleton is not IPowerGeneratorMachine || singleton is not ModTileEntity entity)
				return null;

			// Make a dummy instance
			var clone = ModTileEntity.ConstructFromBase(entity);
			var generator = clone as IPowerGeneratorMachine;

			if (EnergyConversions.Get(generator.EnergyID) is not EnergyTypeID energyType)
				return null;

			IMachine.Update(generator);
			IPoweredMachine.Update(generator);

			if (generator is IInventoryMachine inventory)
				IInventoryMachine.Update(inventory);

			if (MachineData is not null)
				clone.LoadData(MachineData);

			// Inform the dummy object that it is indeed a dummy instance
			// Things that rely on position (e.g. solar panels) can use this to ignore the position
			clone.ID = -1;

			var generated = IPowerGeneratorMachine.GetPowerGenerationWithUpgrades(generator, 1);

			string shortUnit = energyType.ShortName;

			return perGameTick
				? Language.GetTextValue("Mods.SerousEnergyLib.PowerGenerationTooltips.PerGameTick", generated, shortUnit, generated * 60)
				: Language.GetTextValue("Mods.SerousEnergyLib.PowerGenerationTooltips.PerOperation", generated, shortUnit);
		}

		public override void SaveData(TagCompound tag) {
			tag["data"] = MachineData;
		}

		public override void LoadData(TagCompound tag) {
			MachineData = tag.GetCompound("data");
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			if (machineData is not null) {
				TooltipHelper.FindAndModify(tooltips, "<HAS_ITEM_DATA>", Language.GetTextValue("Mods.SerousEnergyLib.ItemHasMachineData"));
				TooltipHelper.FindAndModify(tooltips, "<DATA_HASH>", Language.GetTextValue("Mods.SerousEnergyLib.DataHash", DataHash));
			} else {
				TooltipHelper.FindAndRemoveLine(tooltips, "<HAS_ITEM_DATA>");
				TooltipHelper.FindAndRemoveLine(tooltips, "<DATA_HASH>");
			}

			if (GetEnergyUsageString(MachineUsesEnergyPerTick) is string useString)
				TooltipHelper.FindAndModify(tooltips, "<POWER_USAGE>", $"[c/dddd00:{useString}]");
			else
				TooltipHelper.FindAndRemoveLine(tooltips, "<POWER_USAGE>");

			if (GetEnergyGenerationString(MachineGeneratesEnergyPerTick) is string generateString)
				TooltipHelper.FindAndModify(tooltips, "<POWER_GENERATION>", $"[c/dddd00:{generateString}]");
			else
				TooltipHelper.FindAndRemoveLine(tooltips, "<POWER_GENERATION>");
		}
	}

	/// <inheritdoc cref="BaseMachineItem"/>
	public abstract class BaseMachineItem<T> : BaseMachineItem where T : ModTile, IMachineTile {
		public sealed override int MachineTile => ModContent.TileType<T>();
	}
}
