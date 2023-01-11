using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.API.Machines;
using System.Collections.Generic;
using System;
using Terraria.ModLoader;
using Terraria.DataStructures;
using System.Linq;

namespace SerousEnergyLib.Systems {
	/// <summary>
	/// The central class for storing and retrieving machine UIs
	/// </summary>
	public class MachineUISingletons : ModSystem {
		private static readonly Dictionary<int, BaseMachineUI> machineUIs = new();

		/// <summary>
		/// Registers a <see cref="BaseMachineUI"/> instance and ties it to a machine entity type
		/// </summary>
		/// <param name="ui">The instance</param>
		public static void RegisterUI<T>(BaseMachineUI ui) where T : ModTileEntity, IMachine {
			ArgumentNullException.ThrowIfNull(ui);

			machineUIs[ModContent.TileEntityType<T>()] = ui;
		}

		/// <summary>
		/// Registers a <see cref="BaseMachineUI"/> instance and ties it to a machine entity type
		/// </summary>
		/// <param name="type">The ID of the machine entity to tie the <see cref="BaseMachineUI"/> instance to</param>
		/// <param name="ui">The instance</param>
		public static void RegisterUI(int type, BaseMachineUI ui) {
			if (!TileEntity.manager.TryGetTileEntity(type, out ModTileEntity entity) || entity is not IMachine)
				throw new ArgumentException("Specified type was not a ModTileEntity or was not an IMachine", nameof(type));

			ArgumentNullException.ThrowIfNull(ui);

			machineUIs[type] = ui;
		}

		/// <summary>
		/// Retrieves the <see cref="BaseMachineUI"/> instance tied to a machine entity type
		/// </summary>
		/// <returns>A valid <see cref="BaseMachineUI"/> if one was registered for this machine entity type, <see langword="null"/> otherwise.</returns>
		public static BaseMachineUI GetInstance<T>() where T : ModTileEntity, IMachine => machineUIs.TryGetValue(ModContent.TileEntityType<T>(), out var ui) ? ui : null;

		/// <inheritdoc cref="GetInstance{T}"/>
		/// <param name="type">The ID of the machine entity to retrieve the <see cref="BaseMachineUI"/> instance from</param>
		public static BaseMachineUI GetInstance(int type) {
			if (!TileEntity.manager.TryGetTileEntity(type, out ModTileEntity entity) || entity is not IMachine)
				throw new ArgumentException("Specified type was not a ModTileEntity or was not an IMachine", nameof(type));

			return machineUIs.TryGetValue(type, out var ui) ? ui : null;
		}

#pragma warning disable CS1591
		public override void PostSetupContent() {
			// Autoload all machine UIs
			foreach (var machineSingleton in ModContent.GetContent<ModTileEntity>().OfType<IMachineUIAutoloading>())
				machineSingleton.RegisterUI();
		}

		public override void Unload() {
			machineUIs.Clear();
		}
	}
}
