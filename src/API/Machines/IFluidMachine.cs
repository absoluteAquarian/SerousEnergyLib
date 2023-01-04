using SerousEnergyLib.API.Fluid;
using SerousEnergyLib.Systems.Networks;
using Terraria.DataStructures;
using Terraria.ModLoader.IO;
using System.Linq;
using System.Collections.Generic;
using SerousEnergyLib.TileData;

namespace SerousEnergyLib.API.Machines {
	/// <summary>
	/// An interface containing methods used by machines that can store fluids
	/// </summary>
	public interface IFluidMachine : IMachine {
		/// <summary>
		/// The fluid storages within this machine
		/// </summary>
		FluidStorage[] FluidStorage { get; protected set; }

		/// <summary>
		/// Whether the fluid pipe at position (<paramref name="pipeX"/>, <paramref name="pipeY"/>) can merge with this machine's sub-tile at position (<paramref name="machineX"/>, <paramref name="machineY"/>)
		/// </summary>
		/// <param name="pipeX">The tile X-coordinate for the fluid pipe</param>
		/// <param name="pipeY">The tile Y-coordinate for the fluid pipe</param>
		/// <param name="machineX">The tile X-coordinate for the machine sub-tile</param>
		/// <param name="machineY">The tile Y-coordinate for the machine sub-tile</param>
		bool CanMergeWithFluidPipe(int pipeX, int pipeY, int machineX, int machineY);

		/// <summary>
		/// Select which fluid inventory can be exported from a given <paramref name="pump"/> location at the given <paramref name="subtile"/> location within this machine
		/// </summary>
		/// <param name="pump">The tile coordinates of the pump tile</param>
		/// <param name="subtile">The tile coordinates of which sub-tile of this machine is being pumped from</param>
		/// <returns>An instance from <see cref="FluidStorage"/> or <see langword="null"/> if no fluids can be extracted</returns>
		FluidStorage SelectFluidExportSource(Point16 pump, Point16 subtile);

		public IEnumerable<FluidNetwork> GetAdjacentFluidNetworks() {
			return GetAdjacentNetworks(NetworkType.Fluids)
				.Select(r => r.network as FluidNetwork)
				.OfType<FluidNetwork>();
		}

		public void SaveFluids(TagCompound tag) {
			static TagCompound SaveFluid(FluidStorage storage) {
				TagCompound fluidTag = new TagCompound();
				storage.SaveData(fluidTag);
				return fluidTag;
			}

			tag["fluids"] = FluidStorage.Select(SaveFluid).ToList();
		}

		public void LoadFluids(TagCompound tag) {
			if (tag.GetList<TagCompound>("fluids") is List<TagCompound> fluids && fluids.Count == FluidStorage.Length) {
				for (int i = 0; i < FluidStorage.Length; i++)
					FluidStorage[i].LoadData(fluids[i]);
			}
		}
	}
}
