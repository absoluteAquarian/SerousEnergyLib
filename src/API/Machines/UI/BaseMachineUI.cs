using Microsoft.Xna.Framework;
using SerousCommonLib.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace SerousEnergyLib.API.Machines.UI {
	// Implementation was copied from Magic Storage
	/// <summary>
	/// The base implementation of interfacing with an <see cref="IMachine"/>
	/// </summary>
	public abstract class BaseMachineUI : UIState {
		/// <summary>
		/// The UI element object containing the tabs and view area for this UI
		/// </summary>
		protected UIDragablePanel panel;

		/// <summary>
		/// The pages contained by <see cref="panel"/>
		/// </summary>
		protected Dictionary<string, BaseMachineUIPage> pages;

		/// <summary>
		/// The current tab selected by this UI
		/// </summary>
		public BaseMachineUIPage CurrentPage { get; private set; }

		private bool needsRecalculate;

		/// <summary>
		/// Set this property to <see langword="true"/> to make this UI call <see cref="Refresh"/> when it updates
		/// </summary>
		public bool NeedsToRecalculate {
			get => needsRecalculate;
			set => needsRecalculate |= value;
		}

		/// <summary>
		/// The left edge of this UI's panel
		/// </summary>
		public float PanelLeft {
			get => panel.Left.Pixels;
			set {
				if (panel.Left.Pixels != value)
					needsRecalculate = true;

				panel.Left.Set(value, 0f);
			}
		}
		
		/// <summary>
		/// The top edge of this UI's panel
		/// </summary>
		public float PanelTop {
			get => panel.Top.Pixels;
			set {
				if (panel.Top.Pixels != value)
					needsRecalculate = true;

				panel.Top.Set(value, 0f);
			}
		}
		
		/// <summary>
		/// The width of this UI's panel
		/// </summary>
		public float PanelWidth {
			get => panel.Width.Pixels;
			protected set {
				if (panel.Width.Pixels != value)
					needsRecalculate = true;

				panel.Width.Set(value, 0f);
			}
		}
		
		/// <summary>
		/// The height of this UI's panel
		/// </summary>
		public float PanelHeight {
			get => panel.Height.Pixels;
			protected set {
				if (panel.Height.Pixels != value)
					needsRecalculate = true;

				panel.Height.Set(value, 0f);
			}
		}

		/// <summary>
		/// The right edge of this UI's panel
		/// </summary>
		public float PanelRight {
			get => PanelLeft + PanelWidth;
			protected set => PanelLeft = value - PanelWidth;
		}
		
		/// <summary>
		/// The bottom edge of this UI's panel
		/// </summary>
		public float PanelBottom {
			get => PanelTop + PanelHeight;
			protected set => PanelTop = value - PanelHeight;
		}

		/// <summary>
		/// Return an enumeration of identifiers to be used by <see cref="InitPage(string)"/>
		/// </summary>
		protected abstract IEnumerable<string> GetMenuOptions();

		/// <summary>
		/// Return an instance of localized text for a page, given its key from <see cref="GetMenuOptions"/>
		/// </summary>
		protected abstract LocalizedText GetMenuOptionLocalization(string key);

		/// <summary>
		/// Initialize a page, given its identifier from <see cref="GetMenuOptions"/>
		/// </summary>
		protected abstract BaseMachineUIPage InitPage(string page);

		/// <summary>
		/// The default page to display when opening this UI
		/// </summary>
		public abstract string DefaultPage { get; }

		/// <summary>
		/// Gets a page from this UI's menu
		/// </summary>
		/// <remarks>This method throws an exception if the page does not exist</remarks>
		public BaseMachineUIPage GetPage(string page) => pages[page];

		/// <summary>
		/// Gets a page from this UI's menu and attempts to cast it to <typeparamref name="T"/>
		/// </summary>
		/// <remarks>This method throws an exception if the page does not exist</remarks>
		/// <exception cref="InvalidCastException"/>
		public T GetPage<T>(string page) where T : BaseMachineUIPage
			=> pages is null
				? null
				: (pages[page] as T ?? throw new InvalidCastException($"The underlying object for page \"{GetType().Name}:{page}\" cannot be converted to " + typeof(T).FullName));

		/// <summary>
		/// Assign the default values for this UI's panel width and height here
		/// </summary>
		public virtual void GetDefaultPanelDimensions(out int width, out int height) {
			width = 500;
			height = 600;
		}

		/// <summary>
		/// Set this field to <see langword="true"/> if you want the panel to reset to its default location on the screen
		/// </summary>
		public bool pendingUIChange;

		#pragma warning disable CS1591
		public override void OnInitialize() {
			panel = new(true, GetMenuOptions().Select(p => (p, GetMenuOptionLocalization(p))));

			panel.OnMenuReset += () => pendingUIChange = true;

			GetDefaultPanelDimensions(out int width, out int height);

			PanelTop = Main.screenHeight / 2 - height / 2;
			PanelLeft = Main.screenWidth / 2 - width / 2;
			PanelWidth = width + 2 * UIDragablePanel.cornerPadding;
			PanelHeight = height + 2 * UIDragablePanel.cornerPadding;

			pages = new();

			foreach ((string key, var tab) in panel.menus) {
				var page = pages[key] = InitPage(key);
				page.Width = StyleDimension.Fill;
				page.Height = StyleDimension.Fill;

				tab.OnClick += (evt, e) => {
					SoundEngine.PlaySound(SoundID.MenuTick);
					SetPage((e as UIPanelTab).Name);
				};
			}

			PostInitializePages();

			// Need to manually activate the pages
			foreach (var page in pages.Values)
				page.Activate();

			Append(panel);

			PostAppendPanel();

			needsRecalculate = false;
		}

		public sealed override void OnActivate() => Open();

		public sealed override void OnDeactivate() => Close();

		protected virtual void PostInitializePages() { }

		protected virtual void PostAppendPanel() { }

		public bool SetPage(string page) {
			BaseMachineUIPage newPage = pages[page];

			if (!object.ReferenceEquals(CurrentPage, newPage)) {
				panel.SetActivePage(page);

				if (CurrentPage is not null) {
					CurrentPage.InvokeOnPageDeselected();

					CurrentPage.Remove();
				}

				CurrentPage = newPage;

				panel.viewArea.Append(CurrentPage);

				CurrentPage.InvokeOnPageSelected();

				return true;
			}

			return false;
		}

		/// <summary>
		/// Opens this UI, calls the relevant events and then sets its page to <see cref="DefaultPage"/>
		/// </summary>
		public void Open() {
			if (CurrentPage is not null)
				return;

			OnOpen();

			SetPage(DefaultPage);
		}

		protected virtual void OnOpen() { }

		/// <summary>
		/// Closes this UI and calls the relevant events
		/// </summary>
		public void Close() {
			if (CurrentPage is not null) {
				OnClose();

				CurrentPage.InvokeOnPageDeselected();

				CurrentPage.Remove();
			}

			CurrentPage = null;
		}

		protected virtual void OnClose() { }

		public override void Update(GameTime gameTime) {
			if (needsRecalculate) {
				Refresh();
				Recalculate();
			}

			if (pendingUIChange) {
				PanelTop = Main.instance.invBottom + 60;
				PanelLeft = 20;

				pendingUIChange = false;
			}

			base.Update(gameTime);

			if (needsRecalculate) {
				Refresh();
				Recalculate();
			}
		}

		public override void Recalculate() {
			base.Recalculate();

			needsRecalculate = false;
		}

		/// <summary>
		/// This method is executed whenever the UI recalculates itself in <see cref="Update(GameTime)"/>
		/// </summary>
		public virtual void Refresh() { }
	}
}
