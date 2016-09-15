using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Shadowsocks.View
{
    public partial class HotkeySettingsForm : Form
    {
        /*
         * XXX: The labelName, TextBoxName and callbackName
         *      must follow this rule to make use of reflection
         *
         *      <BaseName><Control-Type-Name>
         */
        private ShadowsocksController _controller;

        // this is a copy of configuration that we are working on
        private HotkeyConfig _modifiedConfig;

        private StringBuilder _sb = new StringBuilder();

        // TODO: not finished
        public HotkeySettingsForm(ShadowsocksController controller)
        {
            InitializeComponent();
            SetupCallbackMap();
            UpdateTexts();
            this.Icon = Icon.FromHandle(Resources.ssw128.GetHicon());

            _controller = controller;
            _controller.ConfigChanged += controller_ConfigChanged;

            LoadCurrentConfiguration();
        }

        private void SetupCallbackMap()
        {
            cbMap["SwitchSystemProxy"] = SwitchSystemProxyCallback;
            cbMap["ChangeToPac"] = ChangeToPacCallback;
            cbMap["ChangeToGlobal"] = ChangeToGlobalCallback;
            cbMap["SwitchAllowLan"] = SwitchAllowLanCallback;
            cbMap["ShowLogs"] = ShowLogsCallback;
            cbMap["ServerMoveUp"] = ServerMoveUpCallback;
            cbMap["ServerMoveDown"] = ServerMoveDownCallback;
        }

        private void LoadConfiguration(HotkeyConfig config)
        {
            SwitchSystemProxyTextBox.Text = config.SwitchSystemProxy;
            ChangeToPacTextBox.Text = config.ChangeToPac;
            ChangeToGlobalTextBox.Text = config.ChangeToGlobal;
            SwitchAllowLanTextBox.Text = config.SwitchAllowLan;
            ShowLogsTextBox.Text = config.ShowLogs;
            ServerMoveUpTextBox.Text = config.ServerMoveUp;
            ServerMoveDownTextBox.Text = config.ServerMoveDown;
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfig = _controller.GetConfigurationCopy().hotkey;
            LoadConfiguration(_modifiedConfig);
        }

        private void UpdateTexts()
        {
            SwitchSystemProxyLabel.Text = I18N.GetString(SwitchSystemProxyLabel.Text);
            ChangeToPacLabel.Text = I18N.GetString(ChangeToPacLabel.Text);
            ChangeToGlobalLabel.Text = I18N.GetString(ChangeToGlobalLabel.Text);
            SwitchAllowLanLabel.Text = I18N.GetString(SwitchAllowLanLabel.Text);
            ShowLogsLabel.Text = I18N.GetString(ShowLogsLabel.Text);
            ServerMoveUpLabel.Text = I18N.GetString(ServerMoveUpLabel.Text);
            ServerMoveDownLabel.Text = I18N.GetString(ServerMoveDownLabel.Text);
            btnOK.Text = I18N.GetString(btnOK.Text);
            btnCancel.Text = I18N.GetString(btnCancel.Text);
            this.Text = I18N.GetString(Text);
        }


        /// <summary>
        /// Capture hotkey - Press key
        /// </summary>
        private void HotkeyDown(object sender, KeyEventArgs e)
        {
            _sb.Length = 0;
            //Combination key only
            if (e.Modifiers != 0)
            {
                // XXX: Hotkey parsing depends on the sequence, more specifically, ModifierKeysConverter.
                // Windows key is reserved by operating system, we deny this key.
                if (e.Control)
                {
                    _sb.Append("Ctrl+");
                }
                if (e.Alt)
                {
                    _sb.Append("Alt+");
                }
                if (e.Shift)
                {
                    _sb.Append("Shift+");
                }

                Keys keyvalue = (Keys) e.KeyValue;
                if ((keyvalue >= Keys.PageUp && keyvalue <= Keys.Down) ||
                    (keyvalue >= Keys.A && keyvalue <= Keys.Z) ||
                    (keyvalue >= Keys.F1 && keyvalue <= Keys.F12))
                {
                    _sb.Append(e.KeyCode);
                }
                else if (keyvalue >= Keys.D0 && keyvalue <= Keys.D9)
                {
                    _sb.Append('D').Append((char) e.KeyValue);
                }
                else if (keyvalue >= Keys.NumPad0 && keyvalue <= Keys.NumPad9)
                {
                    _sb.Append("NumPad").Append((char) (e.KeyValue - 48));
                }
            }
            ((TextBox) sender).Text = _sb.ToString();
        }

        /// <summary>
        /// Capture hotkey - Release key
        /// </summary>
        private void HotkeyUp(object sender, KeyEventArgs e)
        {
            // Don't parse and reg keys if BackSpace is pressed
            if ((Keys)e.KeyValue == Keys.Back) return;

            TextBox tb = sender as TextBox;
            string content = tb.Text.TrimEnd();
            if (content.Length >= 1 && content[content.Length - 1] == '+')
            {
                tb.Text = "";
            }

            // try to register and show result
            var pos = tb.Name.LastIndexOf("TextBox", StringComparison.OrdinalIgnoreCase);
            var rawName = tb.Name.Substring(0, pos);
            var labelName = rawName + "Label";
            var callbackName = rawName + "Callback";
            var hotkey = HotKeys.Str2HotKey(tb.Text);
            if (hotkey == null)
            {
                MessageBox.Show($"Cannot parse hotkey {tb.Text}");
                tb.Clear();
                return;
            }
//            var callback = GetDelegateViaMethodName(this.GetType(), callbackName);
//            if (callback == null)
//            {
//                MessageBox.Show($"No {callbackName}");
//                return;
//            }
            
            object label = GetFieldViaName(this.GetType(), labelName, this);
            if (label == null)
            {
                MessageBox.Show($"No {labelName}");
                return;
            }
            // TODO: dynamic create delegate
            bool regResult = HotKeys.Regist(hotkey, cbMap[rawName]);
            Label lb = label as Label;
            lb.BackColor = regResult ? Color.Green : Color.Yellow;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void OKButton_Click(object sender, EventArgs e)
        {
            if (!CanParseHotkey(this))
            {
                // msgbox i18n
                // let users report this issue
                return;
            }
            // try to register keys
            // if already registered by other progs
            // notify to change

            // use the corresponding label color to indicate
            // reg result.
            // Green: not occupied by others and success
            // Yellow: already reg by others and need action
            // Transparent without color: first run or empty config

            // if first run, no action is needed
            // otherwise, try to register, if failed
            // open this form and notify to change settings

            // All check passed, saving
            SaveConfig();
            this.Close();
        }

        private bool CanParseHotkey(Control control)
        {
            var allTextboxes = HotKeys.GetChildControls<TextBox>(control);
            return allTextboxes.All(tb => HotKeys.Str2HotKey(tb.Text.ToString()) != null);
        }

        private void SaveConfig()
        {
            _modifiedConfig.SwitchSystemProxy = SwitchSystemProxyTextBox.Text;
            _modifiedConfig.ChangeToPac = ChangeToPacTextBox.Text;
            _modifiedConfig.ChangeToGlobal = ChangeToGlobalTextBox.Text;
            _modifiedConfig.SwitchAllowLan = SwitchAllowLanTextBox.Text;
            _modifiedConfig.ShowLogs = ShowLogsTextBox.Text;
            _modifiedConfig.ServerMoveUp = ServerMoveUpTextBox.Text;
            _modifiedConfig.ServerMoveDown = ServerMoveDownTextBox.Text;
            _controller.SaveHotkeyConfig(_modifiedConfig);
        }

        #region Callbacks

        private Dictionary<string, HotKeys.HotKeyCallBackHandler> cbMap = new Dictionary<string, HotKeys.HotKeyCallBackHandler>();
        
        private void SwitchSystemProxyCallback()
        {
            var status = _controller.GetConfigurationCopy().enabled;
            _controller.ToggleEnable(!status);
        }

        private void ChangeToPacCallback()
        {
            _controller.ToggleGlobal(false);
        }

        private void ChangeToGlobalCallback()
        {
            _controller.ToggleGlobal(true);
        }

        private void SwitchAllowLanCallback()
        {
            var status = _controller.GetConfigurationCopy().shareOverLan;
            _controller.ToggleShareOverLAN(!status);
        }

        private void ShowLogsCallback()
        {
            // must use the current MenuViewController
            // thus I choose Reflection
            Assembly ass = Assembly.GetExecutingAssembly();
            FieldInfo fi = ass.GetType("Shadowsocks.Program")
                .GetField("_viewController",
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.IgnoreCase);
            // To retrieve the value of a static field, pass null here
            var mvc = fi.GetValue(null) as MenuViewController;
            mvc.ShowLogForm_HotKey();
        }

        private void ServerMoveUpCallback()
        {
            int currIndex;
            int serverCount;
            GetCurrServerInfo(out currIndex, out serverCount);
            if (currIndex - 1 < 0)
            {
                // revert to last server
                currIndex = serverCount - 1;
            }
            else
            {
                currIndex -= 1;
            }
            _controller.SelectServerIndex(currIndex);
        }

        private void ServerMoveDownCallback()
        {
            int currIndex;
            int serverCount;
            GetCurrServerInfo(out currIndex, out serverCount);
            if (currIndex + 1 == serverCount)
            {
                // revert to first server
                currIndex = 0;
            }
            else
            {
                currIndex += 1;
            }
            _controller.SelectServerIndex(currIndex);
        }

        private void GetCurrServerInfo(out int currIndex, out int serverCount)
        {
            var currConfig = _controller.GetCurrentConfiguration();
            currIndex = currConfig.index;
            serverCount = currConfig.configs.Count;
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">from which type</param>
        /// <param name="name">field name</param>
        /// <param name="obj">pass null if static field</param>
        /// <returns></returns>
        private object GetFieldViaName(Type type, string name, object obj)
        {
            // In general, TextBoxes and Labels are private
            FieldInfo o = type.GetField(name,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.Static);
            return o == null ? null : o.GetValue(obj);
        }

        private object GetDelegateViaMethodName(Type type, string methodname)
        {
            // In general, TextBoxes and Labels are private
            // TODO
            MethodInfo dynMethod = type.GetMethod(methodname,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);


            return null;
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            string content = tb.Text.Trim();
            if (!content.IsNullOrEmpty()) return;

            // reset the corresponding label color
            var pos = tb.Name.LastIndexOf("TextBox", StringComparison.OrdinalIgnoreCase);
            var rawName = tb.Name.Substring(0, pos);
            var labelName = rawName + "Label";
            object label = GetFieldViaName(this.GetType(), labelName, this);
            if (label == null)
            {
                MessageBox.Show($"No {labelName}");
                return;
            }
            Label lb = label as Label;
            lb.BackColor = Color.Empty;
        }
    }
}