namespace SerousEnergyLib.API.Energy {
	/// <summary>
	/// The default energy unit for machines
	/// </summary>
	public readonly struct TerraFlux {
		private readonly double amount;

		/// <summary>
		/// An object reprsenting 0 Terra Flux
		/// </summary>
		public static readonly TerraFlux Zero = new TerraFlux(0f);

		#pragma warning disable CS1591
		public TerraFlux(double amount) {
			this.amount = amount;
		}

		#region Operators
		public static TerraFlux operator +(TerraFlux flux, double val) => new TerraFlux(flux.amount + val);

		public static TerraFlux operator -(TerraFlux flux, double val) => new TerraFlux(flux.amount - val);

		public static TerraFlux operator *(TerraFlux flux, double val) => new TerraFlux(flux.amount * val);

		public static TerraFlux operator /(TerraFlux flux, double val) => new TerraFlux(flux.amount / val);

		public static TerraFlux operator +(double val, TerraFlux flux) => new TerraFlux(flux.amount + val);

		public static TerraFlux operator -(double val, TerraFlux flux) => new TerraFlux(flux.amount - val);

		public static TerraFlux operator *(double val, TerraFlux flux) => new TerraFlux(flux.amount * val);

		public static TerraFlux operator /(double val, TerraFlux flux) => new TerraFlux(flux.amount / val);

		public static TerraFlux operator +(TerraFlux flux, TerraFlux other) => new TerraFlux(flux.amount + other.amount);

		public static TerraFlux operator -(TerraFlux flux, TerraFlux other) => new TerraFlux(flux.amount - other.amount);

		public static TerraFlux operator *(TerraFlux flux, TerraFlux other) => new TerraFlux(flux.amount * other.amount);

		public static TerraFlux operator /(TerraFlux flux, TerraFlux other) => new TerraFlux(flux.amount / other.amount);

		public static bool operator >(TerraFlux first, TerraFlux second) => first.amount > second.amount;

		public static bool operator <(TerraFlux first, TerraFlux second) => first.amount < second.amount;

		public static bool operator ==(TerraFlux first, TerraFlux second) => first.amount == second.amount;

		public static bool operator !=(TerraFlux first, TerraFlux second) => first.amount != second.amount;

		public static bool operator >=(TerraFlux first, TerraFlux second) => first.amount >= second.amount;

		public static bool operator <=(TerraFlux first, TerraFlux second) => first.amount <= second.amount;

		public static explicit operator double(TerraFlux flux) => flux.amount;
		#endregion

		public override bool Equals(object obj) => obj is TerraFlux flux && amount == flux.amount;

		public override int GetHashCode() => amount.GetHashCode();

		public override string ToString() => amount.ToString();
	}
}
