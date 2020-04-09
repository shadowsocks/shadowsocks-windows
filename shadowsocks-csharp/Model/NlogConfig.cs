using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Shadowsocks.Model
{
    public class NLogConfig
    {
        public enum LogLevel
        {
            Fatal,
            Error,
            Warn,
            Info,
            Debug,
            Trace,
        }

        const string NLOG_CONFIG_FILE_NAME = "NLog.config";
        const string TARGET_MIN_LEVEL_ATTRIBUTE = "minlevel";
        const string LOGGER_FILE_NAME_ATTRIBUTE = "fileName";

        XmlDocument doc = new XmlDocument();
        XmlElement logFileNameElement;
        XmlElement logLevelElement;

        /// <summary>
        /// Load the NLog config xml file content
        /// </summary>
        public static NLogConfig LoadXML()
        {
            NLogConfig config = new NLogConfig();
            config.doc.Load(NLOG_CONFIG_FILE_NAME);
            config.logLevelElement = (XmlElement)SelectSingleNode(config.doc, "//nlog:logger[@name='*']");
            config.logFileNameElement = (XmlElement)SelectSingleNode(config.doc, "//nlog:target[@name='file']");
            return config;
        }

        /// <summary>
        /// Save the content to NLog config xml file
        /// </summary>
        public static void SaveXML(NLogConfig nLogConfig)
        {
            nLogConfig.doc.Save(NLOG_CONFIG_FILE_NAME);
        }


        /// <summary>
        /// Get the current minLogLevel from xml file
        /// </summary>
        /// <returns></returns>
        public LogLevel GetLogLevel()
        {
            LogLevel level = LogLevel.Warn;
            string levelStr = logLevelElement.GetAttribute(TARGET_MIN_LEVEL_ATTRIBUTE);
            Enum.TryParse(levelStr, out level);
            return level;
        }

        /// <summary>
        /// Get the target fileName from xml file
        /// </summary>
        /// <returns></returns>
        public string GetLogFileName()
        {
            return logFileNameElement.GetAttribute(LOGGER_FILE_NAME_ATTRIBUTE);
        }

        /// <summary>
        /// Set the minLogLevel to xml file
        /// </summary>
        /// <param name="logLevel"></param>
        public void SetLogLevel(LogLevel logLevel)
        {
            logLevelElement.SetAttribute(TARGET_MIN_LEVEL_ATTRIBUTE, logLevel.ToString("G"));
        }

        /// <summary>
        /// Set the target fileName to xml file
        /// </summary>
        /// <param name="fileName"></param>
        public void SetLogFileName(string fileName)
        {
            logFileNameElement.SetAttribute(LOGGER_FILE_NAME_ATTRIBUTE, fileName);
        }

        /// <summary>
        /// Select a single XML node/elemant
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="xpath"></param>
        /// <returns></returns>
        private static XmlNode SelectSingleNode(XmlDocument doc, string xpath)
        {
            XmlNamespaceManager manager = new XmlNamespaceManager(doc.NameTable);
            manager.AddNamespace("nlog", "http://www.nlog-project.org/schemas/NLog.xsd");
            //return doc.SelectSingleNode("//nlog:logger[(@shadowsocks='managed') and (@name='*')]", manager);
            return doc.SelectSingleNode(xpath, manager);
        }

        /// <summary>
        /// Extract the pre-defined NLog configuration file is does not exist. Then reload the Nlog configuration.
        /// </summary>
        public static void TouchAndApplyNLogConfig()
        {
            try
            {
                if (File.Exists(NLOG_CONFIG_FILE_NAME))
                    return; // NLog.config exists, and has already been loaded

                File.WriteAllText(NLOG_CONFIG_FILE_NAME, Properties.Resources.NLog_config);
            }
            catch (Exception ex)
            {
                NLog.Common.InternalLogger.Error(ex, "[shadowsocks] Failed to setup default NLog.config: {0}", NLOG_CONFIG_FILE_NAME);
                return;
            }

            LoadConfiguration();    // Load the new config-file
        }

        /// <summary>
        /// NLog reload the config file and apply to current LogManager
        /// </summary>
        public static void LoadConfiguration()
        {
            LogManager.LoadConfiguration(NLOG_CONFIG_FILE_NAME);
        }
    }
}
