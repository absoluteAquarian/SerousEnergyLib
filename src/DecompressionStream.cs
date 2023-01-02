using Ionic.Zlib;
using System;
using System.IO;

namespace SerousEnergyLib {
	internal class DecompressionStream : IDisposable {
		private MemoryStream stream;
		public BinaryReader reader;

		public DecompressionStream(BinaryReader reader) {
			int compressedLength = reader.ReadInt32();
			byte[] compressed = reader.ReadBytes(compressedLength);

			stream = new MemoryStream(IOHelper.Decompress(compressed, CompressionLevel.BestCompression));
			this.reader = new BinaryReader(stream);
		}

		public void Dispose() {
			reader.Dispose();
			stream.Dispose();
		}
	}
}
