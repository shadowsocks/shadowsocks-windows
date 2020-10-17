using Newtonsoft.Json.Linq;
using ReactiveUI;
using Shadowsocks.Controller;
using System.Reactive;

namespace Shadowsocks.ViewModels
{
    public class VersionUpdatePromptViewModel : ReactiveObject
    {
        public VersionUpdatePromptViewModel(JToken releaseObject)
        {
            _updateChecker = Program.MenuController.updateChecker;
            _releaseObject = releaseObject;
            ReleaseNotes = string.Concat(
                $"# {((bool)_releaseObject["prerelease"] ? "⚠ Pre-release" : "ℹ Release")} {(string)_releaseObject["tag_name"] ?? "Failed to get tag name"}\r\n",
                (string)_releaseObject["body"] ?? "Failed to get release notes");

            Update = ReactiveCommand.CreateFromTask(_updateChecker.DoUpdate);
            SkipVersion = ReactiveCommand.Create(_updateChecker.SkipUpdate);
            NotNow = ReactiveCommand.Create(_updateChecker.CloseVersionUpdatePromptWindow);
        }

        private readonly UpdateChecker _updateChecker;
        private readonly JToken _releaseObject;

        public string ReleaseNotes { get; }

        public ReactiveCommand<Unit, Unit> Update { get; }

        public ReactiveCommand<Unit, Unit> SkipVersion { get; }

        public ReactiveCommand<Unit, Unit> NotNow { get; }
    }
}
