using SerousEnergyLib.API.Fluid.Default;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.API.Fluid {
	/// <summary>
	/// An object representing storage of fluids
	/// </summary>
	public class FluidStorage {
		/// <summary>
		/// The <see cref="FluidTypeID"/> that this storage contains, or nothing when this property is set to <c>null</c>
		/// </summary>
		public FluidTypeID FluidID { get; set; }

		/// <summary>
		/// The <see cref="FluidTypeID"/> that this storage contains, represented as an integer.<br/>
		/// If this storage contains no fluid, this property returns <see cref="FluidTypeID.None"/>.
		/// </summary>
		public int FluidType => FluidID?.Type ?? FluidTypeID.None;

		/// <summary>
		/// The current amount of fluid that's stored
		/// </summary>
		public double CurrentCapacity;

		/// <summary>
		/// The maximum amount of fluid that can be stored
		/// </summary>
		public double MaxCapacity;

		/// <summary>
		/// The default maximum amount of fluid that can be stored<br/>
		/// This property can be used to reset <see cref="MaxCapacity"/> when components that increase it are removed
		/// </summary>
		public double BaseMaxCapacity { get; private set; }

		private FluidTypeID[] allowedFluidTypes;

		/// <summary>
		/// The fluid IDs that this storage permits storing
		/// </summary>
		public ReadOnlySpan<FluidTypeID> AllowedFluidTypes => allowedFluidTypes ?? Array.Empty<FluidTypeID>();

		#pragma warning disable CS1591
		public bool IsEmpty => CurrentCapacity <= 0;

		public bool IsFull => CurrentCapacity >= MaxCapacity;

		public FluidStorage(double max) {
			BaseMaxCapacity = MaxCapacity = max;

			allowedFluidTypes = null;
		}

		public FluidStorage(double max, params FluidTypeID[] allowedFluidTypes) {
			if (allowedFluidTypes.Any(f => f is null or UnloadedFluidID))
				throw new ArgumentException("Allowed fluid types array contained invalid values", nameof(allowedFluidTypes));

			BaseMaxCapacity = MaxCapacity = max;

			this.allowedFluidTypes = allowedFluidTypes.ToArray();
		}

		public FluidStorage(double max, params int[] allowedFluidTypes) {
			int unloadedType = SerousMachines.FluidType<UnloadedFluidID>();
			if (allowedFluidTypes.Any(f => f == unloadedType || FluidLoader.Get(f) is null))
				throw new ArgumentException("Allowed fluid types array contained invalid values", nameof(allowedFluidTypes));

			BaseMaxCapacity = MaxCapacity = max;

			this.allowedFluidTypes = allowedFluidTypes.Select(FluidLoader.Get).ToArray();
		}

		public void SaveData(TagCompound tag) {
			tag["current"] = CurrentCapacity;
			tag["max"] = MaxCapacity;
			tag["base"] = BaseMaxCapacity;
			
			tag["type"] = SaveFluid(FluidID);

			if (allowedFluidTypes is not null)
				tag["allowed"] = allowedFluidTypes.Select(SaveFluid).OfType<TagCompound>().ToList();
		}

		private static TagCompound SaveFluid(FluidTypeID type) {
			if (type is null)
				return null;

			return type is UnloadedFluidID unloaded
				? new TagCompound() {
					["mod"] = unloaded.unloadedMod,
					["name"] = unloaded.unloadedName
				}
				: new TagCompound() {
					["mod"] = type.Mod.Name,
					["name"] = type.Name
				};
		}

		public void LoadData(TagCompound tag) {
			CurrentCapacity = tag.GetDouble("current");
			MaxCapacity = tag.GetDouble("max");
			BaseMaxCapacity = tag.GetDouble("base");

			if (tag.GetCompound("type") is TagCompound type)
				FluidID = LoadFluid(type);

			// Allow unloaded fluid types here, but not in the ctor
			if (tag.GetList<TagCompound>("allowed") is List<TagCompound> allowed)
				allowedFluidTypes = allowed.Select(LoadFluid).OfType<FluidTypeID>().ToArray();
		}

		private static FluidTypeID LoadFluid(TagCompound tag) {
			string mod = tag.GetString("mod");
			string name = tag.GetString("name");

			if (string.IsNullOrWhiteSpace(mod) || string.IsNullOrWhiteSpace(name))
				return null;  // Invalid tag; load as "no fluid"

			if (!ModLoader.TryGetMod(mod, out Mod source) || !source.TryFind(name, out FluidTypeID id))
				return ModContent.GetInstance<UnloadedFluidID>().Clone(mod, name);

			return id;
		}

		/// <summary>
		/// Imports at most <paramref name="amount"/> fluid into this storage.  Any leftover fluids will be contained in <paramref name="amount"/>
		/// </summary>
		/// <param name="fluidID">The fluid ID to import</param>
		/// <param name="amount">The amount of fluid to import</param>
		/// <remarks>
		/// <see cref="UnloadedFluidID"/> cannot be imported.<br/>
		/// If this storage runs out of fluid, <see cref="FluidID"/> is set to <see langword="null"/>.
		/// </remarks>
		public void Import(int fluidID, ref double amount) {
			if (amount <= 0)
				return;

			// Cannot import fluids
			if (IsFull)
				return;

			// Prohibit importing fluids when the types mismatch or when either is "Unloaded"
			int curFluid = FluidType;
			if (curFluid != fluidID || curFluid == SerousMachines.FluidType<UnloadedFluidID>() || fluidID == SerousMachines.FluidType<UnloadedFluidID>())
				return;

			// Prohibit importing fluids that aren't allowed
			if (allowedFluidTypes is not null && !allowedFluidTypes.Any(f => f.Type == fluidID))
				return;

			// Ensure that the ID and capacity are in sync
			if (curFluid == FluidTypeID.None || CurrentCapacity == 0) {
				FluidID = FluidLoader.Get(fluidID);
				CurrentCapacity = 0;
			}

			// Imported type was invalid
			if (FluidType == FluidTypeID.None)
				return;

			double import = amount;

			if (CurrentCapacity + import > MaxCapacity)
				import = MaxCapacity - import;

			CurrentCapacity += import;
			amount -= import;
		}

		/// <summary>
		/// Imports at most <paramref name="import"/> fluid from <paramref name="source"/> into this storage.
		/// </summary>
		/// <param name="source">The storage to export fluid from</param>
		/// <param name="import">The amount of fluid to import</param>
		public void ImportFrom(FluidStorage source, double import) => Transfer(source, this, import);

		/// <summary>
		/// Exports at most <paramref name="amount"/> fluid from this storage.
		/// </summary>
		/// <param name="amount">The amount of fluid to export</param>
		/// <param name="exportedType">The fluid ID that was exported</param>
		/// <remarks>
		/// <see cref="UnloadedFluidID"/> cannot be exported.<br/>
		/// If this storage runs out of fluid, <see cref="FluidID"/> is set to <see langword="null"/>.
		/// </remarks>
		public void Export(ref double amount, out int exportedType) {
			exportedType = FluidTypeID.None;
			if (amount <= 0)
				return;

			// Cannot export fluids
			if (IsEmpty || FluidType == FluidTypeID.None) {
				// Ensure that the ID is also reset
				FluidID = null;
				amount = 0;
				return;
			}

			int curFluid = FluidType;
			
			// Cannot export unloaded fluids
			if (curFluid == SerousMachines.FluidType<UnloadedFluidID>()) {
				amount = 0;
				return;
			}

			exportedType = curFluid;
			if (amount > CurrentCapacity) {
				amount = CurrentCapacity;
				FluidID = null;
			}

			CurrentCapacity -= amount;
		}

		/// <summary>
		/// Exports at most <paramref name="export"/> fluid from this storage into <paramref name="destination"/>.
		/// </summary>
		/// <param name="destination">The storage to import fluid into</param>
		/// <param name="export">The amount of fluid to export</param>
		public void ExportTo(FluidStorage destination, double export) => Transfer(this, destination, export);

		private static void Transfer(FluidStorage source, FluidStorage destination, double transfer) {
			if (transfer <= 0)
				return;

			// No fluids to export
			if (source.IsEmpty)
				return;

			// Cannot import fluids
			if (destination.IsFull)
				return;

			// Export the fluids
			source.Export(ref transfer, out int exportedType);
			destination.Import(exportedType, ref transfer);

			// Import any leftovers
			source.Import(exportedType, ref transfer);
		}

		/// <summary>
		/// Sends information for this fluid storage to a data stream
		/// </summary>
		/// <param name="writer"></param>
		public void Send(BinaryWriter writer) {
			SendFluidID(writer, FluidID);

			writer.Write(CurrentCapacity);
			writer.Write(MaxCapacity);
			writer.Write(BaseMaxCapacity);

			if (allowedFluidTypes is not null) {
				writer.Write((short)allowedFluidTypes.Length);

				for (int i = 0; i < allowedFluidTypes.Length; i++)
					SendFluidID(writer, allowedFluidTypes[i]);
			} else
				writer.Write((short)-1);
		}

		private static void SendFluidID(BinaryWriter writer, FluidTypeID id) {
			if (id is UnloadedFluidID unloaded) {
				writer.Write(true);
				writer.Write(unloaded.unloadedMod);
				writer.Write(unloaded.unloadedName);
			} else {
				writer.Write(false);
				writer.Write((short)(id?.Type ?? -1));
			}
		}

		/// <summary>
		/// Receives information for this fluid storage from a data stream
		/// </summary>
		/// <param name="reader"></param>
		public void Receive(BinaryReader reader) {
			FluidID = ReceiveFluidID(reader);

			CurrentCapacity = reader.ReadDouble();
			MaxCapacity = reader.ReadDouble();
			BaseMaxCapacity = reader.ReadDouble();

			short count = reader.ReadInt16();

			if (count >= 0) {
				allowedFluidTypes = new FluidTypeID[count];

				for (int i = 0; i < count; i++)
					allowedFluidTypes[i] = ReceiveFluidID(reader);
			} else
				allowedFluidTypes = null;
		}

		private static FluidTypeID ReceiveFluidID(BinaryReader reader) {
			if (reader.ReadBoolean())
				return ModContent.GetInstance<UnloadedFluidID>().Clone(reader.ReadString(), reader.ReadString());
			return FluidLoader.Get(reader.ReadInt16());
		}
	}
}
