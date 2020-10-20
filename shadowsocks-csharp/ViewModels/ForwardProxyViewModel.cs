using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.View;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Shadowsocks.ViewModels
{
    public class ForwardProxyViewModel : ReactiveValidationObject
    {
        public ForwardProxyViewModel()
        {
            _config = Program.MainController.GetCurrentConfiguration();
            _controller = Program.MainController;
            _menuViewController = Program.MenuController;

            if (!_config.proxy.useProxy)
                NoProxy = true;
            else if (_config.proxy.proxyType == 0)
                UseSocks5Proxy = true;
            else
                UseHttpProxy = true;

            Address = _config.proxy.proxyServer;
            Port = _config.proxy.proxyPort;
            Timeout = _config.proxy.proxyTimeout;

            Username = _config.proxy.authUser;
            Password = _config.proxy.authPwd;

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
                _controller.SaveProxy(GetForwardProxyConfig());
                _menuViewController.CloseForwardProxyWindow();
            }, canSave);
            Cancel = ReactiveCommand.Create(_menuViewController.CloseForwardProxyWindow);
        }

        private readonly Configuration _config;
        private readonly ShadowsocksController _controller;
        private readonly MenuViewController _menuViewController;

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

        private ForwardProxyConfig GetForwardProxyConfig()
        {
            var forwardProxyConfig = new ForwardProxyConfig()
            {
                proxyServer = Address,
                proxyPort = Port,
                proxyTimeout = Timeout,
                authUser = Username,
                authPwd = Password
            };
            if (NoProxy)
                forwardProxyConfig.useProxy = false;
            else if (UseSocks5Proxy)
            {
                forwardProxyConfig.useProxy = true;
                forwardProxyConfig.proxyType = 0;
            }
            else
            {
                forwardProxyConfig.useProxy = true;
                forwardProxyConfig.proxyType = 1;
            }
            return forwardProxyConfig;
        }
    }
}
