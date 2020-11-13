using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using Shadowsocks.WPF.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Windows;

namespace Shadowsocks.WPF.ViewModels
{
    public class OnlineConfigViewModel : ReactiveValidationObject
    {
        public OnlineConfigViewModel()
        {
            Sources = new ObservableCollection<string>();
            SelectedSource = "";
            Address = "";

            // TODO in v5: if http:// show warning as materialDesign:HintAssist.HelperText
            AddressRule = this.ValidationRule(
                viewModel => viewModel.Address,
                address => address.StartsWith("http://"),
                "Warning: getting online configuration from plain HTTP sources is NOT secure!");

            var canUpdateCopyRemove = this.WhenAnyValue(
                x => x.SelectedSource,
                selectedSource => !string.IsNullOrWhiteSpace(selectedSource));
            var canUpdateAll = this.WhenAnyValue(
                x => x.Sources.Count,
                count => count > 0);
            var canAdd = this.WhenAnyValue(
                x => x.Address,
                address => Uri.IsWellFormedUriString(address, UriKind.Absolute) &&
                (address.StartsWith("https://") || address.StartsWith("http://")));

            //Update = ReactiveCommand.CreateFromTask(() => _controller.UpdateOnlineConfig(SelectedSource), canUpdateCopyRemove);
            //UpdateAll = ReactiveCommand.CreateFromTask(_controller.UpdateAllOnlineConfig, canUpdateAll);
            CopyLink = ReactiveCommand.Create(() => Clipboard.SetText(SelectedSource), canUpdateCopyRemove);
            Remove = ReactiveCommand.Create(() =>
            {
                bool result;
                var urlToRemove = SelectedSource; // save it here because SelectedSource is lost once we remove the selection
                do
                {
                    result = Sources.Remove(urlToRemove);
                } while (result);
                //_controller.RemoveOnlineConfig(urlToRemove);
            }, canUpdateCopyRemove);
            Add = ReactiveCommand.Create(() =>
            {
                Sources.Add(Address);
                SelectedSource = Address;
                //_controller.SaveOnlineConfigSource(Sources.ToList());
                Address = "";
            }, canAdd);

            // TODO in v5: use MaterialDesignThemes snackbar messages
            this.WhenAnyObservable(x => x.Update)
                .Subscribe(x =>
                {
                    if (x)
                        MessageBox.Show(LocalizationProvider.GetLocalizedValue<string>("sip008UpdateSuccess"));
                    else
                        MessageBox.Show(LocalizationProvider.GetLocalizedValue<string>("sip008UpdateFailure"));
                });
            this.WhenAnyObservable(x => x.UpdateAll)
                .Subscribe(x =>
                {
                    if (x.Count == 0)
                        MessageBox.Show(LocalizationProvider.GetLocalizedValue<string>("sip008UpdateAllSuccess"));
                    else
                    {
                        var stringBuilder = new StringBuilder(LocalizationProvider.GetLocalizedValue<string>("sip008UpdateAllFailure"));
                        foreach (var url in x)
                            stringBuilder.AppendLine(url);
                        MessageBox.Show(stringBuilder.ToString());
                    }
                });
        }

        public ValidationHelper AddressRule { get; }

        public ReactiveCommand<Unit, bool> Update { get; }
        public ReactiveCommand<Unit, List<string>> UpdateAll { get; }
        public ReactiveCommand<Unit, Unit> CopyLink { get; }
        public ReactiveCommand<Unit, Unit> Remove { get; }
        public ReactiveCommand<Unit, Unit> Add { get; }

        [Reactive]
        public ObservableCollection<string> Sources { get; private set; }

        [Reactive]
        public string SelectedSource { get; set; }

        [Reactive]
        public string Address { get; set; }
    }
}
