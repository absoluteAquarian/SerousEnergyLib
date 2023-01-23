using Terraria;

namespace SerousEnergyLib {
	/// <summary>
	/// A helper class containing constants and functions for checking and manipulating <see cref="Main.time"/> and <see cref="Main.dayTime"/>
	/// </summary>
	public static class TimeFunctions {
		/// <summary>
		/// 4 hours, 30 minutes
		/// </summary>
		public const int _4_30 = 4 * 3600 + 30 * 60;
		/// <summary>
		/// 7 hours, 30 minutes
		/// </summary>
		public const int _7_30 = 7 * 3600 + 30 * 60;
		/// <summary>
		/// 12 hours
		/// </summary>
		public const int _12_00 = 12 * 3600;
		/// <summary>
		/// 07:30 PM in daytime ticks
		/// </summary>
		public const int _7_30PM_day = (int)Main.dayLength;
		/// <summary>
		/// 04:30 AM in nighttime ticks
		/// </summary>
		public const int _4_30AM_night = (int)Main.nightLength;
		/// <summary>
		/// 12:00 AM
		/// </summary>
		public const int _12AM = _4_30AM_night - _4_30;  //16,200
		/// <summary>
		/// 12:00 PM
		/// </summary>
		public const int _12PM = _7_30PM_day - _7_30;    //27,000
		/// <summary>
		/// The total tick duration for one day and night
		/// </summary>
		public const int FullDay = _4_30AM_night + _7_30PM_day;

		/// <summary>
		/// Gets <see cref="Main.time"/> in absolute time from the start of a day
		/// </summary>
		public static double CurrentTotalTime() => Main.dayTime ? Main.time : Main.time + _7_30PM_day;

		/// <summary>
		/// Gets the current hour, minutes and seconds in absolute time
		/// </summary>
		/// <param name="hours">The hour</param>
		/// <param name="minutes">The minute</param>
		/// <param name="seconds">The second</param>
		public static void GetCurrentTime(out int hours, out int minutes, out double seconds) {
			double total = CurrentTotalTime();

			//Move time so that 12 AM is at tick time 0
			total += _4_30;
			
			if (total >= FullDay)
				total -= FullDay;

			hours = (int)(total / 3600);
			minutes = (int)(total / 60) % 60;
			seconds = total % 60;
		}

		/// <summary>
		/// Gets the current hour, minutes and seconds in ingame time
		/// </summary>
		/// <param name="hours">The hour</param>
		/// <param name="minutes">The minute</param>
		/// <param name="seconds">The second</param>
		/// <param name="am">Whether the time is AM or PM</param>
		public static void GetCurrentTerrariaTime(out int hours, out int minutes, out double seconds, out bool am) {
			hours = (int)(Main.time / 3600);
			minutes = (int)(Main.time / 60) % 60;
			seconds = Main.time % 60;
			
			if (Main.dayTime) {
				hours += 4;
				minutes += 30;
			} else {
				hours += 7;
				minutes += 30;
			}
			
			if (minutes >= 60) {
				hours++;
				minutes -= 60;
			}
			
			if (Main.dayTime)
				am = hours < 12;
			else
				am = hours >= 12;

			hours %= 12;
		}

		/// <summary>
		/// Converts an hour, minute and seconds duration into time ticks
		/// </summary>
		/// <param name="hours">The hours</param>
		/// <param name="minutes">The minutes</param>
		/// <param name="seconds">The seconds</param>
		/// <returns>The time ticks.  1 tick = 1 second</returns>
		public static double ToTicks(int hours = 0, int minutes = 0, double seconds = 0) => hours * 3600 + minutes * 60 + seconds;
	}
}
