using ReactiveUI;
using Shadowsocks.WPF.Services;
using Splat;
using System.Reactive;
using System.Text.Json;

namespace Shadowsocks.WPF.ViewModels
{
    public class VersionUpdatePromptViewModel : ReactiveObject
    {
        public VersionUpdatePromptViewModel(JsonElement releaseObject)
        {
            _updateChecker = Locator.Current.GetService<UpdateChecker>();
            _releaseObject = releaseObject;
            var releaseTagName = _releaseObject.GetProperty("tag_name").GetString();
            var releaseNotes = _releaseObject.GetProperty("body").GetString();
            var releaseIsPrerelease = _releaseObject.GetProperty("prerelease").GetBoolean();
            ReleaseNotes = string.Concat(
                $"# {(releaseIsPrerelease ? "⚠ Pre-release" : "ℹ Release")} {releaseTagName ?? "Failed to get tag name"}\r\n",
                releaseNotes ?? "Failed to get release notes");

            Update = ReactiveCommand.CreateFromTask(_updateChecker.DoUpdate);
            SkipVersion = ReactiveCommand.Create(_updateChecker.SkipUpdate);
            NotNow = ReactiveCommand.Create(_updateChecker.CloseVersionUpdatePromptWindow);
        }

        private readonly UpdateChecker _updateChecker;
        private readonly JsonElement _releaseObject;

        public string ReleaseNotes { get; }

        public ReactiveCommand<Unit, Unit> Update { get; }

        public ReactiveCommand<Unit, Unit> SkipVersion { get; }

        public ReactiveCommand<Unit, Unit> NotNow { get; }
    }
}
