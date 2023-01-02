using Ionic.Zlib;
using SerousEnergyLib.TileData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SerousEnergyLib.Systems {
	internal class WorldNetworkIO : ModSystem {
		public override void SaveWorldData(TagCompound tag) {
			byte[] data = TransformData(Main.tile.GetData<NetworkInfo>());

			// Compress the data
			tag["network"] = IOHelper.Compress(data, CompressionLevel.BestSpeed);

			static TagCompound SaveNetwork(NetworkInstance instance) {
				TagCompound netData = new();
				TagCompound data = new() {
					["type"] = (byte)instance.Filter,
					["data"] = netData
				};
				instance.SaveData(netData);
				return data;
			}

			tag["maps"] = Network.itemNetworks
				.Concat(Network.fluidNetworks)
				.Concat(Network.powerNetworks)
				.Select(SaveNetwork)
				.ToList();
		}

		public override void LoadWorldData(TagCompound tag) {
			if (tag.GetByteArray("network") is byte[] data) {
				// Decompress the data
				TransformData(IOHelper.Decompress(data, CompressionLevel.BestSpeed), Main.tile.GetData<NetworkInfo>());
			}

			Network.itemNetworks.Clear();
			Network.fluidNetworks.Clear();
			Network.powerNetworks.Clear();
			if (tag.GetList<TagCompound>("maps") is List<TagCompound> maps) {
				foreach (var map in maps) {
					try {
						NetworkType filter = (NetworkType)map.GetByte("filter");

						if (filter != NetworkType.Items && filter != NetworkType.Fluids && filter != NetworkType.Power)
							throw new IOException("Network type was invalid: " + filter);

						NetworkInstance instance = NetworkInstance.CreateNetwork(filter);
						instance.ReserveNextID();

						if (map.GetCompound("data") is not TagCompound netData)
							throw new IOException("Tag compound did not have a \"data\" entry");

						instance.LoadData(netData);

						if (instance.IsEmpty)
							continue;

						if (instance.Filter == NetworkType.Items)
							Network.itemNetworks.Add(instance);
						else if (instance.Filter == NetworkType.Fluids)
							Network.fluidNetworks.Add(instance);
						else if (instance.Filter == NetworkType.Power)
							Network.powerNetworks.Add(instance);
					} catch (Exception ex) {
						SerousMachines.Instance.Logger.Warn($"Failed to load network", ex);
					}
				}
			}
		}

		private static byte[] TransformData(NetworkInfo[] data) {
			byte[] converted = new byte[data.Length];

			unsafe {
				fixed (NetworkInfo* fixedPtr = data) fixed (byte* fixedConvPtr = converted) {
					NetworkInfo* ptr = fixedPtr;
					byte* convPtr = fixedConvPtr;
					int length = data.Length;

					for (int i = 0; i < length; i++, ptr++, convPtr++)
						*convPtr = ptr->netData;
				}
			}

			return converted;
		}

		private static void TransformData(byte[] data, NetworkInfo[] existing) {
			if (data.Length != existing.Length) {
				SerousMachines.Instance.Logger.Warn($"Saved data length ({data.Length}) did not match the world data length ({existing.Length}), data will not be loaded.");
				return;
			}

			unsafe {
				fixed (byte* fixedPtr = data) fixed (NetworkInfo* fixedConvPtr = existing) {
					byte* ptr = fixedPtr;
					NetworkInfo* convPtr = fixedConvPtr;
					int length = data.Length;

					for (int i = 0; i < length; i++, ptr++, convPtr++)
						convPtr->netData = *ptr;
				}
			}
		}
	}
}
