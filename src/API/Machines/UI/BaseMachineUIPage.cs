using System;
using Terraria.UI;

namespace SerousEnergyLib.API.Machines.UI {
	public abstract class BaseMachineUIPage : UIElement {
		internal BaseMachineUI parentUI;

		public readonly string Name;

		public event Action OnPageSelected, OnPageDeselected;

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
