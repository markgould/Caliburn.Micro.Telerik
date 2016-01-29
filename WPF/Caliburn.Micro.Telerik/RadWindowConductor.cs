using System;
using Telerik.Windows.Controls;

namespace Caliburn.Micro.Telerik
{
	internal class RadWindowConductor
	{
		private bool _deactivatingFromView;
		private bool _deactivateFromViewModel;
		private bool _actuallyClosing;
		private readonly RadWindow _view;
		private readonly object _model;

		public RadWindowConductor(object model, RadWindow view)
		{
			_model = model;
			_view = view;

			var activatable = model as IActivate;
		    activatable?.Activate();

		    var deactivatable = model as IDeactivate;
			if (deactivatable != null)
			{
				view.Closed += Closed;
				deactivatable.Deactivated += Deactivated;
			}

			var guard = model as IGuardClose;
			if (guard != null)
			{
				view.PreviewClosed += PreviewClosed;
			}
		}

		private void Closed(object sender, EventArgs e)
		{
			_view.Closed -= Closed;
			_view.PreviewClosed -= PreviewClosed;

			if (_deactivateFromViewModel)
			{
				return;
			}

			var deactivatable = (IDeactivate) _model;

			_deactivatingFromView = true;
			deactivatable.Deactivate(true);
			_deactivatingFromView = false;
		}

		private void Deactivated(object sender, DeactivationEventArgs e)
		{
			if (!e.WasClosed)
			{
				return;
			}

			((IDeactivate) _model).Deactivated -= Deactivated;

			if (_deactivatingFromView)
			{
				return;
			}

			_deactivateFromViewModel = true;
			_actuallyClosing = true;
			_view.Close();
			_actuallyClosing = false;
			_deactivateFromViewModel = false;
		}

		private void PreviewClosed(object sender, WindowPreviewClosedEventArgs e)
		{
			if (e.Cancel == true)
			{
				return;
			}

			var guard = (IGuardClose) _model;

			if (_actuallyClosing)
			{
				_actuallyClosing = false;
				return;
			}

			bool runningAsync = false, shouldEnd = false;

			guard.CanClose(canClose =>
			{
				Execute.OnUIThread(() =>
				{
					if (runningAsync && canClose)
					{
						_actuallyClosing = true;
						_view.Close();
					}
					else
					{
						e.Cancel = !canClose;
					}

					shouldEnd = true;
				});
			});

			if (shouldEnd)
			{
				return;
			}

			e.Cancel = true;
			runningAsync = true;
		}
	}
}