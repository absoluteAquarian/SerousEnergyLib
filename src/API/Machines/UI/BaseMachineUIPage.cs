using System;
using Terraria.UI;

namespace SerousEnergyLib.API.Machines.UI {
	/// <summary>
	/// The base implementation of a page within a <see cref="BaseMachineUI"/> state
	/// </summary>
	public abstract class BaseMachineUIPage : UIElement {
		/// <summary>
		/// The <see cref="BaseMachineUI"/> that this page is assigned to
		/// </summary>
		protected readonly BaseMachineUI parentUI;

		/// <summary>
		/// The name of this page
		/// </summary>
		public readonly string Name;

		/// <summary>
		/// The event that executes whenever this page is selected
		/// </summary>
		public event Action OnPageSelected;

		/// <summary>
		/// The event that executes whenever this page is deselected
		/// </summary>
		public event Action OnPageDeselected;

		#pragma warning disable CS1591
		public BaseMachineUIPage(BaseMachineUI parent, string name) {
			parentUI = parent;
			Name = name;

			OnPageSelected += Recalculate;

			Width = StyleDimension.Fill;
			Height = StyleDimension.Fill;
		}

		public void InvokeOnPageSelected() => OnPageSelected?.Invoke();

		public void InvokeOnPageDeselected() => OnPageDeselected?.Invoke();
	}
}
