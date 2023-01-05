using SerousEnergyLib.API.Machines;
using System;
using System.Collections.Generic;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.CrossMod {
	/// <summary>
	/// A delegate taking the current update tick as input and returning an object used for handling a machine's display in the Machine Workbench machine from Terran Automation
	/// </summary>
	public delegate MachineRegistryDisplayAnimationState GetDisplayStateDelegate(uint currentTick);

	/// <summary>
	/// An object representing information used to render a machine in the Machine Workbench machine from Terran Automation
	/// </summary>
	public sealed class MachineWorkbenchRegistry {
		/// <summary>
		/// The ID of the <see cref="IMachine"/> tile entity that this registry should retrieve its information from
		/// </summary>
		public readonly int MachineEntity;

		#pragma warning disable CS1591
		public readonly GetDisplayStateDelegate GetFirstDisplay;
		public readonly GetDisplayStateDelegate GetSecondDisplay;

		public MachineWorkbenchRegistry(int id, GetDisplayStateDelegate getFirstDisplay, GetDisplayStateDelegate getSecondDisplay = null) {
			ArgumentNullException.ThrowIfNull(getFirstDisplay);

			if (!TileEntity.manager.TryGetTileEntity(id, out ModTileEntity entity) || entity is not IMachine)
				throw new ArgumentException("Input ID either did not refer to a valid ModTileEntity instance or was not an IMachine");

			MachineEntity = id;
			GetFirstDisplay = getFirstDisplay;
			GetSecondDisplay = getSecondDisplay;
		}

		/// <summary>
		/// Returns a new instance of the tile entity indicated by <see cref="MachineEntity"/>
		/// </summary>
		/// <remarks>This method will throw an exception if <see cref="MachineEntity"/> is invalid or does not refer to an <see cref="IMachine"/> entity</remarks>
		/// <exception cref="InvalidOperationException"/>
		public IMachine GetMachineEntity() {
			var entity = ModTileEntity.ConstructFromType(MachineEntity) as IMachine;

			if (entity is null)
				throw new InvalidOperationException("Entity ID either did not refer to a valid ModTileEntity instance or was not an IMachine");

			if (entity is not IScienceWorkbenchViewableMachine)
				throw new InvalidOperationException("Entity does not have support for the Machine Workbench");

			return entity;
		}

		/// <summary>
		/// Examines the machine returned by <see cref="GetMachineEntity"/> and returns an enumeration of descriptors for the entity
		/// </summary>
		public IEnumerable<string> GetDescriptorLines() {
			var entity = GetMachineEntity();

			bool isGeneric = true;

			if (entity is IInventoryMachine inventory) {
				isGeneric = false;
				yield return "Item Storage";

				bool hasInput = inventory.GetInputSlotsOrDefault().Length > 0;
				bool hasOutput = inventory.GetExportSlotsOrDefault().Length > 0;

				if (hasInput && hasOutput)
					yield return "Item Exporting/Importing Available";
				else if (hasInput)
					yield return "Item Importing Available";
				else if (hasOutput)
					yield return "Item Exporting Available";
			}

			if (entity is IFluidMachine) {
				isGeneric = false;
				yield return "Fluid Storage";
			}
			
			if (entity is IPowerGeneratorMachine) {
				isGeneric = false;
				yield return "May Consume Power";
				yield return "Generates Power";
			} else if (entity is IPoweredMachine) {
				isGeneric = false;
				yield return "Consumes Power";
			}

			if (isGeneric)
				yield return "No Specifications";
		}
	}
}
