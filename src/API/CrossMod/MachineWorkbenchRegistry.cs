using SerousEnergyLib.API.Machines;
using SerousEnergyLib.Tiles;
using System;
using System.Collections.Generic;
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
		/// The ID of the <see cref="IMachineTile"/> tile entity that this registry should retrieve its information from
		/// </summary>
		public readonly int MachineTile;

		#pragma warning disable CS1591
		public readonly GetDisplayStateDelegate GetFirstDisplay;
		public readonly GetDisplayStateDelegate GetSecondDisplay;

		public MachineWorkbenchRegistry(int id, GetDisplayStateDelegate getFirstDisplay, GetDisplayStateDelegate getSecondDisplay = null) {
			ArgumentNullException.ThrowIfNull(getFirstDisplay);

			if (TileLoader.GetTile(id) is not IMachineTile)
				throw new ArgumentException("Input ID did not refer to an IMachineTile instance");

			MachineTile = id;
			GetFirstDisplay = getFirstDisplay;
			GetSecondDisplay = getSecondDisplay;
		}

		/// <summary>
		/// Returns a new instance of the tile indicated by <see cref="MachineTile"/>
		/// </summary>
		/// <remarks>This method will throw an exception if <see cref="MachineTile"/> is invalid or does not refer to an <see cref="IMachineTile"/> tile</remarks>
		/// <exception cref="InvalidOperationException"/>
		public IMachineTile GetMachineTile() {
			if (TileLoader.GetTile(MachineTile) is not IMachineTile entity)
				throw new InvalidOperationException("Tile ID did not refer to an IMachineTile instance");

			if (entity is not IMachineWorkbenchViewableMachine)
				throw new InvalidOperationException("Tile ID does not have support for the Machine Workbench");

			return entity;
		}

		/// <summary>
		/// Examines the machine returned by <see cref="GetMachineTile"/> and returns an enumeration of descriptors for its entity
		/// </summary>
		public IEnumerable<string> GetDescriptorLines() {
			var tile = GetMachineTile();

			var entity = tile.GetMachineEntity();

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
