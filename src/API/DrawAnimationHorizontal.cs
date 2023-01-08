using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;

namespace SerousEnergyLib.API {
	#pragma warning disable CS1591
	public class DrawAnimationHorizontal : DrawAnimation {
		public bool PingPong;
		public bool NotActuallyAnimating;
		public int SizeOffsetX;
		public int SizeOffsetY;

		public DrawAnimationHorizontal(int ticksperframe, int frameCount, bool pingPong = false) {
			Frame = 0;
			FrameCounter = 0;
			FrameCount = frameCount;
			TicksPerFrame = ticksperframe;
			PingPong = pingPong;
		}

		public override void Update() {
			if (NotActuallyAnimating || ++FrameCounter < TicksPerFrame)
				return;

			FrameCounter = 0;
			if (PingPong) {
				if (++Frame >= FrameCount * 2 - 2)
					Frame = 0;
			} else if (++Frame >= FrameCount) {
				Frame = 0;
			}
		}

		public override Rectangle GetFrame(Texture2D texture, int frameCounterOverride = -1) {
			if (frameCounterOverride != -1) {
				int num = frameCounterOverride / TicksPerFrame;
				int num2 = FrameCount;
				if (PingPong)
					num2 = num2 * 2 - 1;

				int num3 = num % num2;
				if (PingPong && num3 >= FrameCount)
					num3 = FrameCount * 2 - 2 - num3;

				Rectangle result = texture.Frame(1, FrameCount, 0, num3, SizeOffsetX, SizeOffsetY);
				result.Height -= 2;
				return result;
			}

			int frameX = Frame;
			if (PingPong && Frame >= FrameCount)
				frameX = FrameCount * 2 - 2 - Frame;

			Rectangle result2 = texture.Frame(FrameCount, 1, frameX, 0, SizeOffsetX, SizeOffsetY);
			result2.Height -= 2;
			return result2;
		}
	}
}
