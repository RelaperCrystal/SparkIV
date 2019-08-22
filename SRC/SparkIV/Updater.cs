/**********************************************************************\

 Spark IV
 Copyright (C) 2008  Arushan/Aru <oneforaru at gmail.com>

 This program is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with this program.  If not, see <http://www.gnu.org/licenses/>.

\**********************************************************************/

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace SparkIV
{
    public class Updater
    {
        private const string VersionUrl = "https://pastebin.com/raw/M6nhwaBw";
        private const string UpdateUrl = "https://pastebin.com/raw/R3wJ0GQ7";
        private const string DownloadListUrl = "https://github.com/ahmed605/SparkIV/releases";

        public static void CheckForUpdate()
        {
            string version = GetWebString(VersionUrl);

            if ( string.IsNullOrEmpty(version))
            {
                DialogResult result =
                    MessageBox.Show(
                        "发生错误。请手动到 Github 页面的 Releases 进行更新。",
                        "错误", MessageBoxButtons.YesNo, MessageBoxIcon.Error);

                if (result == DialogResult.Yes)
                {
                    Process.Start(DownloadListUrl);
                }
            }
            else
            {
                var versionSplit = version.Split(new[] {'.'}, 3);
                int versionCode = 0;
                foreach (var s in versionSplit)
                {
                    versionCode *= 0x100;
                    versionCode += int.Parse(s);
                }

                Version vrs = Assembly.GetExecutingAssembly().GetName().Version;
                int assemblyVersionCode = (vrs.Major * 0x100 + vrs.Minor) * 0x100 + vrs.Build;
                
                if (versionCode > assemblyVersionCode)
                {
                    string message =
                        "有新的 SparkIV 可以使用！要升级新版吗？注意您会丢失当前的汉化。" +
                        "\n" + "\n" + "当前版本为:  " + vrs.Major + "." + vrs.Minor + "." + vrs.Build + "\n"
                        + "新版本为: " + version;

                    DialogResult result = MessageBox.Show(message, "有更新!", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (result == DialogResult.Yes)
                    {
                        var url = GetWebString(UpdateUrl);

                        if ( string.IsNullOrEmpty(url) )
                        {
                            result =
                                MessageBox.Show(
                                    "发生错误。请手动到 Github 页面的 Releases 进行更新。",
                                    "错误", MessageBoxButtons.YesNo, MessageBoxIcon.Error);

                            if (result == DialogResult.Yes)
                            {
                                Process.Start(DownloadListUrl);
                            }
                        }
                        else
                        {
                            Process.Start( url );
                            Application.Exit();                            
                        }
                    }
                }
                else
                {
                    MessageBox.Show(String.Format("当前没有更新。"),
                                    "无更新可用", MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                }
            }
        }

        private static string GetWebString(string url)
        {
            string result;
            try
            {
                var client = new System.Net.WebClient();
                result = client.DownloadString(url);
            }
            catch (Exception ex)
            {
                string errorDetails = String.Empty;
                MessageBoxIcon iconsToShow = MessageBoxIcon.Information;

                if (ex.Message.Contains("could not be resolved"))
                {
                    errorDetails =
                        String.Format(
                            "无法解析更新服务器。\n请检查您的网络连接。");
                    iconsToShow = MessageBoxIcon.Error;
                }
                else if (ex.Message.Contains("404"))
                {
                    errorDetails = "更新服务器无法找到。请稍候再试。";
                    iconsToShow = MessageBoxIcon.Information;
                }
                MessageBox.Show(errorDetails, "服务器不可用", MessageBoxButtons.OK, iconsToShow);
                return null;
            }

            return result;
        }
    }
}