namespace Shadowsocks.Interop.V2Ray
{
    public class LogObject
    {
        /// <summary>
        /// Gets or sets the path to the access log file.
        /// Defaults to empty, which prints to stdout.
        /// </summary>
        public string Access { get; set; }

        /// <summary>
        /// Gets or sets the path to the error log file.
        /// Defaults to empty, which prints to stdout.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the log level.
        /// Defaults to warning.
        /// Available values: "debug" | "info" | "warning" | "error" | "none"
        /// </summary>
        public string Loglevel { get; set; }

        public LogObject()
        {
            Access = "";
            Error = "";
            Loglevel = "warning";
        }
    }
}
