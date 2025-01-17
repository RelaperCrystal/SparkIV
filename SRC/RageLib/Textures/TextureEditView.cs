﻿/**********************************************************************\

 RageLib - Textures
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
using System.Windows.Forms;

namespace RageLib.Textures
{
    public partial class TextureEditView : UserControl
    {
        public TextureEditView()
        {
            InitializeComponent();
        }

        public int TextureCount
        {
            set
            {
                tslTexturesInfo.Text = "找到 " + value + " 个材质";
            }
        }

        public TextureView TextureView
        {
            get
            {
                return textureView;
            }
        }

        public event EventHandler ExportClicked
        {
            add { tsbExport.Click += value; }
            remove { tsbExport.Click -= value; }
        }

        public event EventHandler ImportClicked
        {
            add { tsbImport.Click += value; }
            remove { tsbImport.Click -= value; }
        }

        public event EventHandler SaveCloseClicked
        {
            add { tsbSaveClose.Click += value; }
            remove { tsbSaveClose.Click -= value; }
        }

    }
}
