using SerousEnergyLib.API.Machines.UI;
using SerousEnergyLib.Systems;
using Terraria.ModLoader;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface representing automation for initializing and loading <see cref="IMachine.MachineUI"/>
	/// </summary>
	public interface IMachineUIAutoloading : IMachine {
		/// <summary>
		/// Registers the UI for this machine.  This method is called in <see cref="MachineUISingletons.PostSetupContent"/>
		/// </summary>
		void RegisterUI();
	}

	/// <inheritdoc cref="IMachineUIAutoloading"/>
	public interface IMachineUIAutoloading<TEntity, TMachineUI> : IMachineUIAutoloading where TEntity : ModTileEntity, IMachineUIAutoloading<TEntity, TMachineUI> where TMachineUI : BaseMachineUI, new() {
		BaseMachineUI IMachine.MachineUI => MachineUISingletons.GetInstance<TEntity>();

		void IMachineUIAutoloading.RegisterUI() => MachineUISingletons.RegisterUI<TEntity>(new TMachineUI());
	}
}
