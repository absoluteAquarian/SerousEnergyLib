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
		protected UIDragablePanel panel;

		protected Dictionary<string, BaseMachineUIPage> pages;

		public BaseMachineUIPage CurrentPage { get; private set; }

		private bool needsRecalculate;

		public float PanelLeft {
			get => panel.Left.Pixels;
			set {
				if (panel.Left.Pixels != value)
					needsRecalculate = true;

				panel.Left.Set(value, 0f);
			}
		}
		
		public float PanelTop {
			get => panel.Top.Pixels;
			set {
				if (panel.Top.Pixels != value)
					needsRecalculate = true;

				panel.Top.Set(value, 0f);
			}
		}
		
		public float PanelWidth {
			get => panel.Width.Pixels;
			protected set {
				if (panel.Width.Pixels != value)
					needsRecalculate = true;

				panel.Width.Set(value, 0f);
			}
		}
		
		public float PanelHeight {
			get => panel.Height.Pixels;
			protected set {
				if (panel.Height.Pixels != value)
					needsRecalculate = true;

				panel.Height.Set(value, 0f);
			}
		}

		public float PanelRight {
			get => PanelLeft + PanelWidth;
			protected set => PanelLeft = value - PanelWidth;
		}
		
		public float PanelBottom {
			get => PanelTop + PanelHeight;
			protected set => PanelTop = value - PanelHeight;
		}

		protected abstract IEnumerable<string> GetMenuOptions();

		protected abstract LocalizedText GetMenuOptionLocalization(string menu);

		protected abstract BaseMachineUIPage InitPage(string page);

		public abstract string DefaultPage { get; }

		public BaseMachineUIPage GetPage(string page) => pages[page];

		public T GetPage<T>(string page) where T : BaseMachineUIPage
			=> pages is null
				? null
				: (pages[page] as T ?? throw new InvalidCastException($"The underlying object for page \"{GetType().Name}:{page}\" cannot be converted to " + typeof(T).FullName));

		public virtual void GetDefaultPanelDimensions(out int width, out int height) {
			width = 500;
			height = 600;
		}

		public bool pendingUIChange;

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

		public void Open() {
			if (CurrentPage is not null)
				return;

			OnOpen();

			SetPage(DefaultPage);
		}

		protected virtual void OnOpen() { }

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

		public virtual void Refresh() { }
	}
}
