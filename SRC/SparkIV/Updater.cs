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
                        "�����������ֶ��� Github ҳ��� Releases ���и��¡�",
                        "����", MessageBoxButtons.YesNo, MessageBoxIcon.Error);

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
                        MessageBox.Show("��������");
                        return;
                    }

                    if (upd.id != "v2.1")
                    {
                        
                        client.DownloadFile(upd.downloadUrl, "updsetup.exe");
                        MessageBox.Show("�ҵ����¡���������ɡ�");
                        Process.Start("updsetup.exe");
                        Application.Exit();
                    }
                    else
                    {
                        MessageBox.Show("��ǰ�޺������¡�");
                    }
                    
                }
                catch
                {
                    MessageBox.Show("��������");
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
                            "�޷��������·�������\n���������������ӡ�");
                    iconsToShow = MessageBoxIcon.Error;
                }
                else if (ex.Message.Contains("404"))
                {
                    errorDetails = "���·������޷��ҵ������Ժ����ԡ�";
                    iconsToShow = MessageBoxIcon.Information;
                }
                MessageBox.Show(errorDetails, "������������", MessageBoxButtons.OK, iconsToShow);
                return null;
            }

            return result;
        }
    }
}