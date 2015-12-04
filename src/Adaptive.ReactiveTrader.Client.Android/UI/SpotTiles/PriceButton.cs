using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Adaptive.ReactiveTrader.Client.UI.SpotTiles;
using Adaptive.ReactiveTrader.Shared.Extensions;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Adaptive.ReactiveTrader.Client.Android.UI.SpotTiles
{
    public class PriceButton : LinearLayout
    {
        private readonly TextView _bigFiguresTextView;
        private readonly TextView _pipsTextView;
        private readonly TextView _tenthOfPipTextView;

        private readonly SerialDisposable _propertyChangedSubscription = new SerialDisposable();
        private readonly SerialDisposable _executingSubscription = new SerialDisposable();
        private readonly SerialDisposable _canExecuteSubscription = new SerialDisposable();

        private IOneWayPriceViewModel _viewModel;
        private bool _isenabledOverride = true;


        public PriceButton(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            LayoutInflater.From(context).Inflate(Resource.Layout.PriceButton, this);

            _bigFiguresTextView = FindViewById<TextView>(Resource.Id.PriceButtonBigFiguresTextView);
            _pipsTextView = FindViewById<TextView>(Resource.Id.PriceButtonPipsTextView);
            _tenthOfPipTextView = FindViewById<TextView>(Resource.Id.PriceButtonTenthOfPipTextView);
            var directionLabelTextView = FindViewById<TextView>(Resource.Id.PriceButtonDirectionTextView);
            directionLabelTextView.Text = attrs.GetAttributeValue(null, "direction_label");

            Click += PriceButton_Click;
        }

        public void SetDataContext(IOneWayPriceViewModel viewModel)
        {
            _viewModel = viewModel;

            _canExecuteSubscription.Disposable = Observable.FromEventPattern(h => viewModel.ExecuteCommand.CanExecuteChanged += h, h => viewModel.ExecuteCommand.CanExecuteChanged -= h)
                .Subscribe(_ =>
                {
                    var canExecute = viewModel.ExecuteCommand.CanExecute(null);
                    Enabled =_isenabledOverride && canExecute;
                });

            _executingSubscription.Disposable = _viewModel.ObserveProperty(vm => vm.IsExecuting)
                .Subscribe(isExecuting =>
                {
                    Selected = isExecuting;
                });

            _propertyChangedSubscription.Disposable = viewModel.ObserveProperty().Subscribe(_ => Update(viewModel));

            Update(viewModel);
        }

        public void SetEnabledOverride(bool isEnabled)
        {
            if (!isEnabled)
            {
                Enabled = false;
                _pipsTextView.Selected = true;    // state_enabled=false doesn't seem to work in the foreground selector so setting state_selected instead
            }
            else
            {
                Enabled = _viewModel.ExecuteCommand.CanExecute(null);
                _pipsTextView.Selected = false;
            }
            _isenabledOverride = isEnabled;
        }

        private void Update(IOneWayPriceViewModel viewModel)
        {
            _bigFiguresTextView.Text = viewModel.BigFigures;
            _pipsTextView.Text = viewModel.Pips;
            _tenthOfPipTextView.Text = viewModel.TenthOfPip;
        }

        private void PriceButton_Click(object sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                if (_viewModel.ExecuteCommand.CanExecute(null))
                {
                    _viewModel.ExecuteCommand.Execute(null);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            Click -= PriceButton_Click;
            _propertyChangedSubscription.Dispose();
            _executingSubscription.Dispose();
            _canExecuteSubscription.Dispose();
        }
    }
}