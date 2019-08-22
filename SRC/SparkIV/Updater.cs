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
using Newtonsoft.Json;

namespace SparkIV
{
    public class Updater
    {
        private const string VersionUrl = "https://raw.githubusercontent.com/RelaperCrystal/SparkIV/master/zhcnonline/CurrentTranslation.json";
        private const string UpdateUrl = "https://pastebin.com/raw/R3wJ0GQ7";
        private const string DownloadListUrl = "https://github.com/ahmed605/SparkIV/releases";

        public static void CheckForUpdate()
        {
            string version = GetWebString(VersionUrl);
            Update upd;
            
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
                try
                {
                    upd = JsonConvert.DeserializeObject<Update>(version);
                    var client = new System.Net.WebClient();
                    if (string.IsNullOrEmpty(upd.downloadUrl))
                    {
                        MessageBox.Show("发生错误。");
                        return;
                    }

                    if (upd.id != "v2.1")
                    {
                        
                        client.DownloadFile(upd.downloadUrl, "updsetup.exe");
                        MessageBox.Show("找到更新。下载以完成。");
                        Process.Start("updsetup.exe");
                        Application.Exit();
                    }
                    else
                    {
                        MessageBox.Show("当前无汉化更新。");
                    }
                    
                }
                catch
                {
                    MessageBox.Show("发生错误。");
                    return;
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