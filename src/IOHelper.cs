using Ionic.Zlib;
using System.IO;

namespace SerousEnergyLib {
	public static class IOHelper {
		public static byte[] Compress(byte[] data, CompressionLevel level) {
			using MemoryStream decompressed = new MemoryStream(data);
			using DeflateStream compression = new DeflateStream(decompressed, CompressionMode.Compress, level);
			using MemoryStream compressed = new MemoryStream();
			compression.CopyTo(compressed);
			return compressed.ToArray();
		}

		public static byte[] Decompress(byte[] data, CompressionLevel level) {
			using MemoryStream compressed = new MemoryStream(data);
			using DeflateStream decompression = new DeflateStream(compressed, CompressionMode.Decompress, level);
			using MemoryStream decompressed = new MemoryStream();
			decompression.CopyTo(decompressed);
			return decompressed.ToArray();
		}
	}
}
