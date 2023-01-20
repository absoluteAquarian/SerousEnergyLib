using SerousEnergyLib.API.Upgrades;
using Terraria;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface representing a machine that can create item outputs<br/>
	/// This interface is used with <see cref="BaseUpgrade"/>
	/// </summary>
	public interface IItemOutputGeneratorMachine : IInventoryMachine, IMachine {
		/// <summary>
		/// Applies <see cref="BaseUpgrade.GetItemOutputGeneratorExtraStack(int, int)"/> to <paramref name="originalStack"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="originalStack">The original stack</param>
		/// <returns>The modified item stack</returns>
		public static int CalculateItemStack(IItemOutputGeneratorMachine machine, int originalStack)
			=> CalculateFromUpgrades(machine, originalStack, static (u, s, v) => v + u.GetItemOutputGeneratorExtraStack(s, v));

		/// <summary>
		/// Executes <see cref="BaseUpgrade.BlockItemOutputGeneratorOutput(int, int)"/> to determine if <paramref name="itemType"/> should not be generated
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="itemType">The item type being generated</param>
		/// <returns>Whether the item generation should be blocked</returns>
		public static bool ShouldBlockItemOutput(IItemOutputGeneratorMachine machine, int itemType) {
			// Local capturing
			int type = itemType;

			return CalculateFromUpgrades(machine, false, (u, s, v) => u.BlockItemOutputGeneratorOutput(s, type) | v);
		}

		/// <summary>
		/// Scans the possible outputs of <paramref name="recipe"/> and attempts to add them to the export slots of <paramref name="machine"/>
		/// </summary>
		/// <param name="machine">The machine to process</param>
		/// <param name="recipe">The recipe containing possible output items</param>
		protected static void AddRecipeOutputsToExportInventory(IItemOutputGeneratorMachine machine, MachineRecipe recipe) {
			if (recipe is null || recipe.PossibleOutputs.Count == 0)
				return;

			foreach (var output in recipe.PossibleOutputs) {
				if (ShouldBlockItemOutput(machine, output.type))
					continue;

				double chance = GetLuckThreshold(machine, output.chance);

				// Roll the dice
				if (Main.rand.NextDouble() < chance) {
					// Generate the item and add it to this machine's export slots
					int stack = CalculateItemStack(machine, output.stack);

					Item item = new Item(output.type, stack);

					AddItemToExportInventory(machine, item);
				}
			}
		}
	}
}
