using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using Shadowsocks.Net.Settings;
using Shadowsocks.WPF.Models;
using Splat;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Shadowsocks.WPF.ViewModels
{
    public class ForwardProxyViewModel : ReactiveValidationObject
    {
        public ForwardProxyViewModel()
        {
            _forwardProxySettings = Locator.Current.GetService<Settings>().Net.ForwardProxy;

            NoProxy = _forwardProxySettings.NoProxy;
            UseSocks5Proxy = _forwardProxySettings.UseSocks5Proxy;
            UseHttpProxy = _forwardProxySettings.UseHttpProxy;

            Address = _forwardProxySettings.Address;
            Port = _forwardProxySettings.Port;
            Timeout = 5;

            Username = _forwardProxySettings.Username;
            Password = _forwardProxySettings.Password;

            this.WhenAnyValue(x => x.NoProxy, x => !x)
                .ToPropertyEx(this, x => x.CanModifyDetails);

            AddressRule = this.ValidationRule(
                viewModel => viewModel.Address,
                address => !string.IsNullOrWhiteSpace(address),
                "Address can't be empty or whitespaces.");
            PortRule = this.ValidationRule(
                viewModel => viewModel.Port,
                port => port > 0 && port <= 65535,
                port => $"{port} is out of range (0, 65535].");
            TimeoutRule = this.ValidationRule(
                viewModel => viewModel.Timeout,
                timeout => timeout > 0 && timeout <= 10,
                timeout => $"{timeout} is out of range (0, 10].");

            var authValid = this
                .WhenAnyValue(x => x.Username, x => x.Password, (username, password) => new { Username = username, Password = password })
                .Select(x => string.IsNullOrWhiteSpace(x.Username) == string.IsNullOrWhiteSpace(x.Password));
            AuthRule = this.ValidationRule(authValid, "You must provide both username and password.");

            var canSave = this.IsValid();

            Save = ReactiveCommand.Create(() =>
            {
                // TODO: save settings
            }, canSave);
            //Cancel = ReactiveCommand.Create(_menuViewController.CloseForwardProxyWindow);
        }

        private ForwardProxySettings _forwardProxySettings;

        public ValidationHelper AddressRule { get; }
        public ValidationHelper PortRule { get; }
        public ValidationHelper TimeoutRule { get; }
        public ValidationHelper AuthRule { get; }

        public ReactiveCommand<Unit, Unit> Save { get; }
        public ReactiveCommand<Unit, Unit> Cancel { get; }

        [ObservableAsProperty]
        public bool CanModifyDetails { get; }

        [Reactive]
        public bool NoProxy { get; set; }

        [Reactive]
        public bool UseSocks5Proxy { get; set; }

        [Reactive]
        public bool UseHttpProxy { get; set; }

        [Reactive]
        public string Address { get; set; }

        [Reactive]
        public int Port { get; set; }

        [Reactive]
        public int Timeout { get; set; }

        [Reactive]
        public string Username { get; set; }

        [Reactive]
        public string Password { get; set; }

        private ForwardProxySettings GetForwardProxySettings()
            => new ForwardProxySettings()
            {
                NoProxy = NoProxy,
                UseSocks5Proxy = UseSocks5Proxy,
                UseHttpProxy = UseHttpProxy,
                Address = Address,
                Port = Port,
                Username = Username,
                Password = Password,
            };
    }
}
