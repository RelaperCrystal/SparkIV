﻿/**********************************************************************\

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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using RageLib.FileSystem;
using RageLib.FileSystem.Common;
using SparkIV.Editor;
using SparkIV.Viewer;
using RageLib.Common;
using Directory=RageLib.FileSystem.Common.Directory;
using File=RageLib.FileSystem.Common.File;
using IODirectory = System.IO.Directory;
using IOFile = System.IO.File;
using Ookii.Dialogs;

namespace SparkIV
{
    public partial class MainForm : ExtendedForm
    {
        private static readonly Color CustomDataForeColor = SystemColors.HotTrack;
        private const int SizeColumn = 1;

        private FileSystem _fs;
        private int _sortColumn = -1;

        private string _lastOpenPath;
        private string _lastImportExportPath;

        private Directory _selectedDir;

        public MainForm()
        {
            InitializeComponent();

            Version ver = Assembly.GetExecutingAssembly().GetName().Version;
            tslAbout.Text = "SparkIV " + ver.Major + "." + ver.Minor + "." + ver.Build + " (Beta)" + "\n" +
                            "(C)2008-2019, Ahmed";

            SetInitialUIState();

            lvFiles.AllowDrop = true;
            lvFiles.DragDrop += lvFiles_DragDrop;
            lvFiles.DragEnter += lvFiles_DragEnter;
        }

        private void lvFiles_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void lvFiles_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                //lvFiles.Items.Add(file);
                System.IO.File.Copy(file, Path.Combine(KeyUtil.GetDir.Get(), Path.GetFileName(file)));
            }
            lvFiles.Refresh();
        }

            #region Helpers

            public void OpenFile(string filename, FileSystem fs)
        {
            if (fs == null)
            {
                if (filename.EndsWith(".rpf"))
                {
                    fs = new RPFFileSystem();
                }
                else if (filename.EndsWith(".img"))
                {
                    fs = new IMGFileSystem();
                }
                else if (IODirectory.Exists(filename))
                {
                    fs = new RealFileSystem();
                    filename = (new DirectoryInfo(filename)).FullName;
                }
            }

            if (fs != null)
            {
                if (IOFile.Exists(filename))
                {
                    FileInfo fi = new FileInfo(filename);
                    if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                    {
                        DialogResult result =
                            MessageBox.Show("你试图打开的文件是只读的。" +
                                "是否取消该文件的只读再打开？",
                                "Open", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            fi.Attributes = fi.Attributes & ~FileAttributes.ReadOnly;
                        }
                    }
                }

                try
                {
                    using (new WaitCursor(this))
                    {
                        fs.Open(filename);

                        if (_fs != null)
                        {
                            _fs.Close();
                        }
                        _fs = fs;

                        Text = Application.ProductName + " - " + new FileInfo(filename).Name;
                    }

                    PopulateUI();
                }
                catch (Exception ex)
                {
                    fs.Close();
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string FriendlySize(int size)
        {
            if (size < 1024)
            {
                return size + " B";
            }
            else if (size < 1024 * 1024)
            {
                return size / (1024) + " KB";
            }
            else
            {
                return size / (1024 * 1024) + " MB";
            }
        }

        private void PopulateListView()
        {
            // Redisable some buttons (will be autoenabled based on selection)
            tsbPreview.Enabled = false;
            tsbEdit.Enabled = false;

            Directory dir = _selectedDir;

            string filterString = tstFilterBox.Text;

            List<string> selectedFileNames = new List<string>();
            foreach (var o in lvFiles.SelectedItems)
            {
                selectedFileNames.Add((o as ListViewItem).Text);
            }

            var comparer = lvFiles.ListViewItemSorter;
            lvFiles.ListViewItemSorter = null;

            lvFiles.BeginUpdate();

            lvFiles.Items.Clear();

            using (new WaitCursor(this))
            {
                foreach (var item in dir)
                {
                    if (!item.IsDirectory)
                    {
                        File file = item as File;

                        if (filterString == "" || file.Name.IndexOf(filterString) > -1)
                        {

                            ListViewItem lvi = lvFiles.Items.Add(file.Name);
                            lvi.Tag = file;

                            lvi.SubItems.Add(FriendlySize(file.Size));

                            /*
                            string compressed = file.IsCompressed ? "Yes (" + FriendlySize(file.CompressedSize) + ")" : "No";
                            lvi.SubItems.Add(compressed);
                             */

                            string resources = file.IsResource ? "是" : "否";
                            if (file.IsResource)
                            {
                                string rscType = Enum.IsDefined(file.ResourceType.GetType(), file.ResourceType)
                                                     ?
                                                         file.ResourceType.ToString()
                                                     : string.Format("Unknown 0x{0:x}", (int)file.ResourceType);
                                resources += " (" + rscType + ")";
                            }
                            lvi.SubItems.Add(resources);

                            if (file.IsCustomData)
                            {
                                lvi.ForeColor = CustomDataForeColor;
                            }

                            if (selectedFileNames.Contains(file.Name))
                            {
                                lvi.Selected = true;
                            }

                        }
                    }
                }
            }

            lvFiles.EndUpdate();

            lvFiles.ListViewItemSorter = comparer;
            lvFiles.Sort();
        }

        private void CreateDirectoryNode(TreeNode node, Directory dir)
        {
            node.Tag = dir;

            foreach (var item in dir)
            {
                if (item.IsDirectory)
                {
                    Directory subdir = item as Directory;
                    TreeNode subnode = node.Nodes.Add(subdir.Name);
                    CreateDirectoryNode(subnode, subdir);
                }
            }
        }

        private void SetInitialUIState()
        {
            // Disable some buttons
            tsbOpen.Enabled = false;
            tsbSave.Enabled = false;
            tsbRebuild.Enabled = false;
            tsbExportAll.Enabled = false;
            tsbImport.Enabled = false;
            tsbExportSelected.Enabled = false;
            tsbPreview.Enabled = false;
            tsbEdit.Enabled = false;
            tslFilter.Enabled = false;
            tstFilterBox.Enabled = false;
        }

        private void PopulateUI()
        {
            // Reenable some buttons
            tsbOpen.Enabled = true;
            tsbSave.Enabled = true;
            tsbRebuild.Enabled = _fs.SupportsRebuild;
            tsbExportAll.Enabled = true;
            tsbImport.Enabled = true;
            tsbExportSelected.Enabled = true;
            tslFilter.Enabled = true;
            tstFilterBox.Enabled = true;

            // Redisable some buttons (will be autoenabled based on selection)
            tsbPreview.Enabled = false;
            tsbEdit.Enabled = false;

            _sortColumn = -1;
            lvFiles.ListViewItemSorter = null;

            splitContainer.Panel1Collapsed = !_fs.HasDirectoryStructure;

            tvDir.Nodes.Clear();

            TreeNode root = tvDir.Nodes.Add(_fs.RootDirectory.Name);
            CreateDirectoryNode(root, _fs.RootDirectory);

            root.ExpandAll();
            root.EnsureVisible();

            tvDir.SelectedNode = root;
        }

        private File FindFileByName(string name)
        {
            foreach (var fsObject in _selectedDir)
            {
                File file = fsObject as File;
                if (file != null)
                {
                    if (file.Name.ToLower() == name.ToLower())
                    {
                        return file;
                    }
                }
            }
            return null;
        }

        private void ExtractToPath(Directory dir, string path)
        {
            foreach (var item in dir)
            {
                if (item.IsDirectory)
                {
                    try
                    {
                        IODirectory.CreateDirectory(path + item.Name);
                        ExtractToPath(item as Directory, path + item.Name + "\\");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    File file = item as File;
                    byte[] data = file.GetData();
                    IOFile.WriteAllBytes(Path.Combine(path, file.Name), data);
                }
            }
        }

        private void EditFile(File file)
        {
            if (Editors.HasEditor(file))
            {
                Editors.LaunchEditor(_fs, file);
                if (file.IsCustomData)
                {
                    foreach (ListViewItem item in lvFiles.Items)
                    {
                        if (item.Tag == file)
                        {
                            item.ForeColor = CustomDataForeColor;
                            break;
                        }
                    }
                }
            }
        }

        private void PreviewFile(File file)
        {
            if (Viewers.HasViewer(file))
            {
                Control viewerControl = Viewers.GetControl(file);
                if (viewerControl != null)
                {
                    using (var form = new ViewerForm())
                    {
                        form.SetFilename(file.Name);
                        form.SetControl(viewerControl);
                        form.ShowDialog();
                    }
                }
            }
        }

        private void PreviewOrEditFile(File file)
        {
            if (Viewers.HasViewer(file))
            {
                PreviewFile(file);
            }
            else if (Editors.HasEditor(file))
            {
                EditFile(file);
            }
        }

        private void LoadGameDirectory( KeyUtil keyUtil, string gameName )
        {
            using (new WaitCursor(this))
            {
                FileSystem fs = new RealFileSystem();

                string gamePath = keyUtil.FindGameDirectory();
                while (gamePath == null)
                {
                    var fbd = new VistaFolderBrowserDialog
                    {
                        Description =
                            "无法找到 " + gameName + " 的游戏目录。请选择包含 " + keyUtil.ExecutableName + " 的文件夹。",
                        ShowNewFolderButton = false
                    };

                    if (fbd.ShowDialog() == DialogResult.Cancel)
                    {
                        MessageBox.Show(
                            keyUtil.ExecutableName +
                            " 必须存在以从中提取密钥。 " +
                            "SparkIV 没有了这个文件将无法运作。", "Error", MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    if (System.IO.File.Exists(Path.Combine(fbd.SelectedPath, keyUtil.ExecutableName)))
                    {
                        gamePath = fbd.SelectedPath;
                    }
                }

                byte[] key = keyUtil.FindKey( gamePath );

                if (key == null)
                {
                    string message = "你的 " + keyUtil.ExecutableName + " 可能被修改，或者版本高于本工具支持的版本。 " +
                                    "SparkIV 不能在没有 " + keyUtil.ExecutableName + " 的情况下运行。" + "\n" + "是否检查更新？";
                    string caption = keyUtil.ExecutableName + "被修改或高于支持版本";

                    if (MessageBox.Show(message, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                    {
                        Updater.CheckForUpdate();
                    }

                    return;
                }


                KeyStore.SetKeyLoader(() => key);

                fs.Open(gamePath);

                if (_fs != null)
                {
                    _fs.Close();
                }
                _fs = fs;

                Text = Application.ProductName + " - Browse Game Directory";

                PopulateUI();
            }
        }

        #endregion

        #region Toolbar Handlers

        private void toolStripEFLC_Click(object sender, EventArgs e)
        {
            LoadGameDirectory(new KeyUtilEFLC(), "EFLC");
        }

        private void toolStripGTAIV_Click(object sender, EventArgs e)
        {
            LoadGameDirectory( new KeyUtilGTAIV(), "GTAIV" );
        }

        private void tsbOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "打开 GTAIV 资源映像";
            ofd.Filter = "所有支持的类型|*.rpf;*.img|RPF 文件 (*.rpf)|*.rpf|IMG 文件 (*.img)|*.img";
            ofd.FileName = _lastOpenPath;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _lastOpenPath = ofd.FileName;

                FileSystem fs = null;

                if (ofd.FilterIndex == 2)
                {
                    fs = new RPFFileSystem();
                }
                else if (ofd.FilterIndex == 3)
                {
                    fs = new IMGFileSystem();
                }
                else
                {
                    if (ofd.FileName.EndsWith(".rpf"))
                    {
                        fs = new RPFFileSystem();
                    }
                    else if (ofd.FileName.EndsWith(".img"))
                    {
                        fs = new IMGFileSystem();
                    }
                    else
                    {
                        MessageBox.Show("请选择你正在打开的文件的类型。", "打开 GTAIV 资源映像", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }

                OpenFile(ofd.FileName, fs);
            }

        }

        private void tsbSave_Click(object sender, EventArgs e)
        {
            if (_fs == null) return;

            try
            {
                using (new WaitCursor(this))
                {
                    _fs.Save();
                }

                PopulateListView();

                MessageBox.Show("资源归档已被保存。", "保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("无法保存资源归档。",
                                "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void tsbExportSelected_Click(object sender, EventArgs e)
        {
            if (_fs == null) return;

            if (lvFiles.SelectedItems.Count == 1)
            {
                File file = lvFiles.SelectedItems[0].Tag as File;

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "导出...";

                if (_lastImportExportPath != null)
                {
                    sfd.InitialDirectory = _lastImportExportPath;
                    sfd.FileName = Path.Combine(_lastImportExportPath, file.Name);
                }
                else
                {
                    sfd.FileName = file.Name;
                }


                sfd.OverwritePrompt = true;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    _lastImportExportPath = IODirectory.GetParent(sfd.FileName).FullName;

                    using (new WaitCursor(this))
                    {
                        byte[] data = file.GetData();
                        IOFile.WriteAllBytes(sfd.FileName, data);
                    }
                }
            }
            else if (lvFiles.SelectedItems.Count > 1)
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.Description = "导出已选择...";
                fbd.ShowNewFolderButton = true;
                fbd.SelectedPath = _lastImportExportPath;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _lastImportExportPath = fbd.SelectedPath;

                    string path = fbd.SelectedPath;

                    using (new WaitCursor(this))
                    {
                        foreach (ListViewItem item in lvFiles.SelectedItems)
                        {
                            File file = item.Tag as File;
                            byte[] data = file.GetData();
                            IOFile.WriteAllBytes(Path.Combine(path, file.Name), data);
                        }
                    }

                    MessageBox.Show("所有选择的文件已被保存。", "导出已选择", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void tsbExportAll_Click(object sender, EventArgs e)
        {
            if (_fs == null) return;

            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.Description = "导出所有...";
            fbd.ShowNewFolderButton = true;
            fbd.SelectedPath = _lastImportExportPath;

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                _lastImportExportPath = fbd.SelectedPath;

                string path = fbd.SelectedPath;
                if (!path.EndsWith("\\")) path += "\\";

                using (new WaitCursor(this))
                {
                    ExtractToPath(_fs.RootDirectory, path);
                }

                MessageBox.Show("所有在这个映像中的文件已经全部导出。", "Export All", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void tsbImport_Click(object sender, EventArgs e)
        {
            if (_fs == null) return;

            var ofd = new OpenFileDialog();
            ofd.Title = "导入...";

            if (_lastImportExportPath != null)
            {
                ofd.InitialDirectory = _lastImportExportPath;
            }

            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _lastImportExportPath = IODirectory.GetParent(ofd.FileName).FullName;

                List<string> _invalidFiles = new List<string>();
                using (new WaitCursor(this))
                {
                    for (var i = 0; i < ofd.FileNames.Length; i++)
                    {
                        var safename = Path.GetFileName(ofd.FileNames[i]);
                        File file = FindFileByName(safename);
                        if (file == null)
                        {
                            _invalidFiles.Add(safename);
                        }
                        else
                        {
                            byte[] data = IOFile.ReadAllBytes(ofd.FileNames[i]);
                            file.SetData(data);
                        }
                    }
                }

                if (_invalidFiles.Count > 0)
                {
                    var sb = new StringBuilder();
                    foreach (var s in _invalidFiles)
                    {
                        sb.Append("  " + s + "\n");
                    }
                    MessageBox.Show("以下文件没有在映像中找到替换件：\n\n" + sb +
                                    "\n您不能导入新文件，只能替换旧文件。这些文件必须与其在映像内要替换的文件名一模一样。", "Import", MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation);
                }

                PopulateListView();
            }
        }

        private void tsbRebuild_Click(object sender, EventArgs e)
        {
            if (_fs == null) return;

            try
            {
                using (new WaitCursor(this))
                {
                    _fs.Rebuild();
                }

                PopulateListView();

                MessageBox.Show("映像已被重建。", "重建", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("无法重建映像。\n\n" +
                                "当前只有 IMG 映像能被重建，重建 RPF 映像在当前是不支持的。",
                                "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void tstFilterBox_TextChanged(object sender, EventArgs e)
        {
            if (_fs == null) return;

            PopulateListView();
        }

        private void tsbPreview_Click(object sender, EventArgs e)
        {
            if (lvFiles.SelectedItems.Count == 1)
            {
                var file = lvFiles.SelectedItems[0].Tag as File;
                PreviewFile(file);
            }
        }


        private void tsbEdit_Click(object sender, EventArgs e)
        {
            if (lvFiles.SelectedItems.Count == 1)
            {
                var file = lvFiles.SelectedItems[0].Tag as File;
                EditFile(file);
            }
        }

        #endregion

        #region Event Handlers

        private void tvDir_AfterSelect(object sender, TreeViewEventArgs e)
        {
            Directory dir = (e.Node.Tag as Directory);
            _selectedDir = dir;
            PopulateListView();
        }

        private void lvFiles_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hitTest = lvFiles.HitTest(e.X, e.Y);
            if (hitTest.Item != null)
            {
                var file = hitTest.Item.Tag as File;

                PreviewOrEditFile(file);
            }
        }

        private void lvFiles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (lvFiles.SelectedItems.Count == 1)
                {
                    var file = lvFiles.SelectedItems[0].Tag as File;
                    PreviewOrEditFile(file);
                }
            }

            if (e.KeyCode == Keys.Delete)
            {
                DialogResult dialogResult = MessageBox.Show("Are you sure to delete this file : " + lvFiles.SelectedItems[0].Text, "Delete", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    //do something
                    if (System.IO.File.Exists(Path.Combine(KeyUtil.GetDir.Get(), lvFiles.SelectedItems[0].Text)))
                    {
                        System.IO.File.Delete(Path.Combine(KeyUtil.GetDir.Get(), lvFiles.SelectedItems[0].Text));
                        lvFiles.Items.Remove(lvFiles.SelectedItems[0]);
                    }
                }
                else if (dialogResult == DialogResult.No)
                {
                    //do something else
                }
            }

            //if (e.KeyCode == Keys.C && e.Control)
            //{
              //  Clipboard.SetData(DataFormats.FileDrop, Path.Combine(KeyUtil.GetDir.Get(), lvFiles.SelectedItems[0].Text));
                //Clipboard.SetFileDropList(Path.Combine(KeyUtil.GetDir.Get(), lvFiles.SelectedItems[0].Text));
            //}
           // if (e.KeyCode == Keys.V && e.Control)
           // {
              //  if (Clipboard.ContainsFileDropList())
              //  {
                    //copy to D:\test
               //     foreach (string source in Clipboard.GetFileDropList())
                //    {
               //         System.IO.File.Copy(source, KeyUtil.GetDir.Get());
               //         lvFiles.Items.Add(Path.GetFileName(source));
               //         lvFiles.Sorting = SortOrder.Ascending;
                //    }
               // }
           // }
        }

        private void lvFiles_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column != _sortColumn)
            {
                _sortColumn = e.Column;
                lvFiles.Sorting = SortOrder.Ascending;
            }
            else
            {
                lvFiles.Sorting = lvFiles.Sorting == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }

            if (e.Column != SizeColumn)
            {
                lvFiles.ListViewItemSorter = new ListViewItemComparer(e.Column, lvFiles.Sorting == SortOrder.Descending);
            }
            else
            {
                lvFiles.ListViewItemSorter = new ListViewItemComparer(lvFiles.Sorting == SortOrder.Descending);
            }

            lvFiles.Sort();
        }

        private void lvFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvFiles.SelectedItems.Count == 1)
            {
                var file = lvFiles.SelectedItems[0].Tag as File;
                tsbPreview.Enabled = Viewers.HasViewer(file);
                tsbEdit.Enabled = Editors.HasEditor(file);
            }
            else
            {
                tsbPreview.Enabled = false;
                tsbEdit.Enabled = false;
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (_fs != null)
            {
                _fs.Close();
            }
        }

        private void tslAbout_Click(object sender, EventArgs e)
        {
            Updater.CheckForUpdate();
        }

        #endregion

        private void tstFilterBox_Click(object sender, EventArgs e)
        {

        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }
}