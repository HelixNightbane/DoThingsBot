using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace DoThingsBot.Lib {
    public class GitLabTagData {
        public string tag_name = "";
        public DateTime created_at = DateTime.MinValue;

    }

    public static class UpdateChecker {
        private static string json = "";

        public static void CheckForUpdate() {
            new Action(FetchGitlabData).BeginInvoke(OnGitlabFetchComplete, null);

            //new Newtonsoft.Json.Serialization.Action(FetchGitlabData).BeginInvoke(new AsyncCallback(OnGitlabFetchComplete), null);
        }

        public static void FetchGitlabData() {

            // no tls 1.2 in dotnet 3.5???
            try {
                var url = string.Format(@"http://http.haxit.org/dtbupdatecheck.php");

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 10000;
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    using (Stream stream = response.GetResponseStream()) {
                        using (StreamReader reader = new StreamReader(stream)) {
                            json = reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex) { }
        }

        private static void OnGitlabFetchComplete(IAsyncResult result) {
            try {
                if (!string.IsNullOrEmpty(json)) {
                    try {
                        var tags = JsonConvert.DeserializeObject<GitLabTagData[]>(json);

                        Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                        FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                        Version version = new Version(fvi.FileVersion);

                        foreach (var tag in tags) {
                            try {
                                Version releaseVersion = new Version(tag.tag_name.Replace("release-", ""));

                                if (releaseVersion.CompareTo(version) == 1) {
                                    Globals.Core.Actions.AddChatText("[" + Globals.PluginName + "] " + "A new version of DoThingsBot is available for download! https://gitlab.com/trevis/dothingsbot", 3);
                                    break;
                                }
                            }
                            catch (Exception ex) { Util.LogException(ex); }
                        }
                    }
                    catch (Exception ex) { Util.LogException(ex); }
                }
            }
            catch (Exception ex) {
                Util.LogException(ex);
            }
        }
    }
}
