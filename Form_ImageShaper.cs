using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;

using System.IO;
using System.Diagnostics;

namespace ImageShaper
{
    public partial class Form_ImageShaper : Form
    {
        public const string ProgramName = "Image Shaper";
        private ContextMenuStrip dataGrid_CM;

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "ShowWindow")]
        private static extern int ShowWindow(IntPtr hWnd, uint Msg);

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        private static extern int SetWindowPos(int hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        static void ShowInactiveTopmost(Form frm)
        {
            if ((frm == null) || (frm.IsDisposed))
                return;
            try
            {
                ShowWindow(frm.Handle, 4);
                SetWindowPos(frm.Handle.ToInt32(), 0, frm.Left, frm.Top, frm.Width, frm.Height, 0x0010);
            }
            catch { }
        }

        private string GetProgramPath
        {
            get { return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location); }
        }
        private string GetProgramTempPath
        {
            get { return Path.Combine(GetProgramPath, "Temp"); }
        }

        /// <summary>
        /// show index number in row header
        /// </summary>
        void dataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            var rowIdx = e.RowIndex.ToString("00000");

            var centerFormat = new StringFormat()
            {
                // use right alignment for numbers
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            };

            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth - 5, e.RowBounds.Height);
            if (e.RowIndex < this.dataGridView_Files.RowCount - 1)
                e.Graphics.DrawString(rowIdx, this.dataGridView_Files.Font, SystemBrushes.ControlText, headerBounds, centerFormat);
        }

        void dataGridView1_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            DataGridViewColumn column = this.dataGridView_Files.Columns[e.ColumnIndex];
            if (this.dataGridView_Files.Rows.Count > 0)
            {
                bool selected = !this.dataGridView_Files.Rows[0].Cells[column.Index].Selected;
                foreach (DataGridViewRow row in this.dataGridView_Files.Rows)
                {
                    row.Cells[column.Index].Selected = selected;
                }
            }
            UpdatePreview();
        }

        void Form_ImageShaper_WindowChange(object sender, EventArgs e)
        {
            if (form_Preview != null)
                form_Preview.Location = new Point(this.Location.X + this.Width, this.Location.Y);
        }

        private ImageFormat getImageFormat(string extension)
        {
            switch (extension.ToLower())
            {
                case "png": return ImageFormat.Png;
                case "bmp": return ImageFormat.Bmp;
                case "gif": return ImageFormat.Gif;
                case "tiff": return ImageFormat.Tiff;
                default: return null;
            }
        }

        public SHP_TS_EncodingFormat GetDefaultCompression
        {
            get
            {
                //the 0. item is the "Undefined" compression, which is not present in the global compression combobox
                return (SHP_TS_EncodingFormat)(this.toolStripComboBox_DefaultCompression.SelectedIndex + 1);
            }
        }

        void Form_ImageShaper_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (RunAsCommand) return;//don't save anything when run as console command

            if ((this.Location.X >= 0) && (this.Location.Y >= 0))
            {
                Cinimanager.inisettings.StartPosition = this.Location;
                Cinimanager.inisettings.StartSize = this.Size;
            }
            Cinimanager.inisettings.PreventTSWobbleBug = this.checkBox_PreventWobbleBug.Checked;

            Cinimanager.inisettings.CreateImages = this.checkBox_FrameFiles.Checked;
            Cinimanager.inisettings.CreateImages_FileName = this.textBox_CreateFiles.Text;
            Cinimanager.inisettings.CreateImages_Format = this.comboBox_CreateFilesFormat.SelectedItem.ToString();
            if (this.uC_Palette1.Palette != null)
                Cinimanager.inisettings.LastPalette = this.uC_Palette1.Palette.PaletteFile;
            else Cinimanager.inisettings.LastPalette = "";
            Cinimanager.inisettings.ShowPreview = ShowPreview;

            Cinimanager.inisettings.RadarColor = (Color)this.button_RadarColor.Tag;
            Cinimanager.inisettings.AverageRadarColor = this.checkBox_RadarColorAverage.Checked;

            Cinimanager.inisettings.UseCustomBackgroundColor = this.checkBox_UseCustomBackgroundColor.Checked;
            Cinimanager.inisettings.CustomBackgroundColor = (Color)this.button_CustomBackgroundColor.Tag;
            Cinimanager.inisettings.CombineTransparentPixel = this.checkBox_CombineTransparency.Checked;

            Cinimanager.inisettings.OptimizeCanvas = this.checkBox_OptimizeCanvas.Checked;
            Cinimanager.inisettings.KeepCentered = this.checkBox_KeepCentered.Checked;

            Cinimanager.inisettings.OutputFolder = this.toolStripMenuItem_Outputfolder.ToolStrip_UC_FolderSelector.Value;
            Cinimanager.inisettings.PreviewBackgroundImage = this.toolStripMenuItem_previewBackgroundImage.ToolStrip_UC_FolderSelector.Value;

            Cinimanager.SaveIniSettings();
        }

        public Form_ImageShaper()
        {
            InitializeComponent();
            System.Reflection.Assembly thisAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            string version = thisAssembly.GetName().Version.Major.ToString("D2") + "." +
                                     thisAssembly.GetName().Version.Minor.ToString("D2") + "." +
                                     thisAssembly.GetName().Version.Build.ToString("D2") + "." +
                                     thisAssembly.GetName().Version.Revision.ToString("D2");
            this.FormClosing += new FormClosingEventHandler(Form_ImageShaper_FormClosing);
            this.Text = ProgramName + " v" + version;
            this.StartPosition = FormStartPosition.Manual;


            this.dataGridView_BitFields.Rows.Add();
            foreach (DataGridViewCell c in this.dataGridView_BitFields.Rows[0].Cells)
                c.Value = false;
            SetBitField(SHP_TS_BitFlags.UnknownBit1, SHP_TS_EncodingFormat.Undefined);
            this.dataGridView_BitFields.CellPainting += new DataGridViewCellPaintingEventHandler(dataGridView_BitFields_CellPainting);
            this.dataGridView_BitFields.CellClick += new DataGridViewCellEventHandler(dataGridView_BitFields_CellClick);


            this.comboBox_Compression.Items.Clear();
            foreach (SHP_TS_EncodingFormat ef in Enum.GetValues(typeof(SHP_TS_EncodingFormat)))
            {
                this.comboBox_Compression.Items.Add(ef.ToString());
            }
            this.comboBox_Compression.SelectedIndex = 0;


            this.toolStripComboBox_DefaultCompression.Items.Clear();
            foreach (SHP_TS_EncodingFormat ef in Enum.GetValues(typeof(SHP_TS_EncodingFormat)))
            {
                if (ef != SHP_TS_EncodingFormat.Undefined)
                    this.toolStripComboBox_DefaultCompression.Items.Add(ef);
            }
            this.toolStripComboBox_DefaultCompression.SelectedIndex = 1;

            this.comboBox_CreateFilesFormat.Items.Clear();
            this.comboBox_CreateFilesFormat.Items.Add("PNG");
            this.comboBox_CreateFilesFormat.Items.Add("BMP");
            this.comboBox_CreateFilesFormat.Items.Add("GIF");
            this.comboBox_CreateFilesFormat.Items.Add("TIFF");
            this.comboBox_CreateFilesFormat.Items.Add("SHP(TS)");
            this.comboBox_CreateFilesFormat.SelectedIndex = 0;


            ///Load the INI
            Cinimanager.inifilename = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ImageShaper.ini");
            Cinimanager.LoadIniSettings();
            if ((Cinimanager.inisettings.StartPosition.X >= 0) && (Cinimanager.inisettings.StartPosition.Y >= 0))
                this.Location = Cinimanager.inisettings.StartPosition;

            Boolean isWindowOnScreen = false;
            for (int i = 0; i < System.Windows.Forms.Screen.AllScreens.Length; i++)
            {
                if (
                    (this.Location.X < System.Windows.Forms.Screen.AllScreens[i].WorkingArea.Right - 10) &&
                    (this.Location.Y < System.Windows.Forms.Screen.AllScreens[i].WorkingArea.Bottom - 10) &&
                    (this.Location.X >= System.Windows.Forms.Screen.AllScreens[i].WorkingArea.Left) &&
                    (this.Location.Y >= System.Windows.Forms.Screen.AllScreens[i].WorkingArea.Top - 18)
                    )
                {
                    isWindowOnScreen = true;
                    break;
                }
            }
            if (!isWindowOnScreen)
                for (int i = 0; i < System.Windows.Forms.Screen.AllScreens.Length; i++)
                    if (System.Windows.Forms.Screen.AllScreens[i].Primary)
                        this.Location = System.Windows.Forms.Screen.AllScreens[i].Bounds.Location;


            this.Size = Cinimanager.inisettings.StartSize;
            this.checkBox_PreventWobbleBug.Checked = Cinimanager.inisettings.PreventTSWobbleBug;

            for (int i = 0; i < this.toolStripComboBox_DefaultCompression.Items.Count; i++)
            {
                if (Cinimanager.inisettings.DefaultCompression == (SHP_TS_EncodingFormat)this.toolStripComboBox_DefaultCompression.Items[i])
                    this.toolStripComboBox_DefaultCompression.SelectedIndex = i;
            }

            this.checkBox_FrameFiles.Checked = Cinimanager.inisettings.CreateImages;
            this.textBox_CreateFiles.Text = Cinimanager.inisettings.CreateImages_FileName;
            for (int i = 0; i < this.comboBox_CreateFilesFormat.Items.Count; i++)
            {
                if (this.comboBox_CreateFilesFormat.Items[i].ToString() == Cinimanager.inisettings.CreateImages_Format)
                    this.comboBox_CreateFilesFormat.SelectedIndex = i;
            }

            this.checkBox_OptimizeCanvas.Checked = Cinimanager.inisettings.OptimizeCanvas;
            this.checkBox_KeepCentered.Checked = Cinimanager.inisettings.KeepCentered;

            if (Cinimanager.inisettings.LastPalette != "")
                this.uC_Palette1.LoadPalette(Cinimanager.inisettings.LastPalette);
            this.ShowPreview = Cinimanager.inisettings.ShowPreview;


            this.button_RadarColor.Tag = Cinimanager.inisettings.RadarColor;
            this.button_RadarColor.Text = Cinimanager.ColorToStr(Cinimanager.inisettings.RadarColor, true);
            this.checkBox_RadarColorAverage.Checked = Cinimanager.inisettings.AverageRadarColor;

            this.checkBox_UseCustomBackgroundColor.Checked = true;
            this.checkBox_UseCustomBackgroundColor.Checked = Cinimanager.inisettings.UseCustomBackgroundColor;
            this.button_CustomBackgroundColor.Tag = Cinimanager.inisettings.CustomBackgroundColor;
            this.button_CustomBackgroundColor.Text = Cinimanager.ColorToStr(Cinimanager.inisettings.CustomBackgroundColor, true);
            this.checkBox_CombineTransparency.Checked = Cinimanager.inisettings.CombineTransparentPixel;

            this.toolStripMenuItem_Outputfolder.ToolStrip_UC_FolderSelector.Value = Cinimanager.inisettings.OutputFolder;
            this.toolStripMenuItem_previewBackgroundImage.ToolStrip_UC_FolderSelector.Value = Cinimanager.inisettings.PreviewBackgroundImage;

            ///end of ini loading


            this.dataGridView_Files.RowPostPaint += new DataGridViewRowPostPaintEventHandler(dataGridView1_RowPostPaint);//show index nr in row header
            this.Resize += new EventHandler(Form_ImageShaper_WindowChange);
            this.Move += new EventHandler(Form_ImageShaper_WindowChange);

            this.dataGridView_Files.AllowDrop = true;
            this.dataGridView_Files.DragDrop += new DragEventHandler(dataGridView1_DragDrop);
            this.dataGridView_Files.DragEnter += new DragEventHandler(dataGridView1_DragEnter);
            this.dataGridView_Files.KeyUp += new KeyEventHandler(dataGridView1_KeyUp);

            this.dataGridView_Files.MouseDown += new MouseEventHandler(dataGridView_Files_MouseDown);
            this.dataGridView_Files.MouseMove += new MouseEventHandler(dataGridView_Files_MouseMove);
            this.dataGridView_Files.DragOver += new DragEventHandler(dataGridView_Files_DragOver);

            this.dataGridView_Files.MouseClick += new MouseEventHandler(dataGridView1_MouseClick);
            this.dataGridView_Files.ColumnHeaderMouseClick += new DataGridViewCellMouseEventHandler(dataGridView1_ColumnHeaderMouseClick);

            dataGrid_CM = new ContextMenuStrip();
            dataGrid_CM.Items.Add("设置调色板");
            dataGrid_CM.Items.Add("设置位/压缩");
            dataGrid_CM.Items.Add("-");
            dataGrid_CM.Items.Add("从剪贴板加载");
            dataGrid_CM.Items.Add("-");
            dataGrid_CM.Items.Add("加载图像/SHP");
            dataGrid_CM.Items.Add("加载和拆分图像");
            dataGrid_CM.Items.Add("-");
            dataGrid_CM.Items.Add("所选单元格的相反顺序");
            dataGrid_CM.Items.Add("-");
            dataGrid_CM.Items.Add("复制");
            dataGrid_CM.Items.Add("剪切");
            dataGrid_CM.Items.Add("粘贴");
            dataGrid_CM.Items[0].Click += new EventHandler(DataGridCell_SetPalette);
            dataGrid_CM.Items[1].Click += new EventHandler(DataGridCell_SetCompression);
            dataGrid_CM.Items[3].Click += new EventHandler(DataGridCell_LoadFromClipboard);
            dataGrid_CM.Items[5].Click += new EventHandler(DataGridCell_LoadImages);
            dataGrid_CM.Items[6].Click += new EventHandler(DataGridCell_LoadSplitImage);
            dataGrid_CM.Items[8].Click += new EventHandler(DataGridCell_ReverseOrder);
            dataGrid_CM.Items[10].Click += new EventHandler(DataGridCell_Copy);
            dataGrid_CM.Items[11].Click += new EventHandler(DataGridCell_Cut);
            dataGrid_CM.Items[12].Click += new EventHandler(DataGridCell_Paste);

            //changes are now instantly applied to the selected cells
            dataGrid_CM.Items[1].Visible = false;

            this.uC_Palette1.initialDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (Directory.Exists(Path.Combine(this.uC_Palette1.initialDirectory, "Palettes")))
                this.uC_Palette1.initialDirectory = Path.Combine(this.uC_Palette1.initialDirectory, "Palettes");

            this.uC_Palette1.PaletteChanged += new EventHandler<EventArgs>(uC_Palette1_PaletteChanged);

            ToolTip toolTip1 = new ToolTip();
            // Set up the delays for the ToolTip.
            toolTip1.AutoPopDelay = 1000000;
            toolTip1.InitialDelay = 100;
            toolTip1.ReshowDelay = 100;
            // Force the ToolTip text to be displayed whether or not the form is active.
            toolTip1.ShowAlways = true;

            // Set up the ToolTip text for the Button and Checkbox.
            toolTip1.SetToolTip(this.toolStripComboBox_DefaultCompression.Control, "要用于具有“未定义”压缩集的所有映像的默认压缩.\n未压缩的是SHP Builders Compression.\nRLE_Zero 是 SHP Builders Compression 3.\n检测最佳大小首先尝试RLE_Zero. 如果结果比未压缩大，则使用未压缩." +
                "\n未压缩的全画幅写入 SHP 帧未压缩且全画布大小");
            //toolTip1.SetToolTip(this.toolStripMenuItem_Label_NUD_NrWorker.Control, "The number of threads to use for the color conversion of the imagelist. Default is number of processors.");
            toolTip1.SetToolTip(this.comboBox_Compression, "所选图像/帧的压缩方法.\n\"Undefined\" 表示未压缩, 此帧将使用菜单条中的全局设置。");
            this.dataGridView_BitFields.ShowCellToolTips = false;
            toolTip1.SetToolTip(this.dataGridView_BitFields, "该位标志，压缩为第二位。压缩位只能通过上面的组合框进行更改。\n默认情况下，设置了第一个位，因为几乎所有 TS/RA2 SHP 都设置了该位。");

            toolTip1.SetToolTip(this.button_RadarColor, "车架的雷达颜色。仅适用于泰伯利亚/矿石叠加层。\nTS 和 RA2 忽略它。");
            toolTip1.SetToolTip(this.checkBox_RadarColorAverage, "忽略设置的颜色，而是计算此帧的所有彩色像素的平均颜色。");
            toolTip1.SetToolTip(this.checkBox_FrameFiles, "在 Temp 子文件夹中为每个帧创建一个图像文件。");
            toolTip1.SetToolTip(this.textBox_CreateFiles, "框架图像的文件名。后跟一个 5 位数的帧号。\n如果设置了 *，则将使用原始文件名。");
            toolTip1.SetToolTip(this.comboBox_CreateFilesFormat, "单帧图像的格式。");
            toolTip1.SetToolTip(this.checkBox_PreventWobbleBug, "防止炮塔单元在TS中的抖动问题。 勾选后, ImageShaper将确保每帧的 CX/CY 和 OffsetX/OffsetY 值都是均匀的。");

            toolTip1.SetToolTip(this.checkBox_UseCustomBackgroundColor, "启用后，颜色转换将使用指定的背景色作为透明调色板颜色 #0.\n禁用后，颜色转换将图像左上角像素的颜色用于透明颜色 #0.");
            toolTip1.SetToolTip(this.button_CustomBackgroundColor, "用于将颜色转换为透明调色板颜色的固定背景色 #0.");
            toolTip1.SetToolTip(this.checkBox_CombineTransparency, "将此图像与另一个图像组合时，仅复制此图像的透明像素。\n这允许使用此图像提供的透明蒙版删除基本图像的某些部分。\n这仅适用于第二个或第三个...图像列表中的图像！");

            toolTip1.SetToolTip(this.checkBox_OptimizeCanvas, "通过删除所有纯透明边框，将主 SHP 画布减小到尽可能小的尺寸。");
            toolTip1.SetToolTip(this.checkBox_KeepCentered, "在减少主SHP画布的同时，通过均匀地缩小左侧/右侧和顶部/底部的侧面来保持对象居中。\n没有，游戏可以正常工作，但SHP Builder没有显示每个帧的偏移量，因此SHP Builder中的可见中心可能与真实对象的中心点不同。");

            toolTip1.SetToolTip(this.numericUpDown_SplitResult, "将帧拆分为指定数量的文件。#frames div value = 每个 SHP 的帧数。从带有剩余部分的分区中剩余的帧将被跳过！");

            UpdatePreview();

            toolStripMenuItem_Label_NUD_NrWorker.ToolStrip_UC_Label_NUD.Minimum = 1;
            toolStripMenuItem_Label_NUD_NrWorker.ToolStrip_UC_Label_NUD.Maximum = 16;
            if (Environment.ProcessorCount > (this.toolStripMenuItem_Label_NUD_NrWorker.ToolStrip_UC_Label_NUD.Maximum))
                this.toolStripMenuItem_Label_NUD_NrWorker.ToolStrip_UC_Label_NUD.Maximum = Environment.ProcessorCount;
            this.toolStripMenuItem_Label_NUD_NrWorker.ToolStrip_UC_Label_NUD.Value = Environment.ProcessorCount;


            if (ShowPreview) this.showHidePreviewToolStripMenuItem.Text = "隐藏预览";
            else this.showHidePreviewToolStripMenuItem.Text = "显示预览";
            ShowHidePreview();
            this.Activated += new EventHandler(Form_ImageShaper_Activated);
        }

        void uC_Palette1_PaletteChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        void Form_ImageShaper_Activated(object sender, EventArgs e)
        {
            if (form_Preview != null)
                ShowInactiveTopmost(form_Preview);
        }

        #region bitfield
        private void comboBox_Compression_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetBitField(GetBitField(), (SHP_TS_EncodingFormat)this.comboBox_Compression.SelectedIndex);
            if (this.comboBox_Compression.Focused)
            {
                if (this.dataGridView_Files.SelectedCells.Count > 0)
                {
                    SHP_TS_EncodingFormat f = (SHP_TS_EncodingFormat)this.comboBox_Compression.SelectedIndex;
                    SHP_TS_BitFlags b = GetBitField();
                    foreach (DataGridViewCell cell in this.dataGridView_Files.SelectedCells)
                        if (cell.Value != null)
                            ((CImageFile)cell.Value).CompressionFormat = f;
                    this.dataGridView_Files.Invalidate();
                }
            }
        }

        private void SetBitField(SHP_TS_BitFlags bits, SHP_TS_EncodingFormat compression)
        {
            for (byte i = 0; i < this.dataGridView_BitFields.Rows[0].Cells.Count; i++)
            {
                this.dataGridView_BitFields.Rows[0].Cells[i].Value = BitHelper.GetBit((int)bits, i);
                if (i == 1)
                {
                    switch (compression)
                    {
                        case SHP_TS_EncodingFormat.Undefined: this.dataGridView_BitFields.Rows[0].Cells[i].Value = "?"; break;
                        case SHP_TS_EncodingFormat.Uncompressed: this.dataGridView_BitFields.Rows[0].Cells[i].Value = "U"; break;
                        case SHP_TS_EncodingFormat.RLE_Zero: this.dataGridView_BitFields.Rows[0].Cells[i].Value = "R"; break;
                        case SHP_TS_EncodingFormat.Detect_best_size: this.dataGridView_BitFields.Rows[0].Cells[i].Value = "!"; break;
                        case SHP_TS_EncodingFormat.Uncompressed_Full_Frame: this.dataGridView_BitFields.Rows[0].Cells[i].Value = "UF"; break;
                        default: this.dataGridView_BitFields.Rows[0].Cells[i].Value = "?"; break;
                    }
                }
            }
            SHP_TS_BitFlags f = GetBitField();
        }

        private SHP_TS_BitFlags GetBitField()
        {
            int t = 0;
            for (byte i = 0; i < this.dataGridView_BitFields.Rows[0].Cells.Count; i++)
            {
                if ((i != 1) && ((bool)this.dataGridView_BitFields.Rows[0].Cells[i].Value))
                    t += (int)Math.Pow((double)2, (double)i);
            }
            return (SHP_TS_BitFlags)t;
        }

        void dataGridView_BitFields_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridViewCell c = dataGridView_BitFields[e.ColumnIndex, e.RowIndex];
            if (!dataGridView_BitFields.Columns[e.ColumnIndex].ReadOnly)
            {
                if (c.Value == null)
                    c.Value = true;
                else
                    c.Value = !(bool)c.Value;
                dataGridView_BitFields.EndEdit();
            }
            if (this.dataGridView_BitFields.Focused)
            {
                if (this.dataGridView_Files.SelectedCells.Count > 0)
                {
                    SHP_TS_BitFlags b = GetBitField();
                    foreach (DataGridViewCell cell in this.dataGridView_Files.SelectedCells)
                        if (cell.Value != null)
                            ((CImageFile)cell.Value).BitFlags = b;
                    this.dataGridView_Files.Invalidate();
                }
            }
        }

        void dataGridView_BitFields_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            //e.PaintBackground(e.CellBounds, true);
            if (e.ColumnIndex != 1)
                e.Graphics.FillRectangle(Brushes.White, e.CellBounds);
            else
                e.Graphics.FillRectangle(Brushes.Gray, e.CellBounds);
            e.Graphics.DrawRectangle(Pens.Black, new Rectangle(new Point(e.CellBounds.X, e.CellBounds.Y), new Size(e.CellBounds.Width - 1, e.CellBounds.Height - 1)));
            Point center = new Point(e.CellBounds.X + e.CellBounds.Width / 2 - 1, e.CellBounds.Y + e.CellBounds.Height / 2 + 1);
            if (e.ColumnIndex == 1)
            {

                switch (e.Value.ToString())
                {
                    case "!":
                        {
                            for (int i = 0; i < 5; i++)
                                e.Graphics.DrawLines(Pens.Black, new Point[] { 
                                    new Point(center.X + 3+i, center.Y - 6), 
                                    new Point(center.X - 3+i, center.Y), 
                                    new Point(center.X + 3+i, center.Y + 6) });
                            break;
                        }
                    case "R":
                        {
                            //check mark
                            for (int i = 0; i < 5; i++)
                                e.Graphics.DrawLines(Pens.Black, new Point[] { new Point(center.X - 3, center.Y - 3 + i), new Point(center.X, center.Y + i), new Point(center.X + 6, center.Y - 6 + i) });
                            break;
                        }
                    case "?":
                        {
                            Font f = new Font(this.dataGridView_BitFields.Font.FontFamily, 14, FontStyle.Bold);
                            SizeF s = e.Graphics.MeasureString("?", f);
                            e.Graphics.DrawString("?", f, Brushes.Black, new Point(center.X - (int)s.Width / 2, center.Y - 1 - (int)s.Height / 2));
                            break;
                        }
                    case "UF":
                        {
                            e.Graphics.DrawRectangle(Pens.Black, new Rectangle(new Point(e.CellBounds.X + 2, e.CellBounds.Y + 2), new Size(e.CellBounds.Width - 5, e.CellBounds.Height - 5)));
                            e.Graphics.DrawRectangle(Pens.Black, new Rectangle(new Point(e.CellBounds.X + 3, e.CellBounds.Y + 3), new Size(e.CellBounds.Width - 7, e.CellBounds.Height - 7)));
                            break;
                        }
                    default: break;
                }
            }
            else
                if ((bool)e.FormattedValue)
                    for (int i = 0; i < 5; i++)
                        e.Graphics.DrawLines(Pens.Black, new Point[] { new Point(center.X - 3, center.Y - 3 + i), new Point(center.X, center.Y + i), new Point(center.X + 6, center.Y - 6 + i) });
            //ControlPaint.DrawCheckBox(e.Graphics, e.CellBounds.X - 1, e.CellBounds.Y - 1, e.CellBounds.Width + 2, e.CellBounds.Height + 2, (bool)e.FormattedValue ? ButtonState.Checked | ButtonState.Flat : ButtonState.Normal | ButtonState.Flat);
            e.Handled = true;
        }
        #endregion

        #region instantly applied changes to selected cells
        private void checkBox_RadarColorAverage_CheckedChanged(object sender, EventArgs e)
        {
            this.button_RadarColor.Enabled = !this.checkBox_RadarColorAverage.Checked;
            if (this.checkBox_RadarColorAverage.Focused)
            {
                if (this.dataGridView_Files.SelectedCells.Count > 0)
                {
                    foreach (DataGridViewCell cell in this.dataGridView_Files.SelectedCells)
                        if (cell.Value != null)
                            ((CImageFile)cell.Value).RadarColorAverage = this.checkBox_RadarColorAverage.Checked;
                }
            }
        }

        private void button_RadarColor_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            cd.Color = (Color)this.button_RadarColor.Tag;
            cd.AllowFullOpen = true;
            cd.AnyColor = true;
            cd.FullOpen = true;
            if (cd.ShowDialog() == DialogResult.OK)
            {
                this.button_RadarColor.Tag = cd.Color;
                this.button_RadarColor.Text = Cinimanager.ColorToStr(cd.Color, true);

                if (this.dataGridView_Files.SelectedCells.Count > 0)
                {
                    foreach (DataGridViewCell cell in this.dataGridView_Files.SelectedCells)
                        if (cell.Value != null)
                            ((CImageFile)cell.Value).RadarColor = cd.Color;
                }
            }
        }

        private void checkBox_OptimizeCanvas_CheckedChanged(object sender, EventArgs e)
        {
            this.checkBox_KeepCentered.Enabled = this.checkBox_OptimizeCanvas.Checked;
            if (this.checkBox_OptimizeCanvas.Focused)
                this.checkBox_KeepCentered.Checked = this.checkBox_OptimizeCanvas.Checked;
        }

        private void button_CustomBackgroundColor_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            cd.Color = (Color)this.button_CustomBackgroundColor.Tag;
            cd.AllowFullOpen = true;
            cd.AnyColor = true;
            cd.FullOpen = true;
            if (cd.ShowDialog() == DialogResult.OK)
            {
                this.button_CustomBackgroundColor.Text = Cinimanager.ColorToStr(cd.Color, true);
                this.button_CustomBackgroundColor.Tag = cd.Color;

                if (this.dataGridView_Files.SelectedCells.Count > 0)
                {
                    foreach (DataGridViewCell cell in this.dataGridView_Files.SelectedCells)
                        if (cell.Value != null)
                            ((CImageFile)cell.Value).CustomBackgroundColor = cd.Color;
                }
            }
        }

        private void checkBox_UseCustomBackgroundColor_CheckedChanged(object sender, EventArgs e)
        {
            this.button_CustomBackgroundColor.Enabled = this.checkBox_UseCustomBackgroundColor.Checked;
            if (this.checkBox_UseCustomBackgroundColor.Focused)
            {
                if (this.dataGridView_Files.SelectedCells.Count > 0)
                {
                    foreach (DataGridViewCell cell in this.dataGridView_Files.SelectedCells)
                        if (cell.Value != null)
                            ((CImageFile)cell.Value).UseCustomBackgroundColor = this.checkBox_UseCustomBackgroundColor.Checked;
                }
            }
        }

        private void checkBox_CombineTransparency_CheckedChanged(object sender, EventArgs e)
        {
            if (this.checkBox_CombineTransparency.Focused)
            {
                if (this.dataGridView_Files.SelectedCells.Count > 0)
                {
                    foreach (DataGridViewCell cell in this.dataGridView_Files.SelectedCells)
                        if (cell.Value != null)
                            ((CImageFile)cell.Value).CombineTransparentPixel = this.checkBox_CombineTransparency.Checked;
                }
            }
        }

        #endregion


        bool ShowPreview = false;
        Form_DockPreview form_Preview;
        private void showHidePreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowPreview = !ShowPreview;
            ShowHidePreview();
        }

        private void ShowHidePreview()
        {
            if (ShowPreview)
            {
                this.showHidePreviewToolStripMenuItem.Text = "隐藏预览";
                form_Preview = new Form_DockPreview();
                form_Preview.StartPosition = FormStartPosition.Manual;
                form_Preview.Location = new Point(this.Location.X + this.Width, this.Location.Y);
                form_Preview.uC_ImageCanvas1.PixelColorChanged += new EventHandler<UC_ImageCanvas.ImageCanvasDataEventArgs>(uC_ImageCanvas1_PixelColorChanged);

                UpdatePreview();
                form_Preview.Show();
            }
            else
            {
                this.showHidePreviewToolStripMenuItem.Text = "显示预览";
                if (form_Preview != null)
                {
                    form_Preview.Close();
                    form_Preview.uC_ImageCanvas1.PixelColorChanged -= uC_ImageCanvas1_PixelColorChanged;
                    form_Preview = null;
                }
            }
        }

        void uC_ImageCanvas1_PixelColorChanged(object sender, UC_ImageCanvas.ImageCanvasDataEventArgs e)
        {
            this.uC_Palette1.PaletteSelectedColor = e.Color;
        }

        #region datagrid control
        void dataGridView1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                foreach (DataGridViewCell cell in this.dataGridView_Files.SelectedCells)
                {
                    if ((cell.ColumnIndex < this.dataGridView_Files.ColumnCount) && (cell.ColumnIndex >= 0) &&
                        (cell.RowIndex < this.dataGridView_Files.RowCount) && (cell.RowIndex >= 0))
                    {
                        cell.Value = null;
                        if (IsEmptyRow(this.dataGridView_Files.Rows[cell.RowIndex])) this.dataGridView_Files.Rows.RemoveAt(cell.RowIndex);
                    }
                }
                AddLastEmptyRow();
            }
            if ((e.Control) && (e.KeyCode == Keys.V))
            {
                LoadFromClipboard();
            }
            UpdatePreview();
        }

        private bool IsEmptyRow(DataGridViewRow row)
        {
            if (row == null) return true;
            for (int i = 0; i < row.Cells.Count; i++)
                if (row.Cells[i].Value != null) return false;
            return true;
        }

        void dataGridView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        void dataGridView1_DragDrop(object sender, DragEventArgs e)
        {
            Point clientPoint = dataGridView_Files.PointToClient(new Point(e.X, e.Y));
            int rowindex = this.dataGridView_Files.HitTest(clientPoint.X, clientPoint.Y).RowIndex;
            int columnindex = this.dataGridView_Files.HitTest(clientPoint.X, this.dataGridView_Files.ColumnHeadersHeight + 5).ColumnIndex;

            if (columnindex == -1) columnindex = 0;
            if (columnindex >= this.dataGridView_Files.ColumnCount) return;

            string[] formats = e.Data.GetFormats();
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
                AddFilesToDataGrid(files, columnindex, rowindex);
            else
            {
                RemoveLastEmptyRow();
                //d&d selected cells
                //only when target cell is empty, the value is pasted, otherwise a new row inserted/added
                List<DGVCell> dgvscc = e.Data.GetData(typeof(List<DGVCell>)) as List<DGVCell>;

                Point cursorLocation = this.PointToClient(new Point(e.X, e.Y));

                int rowdelta = int.MaxValue;
                int coldelta = int.MaxValue;
                for (int i = 0; i < dgvscc.Count; i++)
                {
                    if (dgvscc[i].RowIndex < rowdelta) rowdelta = dgvscc[i].RowIndex;
                    if (dgvscc[i].ColumnIndex < coldelta) coldelta = dgvscc[i].ColumnIndex;
                }

                if (rowindex == -1) rowindex = this.dataGridView_Files.RowCount;
                for (int i = 0; i < dgvscc.Count; i++)
                {
                    int cellcolindex = columnindex + (dgvscc[i].ColumnIndex - coldelta);
                    int cellrowindex = rowindex + (dgvscc[i].RowIndex - rowdelta);

                    if (cellcolindex >= this.dataGridView_Files.ColumnCount) continue;

                    if ((cellrowindex < this.dataGridView_Files.RowCount) && (this.dataGridView_Files[cellcolindex, cellrowindex].Value == null))
                        this.dataGridView_Files[cellcolindex, cellrowindex].Value = dgvscc[i].Value;
                    else
                    {
                        DataGridViewRow row = (DataGridViewRow)this.dataGridView_Files.RowTemplate.Clone();
                        object[] values = new object[this.dataGridView_Files.ColumnCount];
                        values[cellcolindex] = dgvscc[i].Value;
                        row.CreateCells(this.dataGridView_Files, values);
                        if (cellrowindex < this.dataGridView_Files.RowCount)
                            this.dataGridView_Files.Rows.Insert(cellrowindex, row);
                        else
                            this.dataGridView_Files.Rows.Add(row);
                    }

                }
                AddLastEmptyRow();
            }
        }

        void dataGridView_Files_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        private Rectangle dragBoxFromMouseDown;
        //necessary, since the DataGridViewCell changes its properties as soon as something is changed in the DGV
        //e.g. the first pasted cell will change the rowindex of all other selected cells and thus mess up the pasting operation
        internal struct DGVCell
        {
            public DGVCell(DataGridViewCell cell)
            {
                this.Value = cell.Value;
                this.ColumnIndex = cell.ColumnIndex;
                this.RowIndex = cell.RowIndex;
            }
            public int ColumnIndex;
            public int RowIndex;
            public object Value;
        }
        private void dataGridView_Files_MouseDown(object sender, MouseEventArgs e)
        {
            // Get the index of the item the mouse is below.
            int rowindex = this.dataGridView_Files.HitTest(e.X, e.Y).RowIndex;
            int colindex = this.dataGridView_Files.HitTest(e.X, e.Y).ColumnIndex;

            if (rowindex != -1)
            {
                //only by moving outside the cell, the d&d operation starts
                //the default SystemInformation.DragSize is way too tiny and errorprone. just 4 pixel sucks for fast working people.
                dragBoxFromMouseDown = this.dataGridView_Files.GetCellDisplayRectangle(colindex, rowindex, true);
            }
            else
                // Reset the rectangle if the mouse is not over an item in the ListBox.
                dragBoxFromMouseDown = Rectangle.Empty;
        }
        void dataGridView_Files_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (dragBoxFromMouseDown != Rectangle.Empty && !dragBoxFromMouseDown.Contains(e.X, e.Y))
                {
                    List<DGVCell> cells = new List<DGVCell>();
                    for (int c = this.dataGridView_Files.ColumnCount - 1; c >= 0; c--)
                        for (int i = 0; i < this.dataGridView_Files.SelectedCells.Count; i++)
                        {
                            DGVCell cell = new DGVCell(this.dataGridView_Files.SelectedCells[i]);
                            if ((cell.Value != null) && (cell.ColumnIndex == c))
                                cells.Insert(0, cell);
                        }
                    this.dataGridView_Files.DoDragDrop(cells, DragDropEffects.Copy);
                }
            }
        }

        List<DGVCell> Cells2Copy;
        void DataGridCell_Copy(object sender, EventArgs e)
        {
            Cells2Copy = new List<DGVCell>();
            for (int c = this.dataGridView_Files.ColumnCount - 1; c >= 0; c--)
                for (int i = 0; i < this.dataGridView_Files.SelectedCells.Count; i++)
                {
                    DGVCell cell = new DGVCell(this.dataGridView_Files.SelectedCells[i]);
                    if ((cell.Value != null) && (cell.ColumnIndex == c))
                        Cells2Copy.Insert(0, cell);
                }
        }

        void DataGridCell_Cut(object sender, EventArgs e)
        {
            Cells2Copy = new List<DGVCell>();
            for (int c = this.dataGridView_Files.ColumnCount - 1; c >= 0; c--)
                for (int i = 0; i < this.dataGridView_Files.SelectedCells.Count; i++)
                {
                    DGVCell cell = new DGVCell(this.dataGridView_Files.SelectedCells[i]);
                    if ((cell.Value != null) && (cell.ColumnIndex == c))
                        Cells2Copy.Insert(0, cell);
                }

            foreach (DataGridViewCell cell in this.dataGridView_Files.SelectedCells)
                cell.Value = null;
        }

        void DataGridCell_Paste(object sender, EventArgs e)
        {
            if ((targetCell.X >= 0) && (targetCell.Y >= 0))
            {
                int rowindex = targetCell.Y;
                int columnindex = targetCell.X;

                int rowdelta = int.MaxValue;
                int coldelta = int.MaxValue;
                for (int i = 0; i < Cells2Copy.Count; i++)
                {
                    if (Cells2Copy[i].RowIndex < rowdelta) rowdelta = Cells2Copy[i].RowIndex;
                    if (Cells2Copy[i].ColumnIndex < coldelta) coldelta = Cells2Copy[i].ColumnIndex;
                }

                if (rowindex == -1) rowindex = this.dataGridView_Files.RowCount;
                for (int i = 0; i < Cells2Copy.Count; i++)
                {
                    int cellcolindex = columnindex + (Cells2Copy[i].ColumnIndex - coldelta);
                    int cellrowindex = rowindex + (Cells2Copy[i].RowIndex - rowdelta);

                    if (cellcolindex >= this.dataGridView_Files.ColumnCount) continue;

                    if ((cellrowindex < this.dataGridView_Files.RowCount) && (this.dataGridView_Files[cellcolindex, cellrowindex].Value == null))
                        this.dataGridView_Files[cellcolindex, cellrowindex].Value = Cells2Copy[i].Value;
                    else
                    {
                        DataGridViewRow row = (DataGridViewRow)this.dataGridView_Files.RowTemplate.Clone();
                        object[] values = new object[this.dataGridView_Files.ColumnCount];
                        values[cellcolindex] = Cells2Copy[i].Value;
                        row.CreateCells(this.dataGridView_Files, values);
                        if (cellrowindex < this.dataGridView_Files.RowCount)
                            this.dataGridView_Files.Rows.Insert(cellrowindex, row);
                        else
                            this.dataGridView_Files.Rows.Add(row);
                    }

                }
                AddLastEmptyRow();
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
        private const int WM_SETREDRAW = 11;
        BackgroundWorker LoadFilesBW;
        private void AddFilesToDataGrid(string[] filenames, int columnindex, int rowindex)
        {
            if ((LoadFilesBW != null) && (LoadFilesBW.IsBusy))
            {
                if (MessageBox.Show("文件正在加载中！\n是否要中止该过程，而是添加新文件？", "正在加载文件", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    LoadFilesBW.CancelAsync();
                else
                    return;
            }

            if (this.dataGridView_Files.SelectedCells.Count > 1)
            {
                int SelectedCellsInOneColumn = 0;
                int columnnr = -1;
                int toprowindex = int.MaxValue;
                foreach (DataGridViewCell c in this.dataGridView_Files.SelectedCells)
                {
                    SelectedCellsInOneColumn++;
                    if (c.RowIndex < toprowindex) toprowindex = c.RowIndex;
                    if (columnnr == -1)
                        columnnr = c.ColumnIndex;
                    else
                        if (columnnr != c.ColumnIndex)
                        {
                            SelectedCellsInOneColumn = -1;
                            break;
                        }
                }
                //don't do anything if the user selected cells across multiple columns
                //this works for cells in a single column only!
                if ((SelectedCellsInOneColumn > 0) && (SelectedCellsInOneColumn > filenames.Length))
                {
                    if (MessageBox.Show("是否要复制 " + filenames.Length.ToString() + " 要填充的文件 " + SelectedCellsInOneColumn.ToString() + " 选定的单元格？", "重复的文件？", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        string[] dupfilenames = new string[SelectedCellsInOneColumn];
                        for (int i = 0; i < dupfilenames.Length; i++)
                            dupfilenames[i] = filenames[i % filenames.Length];
                        filenames = dupfilenames;
                        rowindex = toprowindex;
                    }
                }
            }


            this.richTextBox_Reports.Clear();
            int palindex = -1;
            palindex = PaletteManager.GetPaletteIndex(this.uC_Palette1.Palette, false);

            this.progressBar1.Minimum = 0;
            this.progressBar1.Maximum = filenames.Length;

            this.richTextBox_Reports.SelectedText = "添加 " + filenames.Length + " 文件中...";


            SHP_TS_EncodingFormat format = (SHP_TS_EncodingFormat)this.comboBox_Compression.SelectedIndex;
            SHP_TS_BitFlags bitflags = GetBitField();
            bool checkBox_RadarColorAverage = this.checkBox_RadarColorAverage.Checked;
            bool checkBox_UseCustomBackgroundColor = this.checkBox_UseCustomBackgroundColor.Checked;
            Color button_CustomBackgroundColor = (Color)this.button_CustomBackgroundColor.Tag;
            bool checkBox_CombineTransparency = this.checkBox_CombineTransparency.Checked;

            LoadFilesBW = new BackgroundWorker();
            LoadFilesBW.DoWork += (w_s, w_e) =>
            {
                BackgroundWorker worker = (BackgroundWorker)w_s;
                CFiles2Load job = w_e.Argument as CFiles2Load;
                DataGridView tmpdgv = job.dgv;
                w_e.Result = "Loading-Files-Worker stopped unfinished";
                int SHPFrameCount = 0;

                for (int f = 0; f < job.files.Length; f++)
                {
                    //System.Threading.Thread.Sleep(1000);
                    string file = job.files[f];
                    if (!worker.CancellationPending)
                        worker.ReportProgress(f, "");
                    else
                    {
                        w_e.Cancel = true;
                        return;
                    }

                    bool IsSHP = false;
                    //ignore unsupported files by testing each file if we can use it as Bitmap
                    try
                    {
                        Bitmap test = (Bitmap)Image.FromFile(file);
                    }
                    catch
                    {
                        IsSHP = CSHaPer.IsSHP(file);
                        //if its no image and no SHP, skip this
                        if (!IsSHP)
                            continue;
                    }

                    if (IsSHP)
                    {
                        CImageFile[] SHPFrames = CSHaPer.GetFrames(file, palindex);
                        DataGridViewRow row;
                        for (int shp_i = 0; shp_i < SHPFrames.Length; shp_i++)
                        {
                            CImageFile SHPFrame = SHPFrames[shp_i];
                            SHPFrame.RadarColorAverage = checkBox_RadarColorAverage;
                            if ((rowindex == -1) || ((rowindex >= 0) && (rowindex + (f + SHPFrameCount) + shp_i >= tmpdgv.Rows.Count)))
                            {
                                row = (DataGridViewRow)tmpdgv.RowTemplate.Clone();
                                object[] values = new object[3];
                                for (int i = 0; i < values.Length; i++)
                                    if (i == columnindex) values[i] = SHPFrame;
                                    else values[i] = null;

                                row.CreateCells(tmpdgv, values);
                                tmpdgv.Rows.Add(row);
                            }
                            else
                            {
                                row = tmpdgv.Rows[rowindex + (f + SHPFrameCount) + shp_i];
                                row.Cells[columnindex].Value = SHPFrame;
                            }
                        }
                        SHPFrameCount += SHPFrames.Length - 1;//f for the files counter already is 1 for the SHP itself. e.g. for an SHP with only 1 frame, SHPFrameCount doesn't need to be raised
                    }
                    else
                    {
                        DataGridViewRow row;
                        CImageFile cif = new CImageFile(file, palindex, format);
                        cif.UseCustomBackgroundColor = checkBox_UseCustomBackgroundColor;
                        cif.CustomBackgroundColor = button_CustomBackgroundColor;
                        cif.RadarColorAverage = checkBox_RadarColorAverage;
                        cif.CombineTransparentPixel = checkBox_CombineTransparency;
                        cif.BitFlags = bitflags;
                        if ((rowindex == -1) || ((rowindex >= 0) && (rowindex + (f + SHPFrameCount) >= tmpdgv.Rows.Count)))
                        {
                            row = (DataGridViewRow)tmpdgv.RowTemplate.Clone();
                            object[] values = new object[3];
                            for (int i = 0; i < values.Length; i++)
                                if (i == columnindex) values[i] = cif;
                                else values[i] = null;

                            row.CreateCells(tmpdgv, values);
                            tmpdgv.Rows.Add(row);
                        }
                        else
                        {
                            row = tmpdgv.Rows[rowindex + (f + SHPFrameCount)];
                            row.Cells[columnindex].Value = cif;
                        }
                    }
                }

                w_e.Result = tmpdgv;
            };

            LoadFilesBW.ProgressChanged += (w_s, w_e) =>
            {
                if (w_e.ProgressPercentage != -1)
                {
                    this.progressBar1.Value = w_e.ProgressPercentage;
                    if ((w_e.UserState != null) && (w_e.UserState.ToString() != ""))
                        this.richTextBox_Reports.SelectedText = w_e.UserState.ToString();
                }
            };

            //throw new Exception("this.dataGridView_Files.Rows.Clear(); is shit. add only changes so the selected cells and the current scrollbar location are kept");
            LoadFilesBW.RunWorkerCompleted += (w_s, w_e) =>
            {
                if ((!w_e.Cancelled) && (w_e.Error == null))
                {
                    SendMessage(this.dataGridView_Files.Handle, WM_SETREDRAW, false, 0);
                    //this.dataGridView_Files.Rows.Clear();
                    DataGridView wdgv = (DataGridView)w_e.Result;
                    DataGridViewRow row = new DataGridViewRow();
                    for (int i = 0; i < wdgv.Rows.Count; i++)
                    {
                        if (i < this.dataGridView_Files.Rows.Count)
                        {
                            for (int c = 0; c < wdgv.Rows[i].Cells.Count; c++)
                                this.dataGridView_Files.Rows[i].Cells[c].Value = wdgv.Rows[i].Cells[c].Value;
                        }
                        else
                        {
                            row = (System.Windows.Forms.DataGridViewRow)wdgv.Rows[i].Clone();
                            for (int c = 0; c < wdgv.Rows[i].Cells.Count; c++)
                                row.Cells[c].Value = wdgv.Rows[i].Cells[c].Value;

                            this.dataGridView_Files.Rows.Add(row);
                        }
                    }
                    AddLastEmptyRow();

                    SendMessage(this.dataGridView_Files.Handle, WM_SETREDRAW, true, 0);
                    this.dataGridView_Files.Refresh();

                    this.progressBar1.Value = 0;
                    this.richTextBox_Reports.SelectedText = " 完毕." + Environment.NewLine;
                }
                else
                {
                    if (w_e.Error != null)
                    {
                        this.richTextBox_Reports.SelectionColor = Color.Red;
                        this.richTextBox_Reports.SelectedText = "预览错误:" + w_e.Error.Message + Environment.NewLine;
                    }
                }
            };

            LoadFilesBW.WorkerReportsProgress = true;
            LoadFilesBW.WorkerSupportsCancellation = true;

            RemoveLastEmptyRow();
            LoadFilesBW.RunWorkerAsync(new CFiles2Load(filenames, this.dataGridView_Files));
        }


        /// <summary>
        /// synchronous loading for command line file load
        /// </summary>
        private void AddFilesToDataGridSync(string[] filenames, int columnindex, int rowindex, bool setSHPBits, bool setSHPCompression)
        {
            int palindex = -1;
            palindex = PaletteManager.GetPaletteIndex(this.uC_Palette1.Palette, false);

            this.progressBar1.Minimum = 0;
            this.progressBar1.Maximum = filenames.Length;

            Console.Write("添加 " + filenames.Length + " 文件中...");


            SHP_TS_EncodingFormat compressionformat = (SHP_TS_EncodingFormat)this.comboBox_Compression.SelectedIndex;
            SHP_TS_BitFlags bitflags = GetBitField();
            bool checkBox_RadarColorAverage = this.checkBox_RadarColorAverage.Checked;
            bool checkBox_UseCustomBackgroundColor = this.checkBox_UseCustomBackgroundColor.Checked;
            Color button_CustomBackgroundColor = (Color)this.button_CustomBackgroundColor.Tag;
            bool checkBox_CombineTransparency = this.checkBox_CombineTransparency.Checked;


            RemoveLastEmptyRow();
            int SHPFrameCount = 0;

            SendMessage(this.dataGridView_Files.Handle, WM_SETREDRAW, false, 0);

            for (int f = 0; f < filenames.Length; f++)
            {
                string file = filenames[f];

                bool IsSHP = false;
                //ignore unsupported files by testing each file if we can use it as Bitmap
                try
                {
                    Bitmap test = (Bitmap)Image.FromFile(file);
                }
                catch
                {
                    IsSHP = CSHaPer.IsSHP(file);
                    //if its no image and no SHP, skip this
                    if (!IsSHP)
                        continue;
                }

                if (IsSHP)
                {
                    CImageFile[] SHPFrames = CSHaPer.GetFrames(file, palindex);
                    DataGridViewRow row;
                    for (int shp_i = 0; shp_i < SHPFrames.Length; shp_i++)
                    {
                        CImageFile SHPFrame = SHPFrames[shp_i];
                        if (setSHPBits) SHPFrame.BitFlags = bitflags;
                        if (setSHPCompression) SHPFrame.CompressionFormat = compressionformat;
                        SHPFrame.RadarColorAverage = checkBox_RadarColorAverage;
                        if ((rowindex == -1) || ((rowindex >= 0) && (rowindex + (f + SHPFrameCount) + shp_i >= this.dataGridView_Files.Rows.Count)))
                        {
                            row = (DataGridViewRow)this.dataGridView_Files.RowTemplate.Clone();
                            object[] values = new object[3];
                            for (int i = 0; i < values.Length; i++)
                                if (i == columnindex) values[i] = SHPFrame;
                                else values[i] = null;

                            row.CreateCells(this.dataGridView_Files, values);
                            this.dataGridView_Files.Rows.Add(row);
                        }
                        else
                        {
                            row = this.dataGridView_Files.Rows[rowindex + (f + SHPFrameCount) + shp_i];
                            row.Cells[columnindex].Value = SHPFrame;
                        }
                    }
                    SHPFrameCount += SHPFrames.Length - 1;//f for the files counter already is 1 for the SHP itself. e.g. for an SHP with only 1 frame, SHPFrameCount doesn't need to be raised
                }
                else
                {
                    DataGridViewRow row;
                    CImageFile cif = new CImageFile(file, palindex, compressionformat);
                    cif.UseCustomBackgroundColor = checkBox_UseCustomBackgroundColor;
                    cif.CustomBackgroundColor = button_CustomBackgroundColor;
                    cif.RadarColorAverage = checkBox_RadarColorAverage;
                    cif.CombineTransparentPixel = checkBox_CombineTransparency;
                    cif.BitFlags = bitflags;
                    if ((rowindex == -1) || ((rowindex >= 0) && (rowindex + (f + SHPFrameCount) >= this.dataGridView_Files.Rows.Count)))
                    {
                        row = (DataGridViewRow)this.dataGridView_Files.RowTemplate.Clone();
                        object[] values = new object[3];
                        for (int i = 0; i < values.Length; i++)
                            if (i == columnindex) values[i] = cif;
                            else values[i] = null;

                        row.CreateCells(this.dataGridView_Files, values);
                        this.dataGridView_Files.Rows.Add(row);
                    }
                    else
                    {
                        row = this.dataGridView_Files.Rows[rowindex + (f + SHPFrameCount)];
                        row.Cells[columnindex].Value = cif;
                    }
                }
            }

            AddLastEmptyRow();

            SendMessage(this.dataGridView_Files.Handle, WM_SETREDRAW, true, 0);
            this.dataGridView_Files.Refresh();

            this.progressBar1.Value = 0;
            Console.WriteLine(" 完毕.");
        }

        private Point targetCell;
        void dataGridView1_MouseClick(object sender, MouseEventArgs e)
        {
            targetCell = new Point(-1, -1);

            if (e.Button == MouseButtons.Right)
            {
                //Point clientPoint = dataGridView1.PointToClient(new Point(e.X, e.Y));
                int rowindex = this.dataGridView_Files.HitTest(e.X, e.Y).RowIndex;
                int columnindex = this.dataGridView_Files.HitTest(e.X, e.Y).ColumnIndex;
                targetCell = new Point(columnindex, rowindex);
                dataGrid_CM.Show(dataGridView_Files, new Point(e.X, e.Y));
            }

            if (e.Button == MouseButtons.Left)
                UpdatePreview();
        }

        void DataGridCell_LoadImages(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "加载图像";
            ofd.InitialDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            ofd.FileName = "";
            ofd.Filter = "图像文件|*.png;*.bmp;*.shp";
            ofd.Multiselect = true;
            if (ofd.ShowDialog() != DialogResult.Cancel)
            {
                int rowindex = targetCell.Y;
                int columnindex = targetCell.X;
                if (columnindex == -1) columnindex = 0;
                AddFilesToDataGrid(ofd.FileNames, columnindex, rowindex);
            }
        }

        void DataGridCell_LoadSplitImage(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "加载图像";
            ofd.InitialDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            ofd.FileName = "";
            ofd.Filter = "图像文件|*.png;*.bmp";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() != DialogResult.Cancel)
            {
                int rowindex = targetCell.Y;
                int columnindex = targetCell.X;
                if (columnindex == -1) columnindex = 0;

                Form_LoadAndSplitImage lasi = new Form_LoadAndSplitImage(ofd.FileName, GetProgramTempPath);
                lasi.Icon = this.Icon;
                lasi.StartPosition = FormStartPosition.Manual;
                lasi.Location = this.PointToScreen(this.customMenuStrip1.Location);
                if (lasi.ShowDialog() == DialogResult.OK)
                    AddFilesToDataGrid(lasi.images, columnindex, rowindex);
            }
        }

        void DataGridCell_ReverseOrder(object sender, EventArgs e)
        {
            List<DGVCell> Cells2Reverse = new List<DGVCell>();
            for (int c = this.dataGridView_Files.ColumnCount - 1; c >= 0; c--)
                for (int i = 0; i < this.dataGridView_Files.SelectedCells.Count; i++)
                {
                    DGVCell cell = new DGVCell(this.dataGridView_Files.SelectedCells[i]);
                    if ((cell.Value != null) && (cell.ColumnIndex == c))
                        Cells2Reverse.Insert(0, cell);
                }

            for (int i = 0; i < Cells2Reverse.Count; i++)
            {
                int ii = Cells2Reverse.Count - 1 - i;
                this.dataGridView_Files[Cells2Reverse[ii].ColumnIndex, Cells2Reverse[ii].RowIndex].Value = Cells2Reverse[i].Value;
            }
        }

        void DataGridCell_SetPalette(object sender, EventArgs e)
        {
            if (this.dataGridView_Files.SelectedCells.Count > 0)
            {
                int palindex = -1;
                palindex = PaletteManager.GetPaletteIndex(this.uC_Palette1.Palette, false);


                foreach (DataGridViewCell cell in this.dataGridView_Files.SelectedCells)
                    if (cell.Value != null)
                        ((CImageFile)cell.Value).PaletteIndex = palindex;
                this.dataGridView_Files.Invalidate();
                UpdatePreview();
            }
        }
        void DataGridCell_SetCompression(object sender, EventArgs e)
        {
            if (this.dataGridView_Files.SelectedCells.Count > 0)
            {
                SHP_TS_EncodingFormat f = (SHP_TS_EncodingFormat)this.comboBox_Compression.SelectedIndex;
                SHP_TS_BitFlags b = GetBitField();
                foreach (DataGridViewCell cell in this.dataGridView_Files.SelectedCells)
                    if (cell.Value != null)
                    {
                        ((CImageFile)cell.Value).CompressionFormat = f;
                        ((CImageFile)cell.Value).BitFlags = b;
                    }
                this.dataGridView_Files.Invalidate();
            }
        }
        void DataGridCell_LoadFromClipboard(object sender, EventArgs e)
        {
            LoadFromClipboard();
        }
        private void LoadFromClipboard()
        {
            try
            {
                Image img = Clipboard.GetImage();
                if (img == null) return;
                string clipboardfilename = "tmpclipb" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
                string filename = System.IO.Path.Combine(GetProgramTempPath, clipboardfilename);
                img.Save(filename);

                int rowindex = targetCell.Y;
                int columnindex = targetCell.X;
                if (columnindex == -1) columnindex = 0;
                AddFilesToDataGrid(new string[] { filename }, columnindex, rowindex);
            }
            catch (Exception ex)
            {
                this.richTextBox_Reports.SelectionColor = Color.Red;
                this.richTextBox_Reports.SelectedText = ex.Message;
            }
        }

        #endregion

        private void button_Start_Click(object sender, EventArgs e)
        {
            CreatSHP(false);
        }

        private void CreatSHP(bool CloseWhenFinished)
        {
            bool PreventWobbleBug = this.checkBox_PreventWobbleBug.Checked;
            bool optimizeCanvas = this.checkBox_OptimizeCanvas.Checked;
            bool keepCentered = this.checkBox_KeepCentered.Checked;
            bool CreateFrameFiles = this.checkBox_FrameFiles.Checked;
            int max_Worker = (int)this.toolStripMenuItem_Label_NUD_NrWorker.ToolStrip_UC_Label_NUD.Value;
            SHP_TS_EncodingFormat DefaultCompression = GetDefaultCompression;
            string tmpfilename = this.textBox_CreateFiles.Text;
            string tmpfileformat = this.comboBox_CreateFilesFormat.SelectedItem.ToString();

            int SplitResultCount = (int)this.numericUpDown_SplitResult.Value;

            this.richTextBox_Reports.Clear();
            this.richTextBox_Reports.Focus();

            Stopwatch duration = Stopwatch.StartNew();

            string outputfolder = GetProgramPath; // Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string ouputfolder_Temp = GetProgramTempPath; // Path.Combine(outputfolder, "Temp");
            if (this.toolStripMenuItem_Outputfolder.ToolStrip_UC_FolderSelector.Value != "")
                outputfolder = this.toolStripMenuItem_Outputfolder.ToolStrip_UC_FolderSelector.Value;

            try
            {
                if (!Directory.Exists(ouputfolder_Temp))
                {
                    Directory.CreateDirectory(ouputfolder_Temp);
                }

                //no longer necessary to delete the files, since they aren't used anymore for conversion
                //DirectoryInfo di = new DirectoryInfo(targetpath);

                //string extension = "";
                //switch (tmpfileformat.ToLower())
                //{
                //    case "png": extension = ".png"; break;
                //    case "bmp": extension = ".bmp"; break;
                //    case "gif": extension = ".gif"; break;
                //    case "tiff": extension = ".tiff"; break;
                //    case "shp(ts)": extension = ".shp"; break;
                //    default: extension = ".shp"; break;
                //}

                //foreach (FileInfo file in di.GetFiles("*" + extension))
                //    file.Delete();
            }
            catch (Exception ex)
            {
                this.richTextBox_Reports.SelectionColor = Color.Red;
                this.richTextBox_Reports.SelectedText = ex.Message;
                return;
            }


            string SHPFilename = "result";
            string SHPFileExtension = ".shp"; //default is .shp, but when a .tem file in SHP format is loaded, keep that extension instead
            bool DoTrim = true;
            for (int r = 0; r < this.dataGridView_Files.Rows.Count; r++)
            {
                if (this.dataGridView_Files.Rows[r].Cells[0].Value != null)
                {
                    SHPFilename = ((CImageFile)this.dataGridView_Files.Rows[r].Cells[0].Value).FileName;
                    if (((CImageFile)this.dataGridView_Files.Rows[r].Cells[0].Value).IsSHP)
                    {
                        DoTrim = false;
                        //remember for SHPs the extension, because they can be also called .tem etc
                        SHPFileExtension = Path.GetExtension(SHPFilename);
                    }
                }
                if (SHPFilename != "") break;
            }
            SHPFilename = Path.GetFileNameWithoutExtension(SHPFilename);
            //trim trailing frame numbers only if the first image is not an SHP
            //for SHPs keep the filename exactly the same
            if (DoTrim)
            {
                //first remove the trailing frame numbers
                SHPFilename = SHPFilename.TrimEnd(new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' });
                //then remove trailing spaces or underlines (this way allowing "myfile01_00001.png" to result in the SHP name "myfile01.shp" with a trailing number)
                SHPFilename = SHPFilename.TrimEnd(new char[] { ' ', '_' });
            }

            //collect the interface data into a package that can be send to the worker thread
            List<CImageJob> jobs = new List<CImageJob>();
            int frameNr = 0;
            for (int r = 0; r < this.dataGridView_Files.Rows.Count; r++)
            {

                List<CImageFile> files = new List<CImageFile>();
                for (int i = 0; i < this.dataGridView_Files.Rows[r].Cells.Count; i++)
                    if (this.dataGridView_Files.Rows[r].Cells[i].Value != null)
                    {
                        CImageFile cell = (CImageFile)this.dataGridView_Files.Rows[r].Cells[i].Value;
                        files.Add(cell);
                        if (cell.PaletteIndex == -1)
                        {
                            this.richTextBox_Reports.SelectionColor = Color.Red;
                            this.richTextBox_Reports.SelectedText = "图像行：[" + r.ToString() + "] 列：[" + i.ToString() + "] 没有分配调色板！";
                            return;
                        }
                    }
                if (files.Count > 0)
                {
                    jobs.Add(new CImageJob(ouputfolder_Temp, frameNr, tmpfilename, tmpfileformat, files, CreateFrameFiles, DefaultCompression));
                    frameNr++;
                }
            }
            if (jobs.Count == 0)
            {
                if (CloseWhenFinished)
                    this.Close();
                return;
            }
            if (jobs.Count > ushort.MaxValue)
            {
                this.richTextBox_Reports.SelectionColor = Color.Red;
                this.richTextBox_Reports.SelectedText = jobs.Count.ToString() + " 检测到帧！SHP 格式仅支持最大值 " + ushort.MaxValue.ToString() + " 框架";
                return;
            }

            this.button_Start.Enabled = false;
            this.progressBar1.Minimum = 0;
            this.progressBar1.Maximum = this.dataGridView_Files.Rows.Count;

            if (!CreateFrameFiles) this.richTextBox_Reports.SelectedText = "创建框架" + Environment.NewLine;
            else this.richTextBox_Reports.SelectedText = "在文件夹中创建调色板索引文件:" + Environment.NewLine + ouputfolder_Temp + Environment.NewLine;

            //create the worker and divide the imagejobs in small parts that get assigned to each worker
            List<BackgroundWorker> bw = new List<BackgroundWorker>();
            int jobsize = jobs.Count / max_Worker + jobs.Count % max_Worker;
            int finished_Worker = 0;

            //initialized with same length as jobs, so we can insert the in random order returned images at the right place, without having to sort them afterwards
            CImageResult[] convertedframes = new CImageResult[jobs.Count];

            for (int i = 0; i < max_Worker; i++)
            {
                List<CImageJob> jobspart = new List<CImageJob>();
                for (int j = 0; j < jobsize; j++)
                    if (jobs.Count > i * jobsize + j)
                        jobspart.Add(jobs[i * jobsize + j]);

                CWorkerJob workerJob = new CWorkerJob(jobspart, i);

                bw.Add(new BackgroundWorker());
                bw[i].DoWork += (w_s, w_e) =>
                {
                    BackgroundWorker worker = (BackgroundWorker)w_s;
                    CWorkerJob wJ = w_e.Argument as CWorkerJob;

                    w_e.Result = "工作 " + wJ.workerID.ToString() + " 停止未完成";
                    CConverter c = new CConverter();
                    for (int j = 0; j < wJ.imagejobs.Count; j++)
                    {
                        try
                        {
                            CImageResult cac = c.CombineAndConvert(wJ.imagejobs[j].files);
                            Bitmap img = cac.bmp;
                            if (wJ.imagejobs[j].files[0].RadarColorAverage) wJ.imagejobs[j].files[0].RadarColor = cac.RadarColor;

                            if (wJ.imagejobs[j].CreateImageFile)
                            {
                                ImageFormat imfo = getImageFormat(wJ.imagejobs[j].tmpfileformat);
                                if ((imfo != null) && (wJ.imagejobs[j].tmpfileformat != "SHP(TS)"))
                                    img.Save(wJ.imagejobs[j].outputfilename + "." + wJ.imagejobs[j].tmpfileformat, imfo);
                                else
                                    CSHaPer.CreateSHP(wJ.imagejobs[j].outputfilename + ".SHP", new CImageResult[] { cac }, PreventWobbleBug, optimizeCanvas, keepCentered);
                            }
                            //the first image file sets the encoding format (previous checks make sure, that at this point is always a first file present)
                            SHP_TS_EncodingFormat format = wJ.imagejobs[j].files[0].CompressionFormat;
                            if (format == SHP_TS_EncodingFormat.Undefined) format = wJ.imagejobs[j].DefaultCompression;
                            string report = " 创建";
                            if (!wJ.imagejobs[j].CreateImageFile) report = " 处理";
                            worker.ReportProgress(j, new CImageResult(img,
                                wJ.imagejobs[j].frameNr, format,
                                wJ.imagejobs[j].files[0].BitFlags,
                                wJ.imagejobs[j].files[0].RadarColor,
                                wJ.imagejobs[j].tmpfilename + report + Environment.NewLine));
                        }
                        catch (Exception ex)
                        {
                            worker.ReportProgress(-1, "Job#" + j.ToString() + " 错误!\n" + ex.Message);
                            return;
                        }
                    }
                    w_e.Result = "工作 " + wJ.workerID.ToString() + " 完成";
                };

                bw[i].ProgressChanged += (w_s, w_e) =>
                {
                    if (w_e.ProgressPercentage != -1)
                    {
                        CImageResult wr = (CImageResult)w_e.UserState;

                        if (wr.frameNr < convertedframes.Length)
                            convertedframes[wr.frameNr] = wr;

                        this.richTextBox_Reports.SelectedText = wr.message;
                        if (this.progressBar1.Value + 1 < this.progressBar1.Maximum)
                            this.progressBar1.Value += 1;
                    }
                    else
                    {
                        this.richTextBox_Reports.SelectionColor = Color.Red;
                        this.richTextBox_Reports.SelectedText = "工作错误:" + w_e.UserState.ToString();
                    }
                };

                bw[i].RunWorkerCompleted += (w_s, w_e) =>
                {
                    if ((!w_e.Cancelled) && (w_e.Error == null))
                    {
                        this.richTextBox_Reports.SelectedText = w_e.Result.ToString() + Environment.NewLine;
                        finished_Worker++;
                        if (finished_Worker >= max_Worker)
                        {
                            this.progressBar1.Value = 0;
                            this.richTextBox_Reports.SelectedText = "___ 创建 SHP ___" + Environment.NewLine;

                            //lets take here the convertedframes and create the SHP
                            try
                            {
                                int splitFramesCount = convertedframes.Length / SplitResultCount;
                                int maxdigits = (int)Math.Floor(Math.Log10(SplitResultCount) + 1);

                                if (SplitResultCount <= convertedframes.Length)
                                    for (int r = 0; r < SplitResultCount; r++)
                                    {
                                        string SHPFilenameResult = SHPFilename + SHPFileExtension;
                                        if (SplitResultCount > 1) SHPFilenameResult = SHPFilename + "_" + r.ToString().PadLeft(maxdigits, '0') + SHPFileExtension;

                                        CImageResult[] SplitFrames = new CImageResult[splitFramesCount];
                                        Array.Copy(convertedframes, r * splitFramesCount, SplitFrames, 0, splitFramesCount);

                                        CSHaPer.CreateSHP(Path.Combine(outputfolder, SHPFilenameResult), SplitFrames, PreventWobbleBug, optimizeCanvas, keepCentered);
                                        this.richTextBox_Reports.SelectedText = "SHP 文件 [" + SHPFilenameResult + "] 在输出文件夹中创建:" + Environment.NewLine;
                                        this.richTextBox_Reports.SelectedText = "\t" + outputfolder + Environment.NewLine;
                                        if (RunAsCommand)
                                            Console.WriteLine("SHP 文件 [" + SHPFilenameResult + "] 在输出文件夹中创建" + outputfolder);
                                        PlaySound();
                                    }
                                else
                                {
                                    this.richTextBox_Reports.SelectionColor = Color.Red;
                                    this.richTextBox_Reports.SelectedText = "无法拆分 " + convertedframes.Length.ToString() + " 帧于 " + SplitResultCount.ToString() + " 文件." + Environment.NewLine;
                                }
                                //CSHaPer.CreateSHP(Path.Combine(programpath, SHPFilename), convertedframes, PreventWobbleBug, optimizeCanvas, keepCentered);
                                //this.richTextBox_Reports.SelectedText = "SHP File [" + SHPFilename + "] created" + Environment.NewLine;
                            }
                            catch (Exception ex)
                            {
                                this.richTextBox_Reports.SelectionColor = Color.Red;
                                this.richTextBox_Reports.SelectedText = "CSHaPer.CreateSHP 错误:" + ex.Message + Environment.NewLine;
                                if (RunAsCommand)
                                    Console.WriteLine("CSHaPer.CreateSHP 错误:" + ex.Message);
                            }

                            duration.Stop();
                            this.richTextBox_Reports.SelectedText = "时间: " + duration.Elapsed.TotalSeconds.ToString("0.0sec") + Environment.NewLine;
                            try
                            {
                                using (TextWriter w = File.CreateText(Path.Combine(ouputfolder_Temp, "#conversionlog.txt")))
                                {
                                    w.Write(this.richTextBox_Reports.Text);
                                }
                            }
                            catch (Exception ex)
                            {
                                this.richTextBox_Reports.SelectionColor = Color.Red;
                                this.richTextBox_Reports.SelectedText = "日志错误:" + ex.Message + Environment.NewLine;
                            }
                            this.button_Start.Enabled = true;
                            this.dataGridView_Files.Focus();

                            if (CloseWhenFinished)
                            {
                                System.Threading.Thread.Sleep(1000);
                                this.Close();
                            }
                        }
                    }
                    else
                    {
                        if (w_e.Error != null)
                        {
                            this.richTextBox_Reports.SelectionColor = Color.Red;
                            this.richTextBox_Reports.SelectedText = w_e.Error.Message + Environment.NewLine;
                        }
                    }
                };

                bw[i].WorkerReportsProgress = true;

                bw[i].RunWorkerAsync(workerJob);
            }
        }

        BackgroundWorker PreviewBW;
        private void UpdatePreview()
        {
            List<CImageFile> files = new List<CImageFile>();
            if (this.dataGridView_Files.SelectedRows.Count > 0)
            {
                for (int i = 0; i < this.dataGridView_Files.SelectedRows[0].Cells.Count; i++)
                    if (this.dataGridView_Files.SelectedRows[0].Cells[i].Value != null)
                    {
                        files.Add((CImageFile)this.dataGridView_Files.SelectedRows[0].Cells[i].Value);
                    }
            }
            else
                if (this.dataGridView_Files.SelectedCells.Count > 0)
                    if (this.dataGridView_Files.SelectedCells[0].Value != null)
                        files.Add((CImageFile)this.dataGridView_Files.SelectedCells[0].Value);

            if (files.Count > 0)
            {
                if (files[0].PaletteIndex != -1)
                    this.uC_Palette1.Palette = PaletteManager.GetPalette(files[0].PaletteIndex);

                this.button_RadarColor.Tag = files[0].RadarColor;
                this.button_RadarColor.Text = Cinimanager.ColorToStr(files[0].RadarColor, true);
                this.checkBox_RadarColorAverage.Checked = files[0].RadarColorAverage;

                this.checkBox_CombineTransparency.Checked = files[0].CombineTransparentPixel;
                this.checkBox_UseCustomBackgroundColor.Checked = files[0].UseCustomBackgroundColor;
                this.button_CustomBackgroundColor.Tag = files[0].CustomBackgroundColor;
                this.button_CustomBackgroundColor.Text = Cinimanager.ColorToStr(files[0].CustomBackgroundColor, true);

                this.comboBox_Compression.SelectedIndex = (int)files[0].CompressionFormat;
                SetBitField(files[0].BitFlags, files[0].CompressionFormat);

                if ((form_Preview != null) && (ShowPreview))
                {
                    if ((this.toolStripMenuItem_previewBackgroundImage.ToolStrip_UC_FolderSelector.Value != "")
                     && (System.IO.File.Exists(this.toolStripMenuItem_previewBackgroundImage.ToolStrip_UC_FolderSelector.Value)))
                    {
                        try
                        {
                            form_Preview.SetBackgroundImage(Image.FromFile(this.toolStripMenuItem_previewBackgroundImage.ToolStrip_UC_FolderSelector.Value));
                        }
                        catch (Exception ex)
                        {
                            form_Preview.SetBackgroundImage(null);
                            this.richTextBox_Reports.SelectionColor = Color.Red;
                            this.richTextBox_Reports.SelectedText = "背景图像错误:" + ex.Message + Environment.NewLine;
                        }
                    }
                    else
                        form_Preview.SetBackgroundImage(null);

                    if ((PreviewBW != null) && (PreviewBW.IsBusy))
                        PreviewBW.CancelAsync();
                    PreviewBW = new BackgroundWorker();
                    PreviewBW.DoWork += (w_s, w_e) =>
                    {
                        BackgroundWorker worker = (BackgroundWorker)w_s;
                        CImageJob job = w_e.Argument as CImageJob;

                        w_e.Result = "预览工作线程已停止未完成";
                        CConverter c = new CConverter();
                        CImageResult cac;
                        try
                        {
                            cac = c.CombineAndConvert(job.files);
                        }
                        catch (Exception ex)
                        {
                            worker.ReportProgress(-1, ex.Message);
                            return;
                        }
                        Bitmap img = cac.bmp;
                        if (job.files[0].RadarColorAverage) job.files[0].RadarColor = cac.RadarColor;
                        //do not update the preview if this was cancelled
                        if (!worker.CancellationPending)
                            worker.ReportProgress(100, new CImageResult(img,
                                                            job.frameNr,
                                                            job.files[0].CompressionFormat,
                                                            job.files[0].BitFlags,
                                                            job.files[0].RadarColor,
                                                            job.tmpfilename + " 创建" + Environment.NewLine));
                        w_e.Result = "预览工作线程已完成";
                    };

                    PreviewBW.ProgressChanged += (w_s, w_e) =>
                    {
                        if (w_e.ProgressPercentage != -1)
                        {
                            CImageResult wr = (CImageResult)w_e.UserState;
                            if (form_Preview != null)
                                form_Preview.SetImage(wr.bmp);
                        }
                        else
                        {
                            this.richTextBox_Reports.SelectionColor = Color.Red;
                            this.richTextBox_Reports.SelectedText = "工作错误:" + w_e.UserState.ToString() + Environment.NewLine;
                        }

                    };

                    PreviewBW.RunWorkerCompleted += (w_s, w_e) =>
                    {
                        if ((!w_e.Cancelled) && (w_e.Error == null))
                        {
                        }
                        else
                        {
                            this.richTextBox_Reports.SelectionColor = Color.Red;
                            this.richTextBox_Reports.SelectedText = "预览工作错误：" + w_e.Error.Message + Environment.NewLine;
                        }
                    };

                    PreviewBW.WorkerReportsProgress = true;
                    PreviewBW.WorkerSupportsCancellation = true;

                    PreviewBW.RunWorkerAsync(new CImageJob("", -1, "", "", files, false, GetDefaultCompression));

                }
            }
        }

        private void InfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form_AboutBox ab = new Form_AboutBox();
            ab.Icon = this.Icon;
            ab.StartPosition = FormStartPosition.Manual;
            ab.Location = new Point(this.Location.X, PointToScreen(this.customMenuStrip1.Location).Y);
            ab.Height = this.Height - this.customMenuStrip1.Height - 20;
            ab.Width = this.Width;
            ab.Text = "关于 " + ProgramName;

            Font f = new Font("华文隶书", 29.0f, GraphicsUnit.Pixel);
            ab.AddText("ImageShaper汉化版", Color.Red, f);
            ab.AddEmptyLine(1);
            System.Reflection.Assembly thisAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            string version = thisAssembly.GetName().Version.Major.ToString("D2") + "." +
                                     thisAssembly.GetName().Version.Minor.ToString("D2") + "." +
                                     thisAssembly.GetName().Version.Build.ToString("D2") + "." +
                                     thisAssembly.GetName().Version.Revision.ToString("D2");

            ab.AddText("版本: \t\t" + version + "\r\n" +
                       "版权: \t" + "LKO Software Solutions\r\n" +
                       "公司: \t" + "LKO Industries\r\n" +
                       "汉化: \t" + "AoMe Studio\r\n" +
                       "      \t" + "Ленинград\r\n" +
                       "      \t" + "QQ:3376018813" +
					   "      \t" + "AoMe奥美工作室 QQ群号：637059474 \r\n");

            ab.AddEmptyLine(1);
            ab.AddText("支持: \t\t"); ab.InsertLink("www.ppmsite.com");
            ab.AddEmptyLine();
            ab.AddUnderLine();
            ab.AddText("该程序允许合并图像并将图像序列转换为SHP文件。");
            ab.AddEmptyLine(1);
            ab.AddText("将文件拖放到数据网格上，或使用其右键单击上下文菜单加载图像文件。");
            ab.AddEmptyLine(2);
            ab.AddText("3列可以保存从右到左组合的不同图像。\n第 3 列中的图像被复制到第 2 列图像上，再复制到第 1 列图像上。");
            ab.AddEmptyLine(1);
            ab.AddText("因此，每一行都表示当时生成的 SHP 文件的一个帧。");
            ab.AddEmptyLine(1);
            ab.AddText("这允许从不同渲染通道的图像中一步创建SHP。 (e.g. 普通图像 + 发光颜色图像)");

            ab.AddEmptyLine(2);
            ab.AddText("为了避免数十个不同的调色板进行颜色转换，可以忽略颜色。");
            ab.AddEmptyLine(1);
            ab.AddText("右键单击调色板以加载调色板。 如果加载了调色板，则右键单击上下文菜单将允许或忽略某些颜色");
            ab.AddEmptyLine(1);
            ab.AddText("标记为“设为透明”，这意味着将转换为此颜色的像素将改为设置为颜色 #0, 这是透明的。");
            ab.AddEmptyLine(1);
            ab.AddText("笔记: 不要忘记右键单击图像并在图像上下文菜单中设置“设置调色板”，以便将修改后的调色板分配给图像。");
            ab.AddEmptyLine(1);
            ab.AddText("最好的方法是首先选择所有图像，而不是应该获得相同的调色板，然后进行调色板调整，最后通过“设置调色板”将调色板分配给图像。");
            ab.AddEmptyLine(2);
            ab.AddText("");
            ab.AddEmptyLine(1);
            ab.AddUnderLine();
            f = new Font("微软雅黑", 15.0f, FontStyle.Bold, GraphicsUnit.Pixel);
            ab.AddText("更新日志:", Color.Black, f);
            ab.AddEmptyLine();
            ab.AddText("--Version 01.00.00.01--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t发布了第一个公开版本");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.00.00.02--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(修复BUG) 编译为x86以匹配x86 MagickLibrary并防止在x64系统上不匹配。");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.00--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) SHP创建了例程，这允许摆脱大型ImageMagick库和有限的ShapeSet工具。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 实现了 SHP 读取例程，允许加载 SHP 并列出帧以进行进一步的重新保存和修改。");
            ab.AddEmptyLine(1);
            ab.AddText("\t\t加载的 SHP 不会再次转换颜色，而是读取/显示为索引调色板图像。颜色转换方法不适用于这些。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 添加了雷达颜色设置，允许修改/设置每个帧的雷达颜色。TS/RA2 仅用于覆盖 SHP，如泰伯利亚/矿石。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 添加了为每个已处理帧创建图像的选项。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 添加了防止 TS 中摆动错误的选项。这可以修复炮塔 SHP 单元，并防止它们在旋转炮塔或使用移动动画时向下摆动 1 个像素。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 用于设置每个帧的压缩的选项。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 将鼠标悬停在预览图像中调色板索引像素上，会突出显示调色板中该像素的已用颜色");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.01--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-编译为目标平台 “Any CPU”");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.02--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-SHP转换过程完成后，焦点被设置回数据网格");
            ab.AddEmptyLine(1);
            ab.AddText("\t-无论如何，仍会加载具有错误压缩位设置的 SHP");
            ab.AddEmptyLine(1);
            ab.AddText("\t-上次使用过的设置是从 ImageShaper 存储/加载的.ini");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.03--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) 位置和大小的错误 ini 值不再崩溃");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 添加了简单的调色板管理器，可跟踪不同的调色板设置");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 图像文件仅指向管理器中的调色板，并且不保留其完整副本，因此希望减少内存使用量");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 调色板编辑会立即应用于共享当前所选调色板的所有文件");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 改进了删除文件/帧的性能");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.04--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 图像/帧仅存储调色板管理器中已用调色板的 int 索引，从而减少每个图像/帧的内存使用量");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.05--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) 调色板管理器在具有未分配调色板的图像上崩溃");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.06--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-对内部存储图像/帧的类进行一些调整");
            ab.AddEmptyLine(1);
            ab.AddText("\t-进度条显示导入文件的当前进度");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.07--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) 不支持加载调色板索引图像。现在直接加载，无需任何颜色转换。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) 加载在第 2 个或第 3 个图像列表中的 SHP 帧导致异常");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 直接支持具有 32bpp ARGB、24bpp RGB 和 8bpp 调色板索引颜色格式的图像。在以颜色转换处理图像之前，所有其他图像颜色格式都转换为 32bpp ARGB。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 添加了用于优化画布大小的选项");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 添加了位标志");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 用于计算添加的平均雷达颜色的选项（忽略所有透明背景像素)");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 识别 SHP 的例程改为不那么严格。以前，BitFlag 值为 >3 的任何内容都会作为无效的 SHP 被丢弃。");


            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.08--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) 包含空帧时优化画布失败");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) 在使用 “RLE_Zero” 压缩时, 未在 SHP 图像中设置位标识");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 上次使用的平均颜色复选框值存储在 ini 中");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 启用 “优化画布”时，默认情况下也会启用 “保持居中”。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 添加了选项以在颜色转换期间使用自定义颜色作为透明背景色");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 添加了此选项，用于在将此图像与基本图像组合时仅复制透明像素");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) “图像/帧设置”中的更改将立即应用于选定的图像/帧");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.09--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 创建使用 SHP（TS） 文件格式扩展的图像，允许将每个帧另存为 SHP");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 创建图像文件名现在可以使用星号 *，以保留单个框架的原始文件名");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.10--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) 当编码结果数据大于未压缩数据的 2 倍时，RLE-Zero 编码算法崩溃（RLE 编码的最坏情况是未压缩数据的 3 倍大小)");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) 导入的文件忽略了“固定背景颜色”设置");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 在数据网格视图中导入文件的速度更快（现在导入完成后仅刷新一次)");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.11--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 支持 JASC 调色板格式");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 加载文件现在在单独的线程中完成，以防止/减少界面冻结");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) 加载多个 SHP 时，在每个 SHP 最后一帧之后添加一个空单元格");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.12--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 如果预览窗口已聚焦，则 ctrl+c 会将图像复制到剪贴板中");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 添加了拆分结果，允许将帧均匀地拆分为多个 SHP");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.13--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) RLE编码器固定（再次）。现在，如果编码数据超过预先分配的数组大小，则在编码期间调整数组大小。");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.14--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) RLE 编码器固定（再次）");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.15--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 内部拖放已添加到数据网格单元格中。如果单元格为空，则设置 d&d 值，否则插入新行。现有值不会被替换！");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) [文件] 菜单添加到菜单条中，该菜单提供了保存和加载项目的功能。项目包括来自数据网格和完整调色板设置的数据。");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.16--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) “从剪贴板加载” 添加到数据网格右键单击菜单。剪贴板图像保存在 Temp 子文件夹中。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) 复制，剪切，粘贴到数据网格中右键单击菜单固定");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 数据网格确保末尾有一个空行");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 预览窗口也会移动到前面，当程序获得焦点时");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.17--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) 添加文件时删除了烦人的窗口警告声音");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) “合并透明像素”仅应用于其左侧列中的图像");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 如果在添加文件时选择了多个单元格，则会询问用户是否应将文件复制到单元格中");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 音频文件SHP完成.wav在SHP转换完成后播放");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.18--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 支持 RGBA JASC 调色板");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 支持 Alpha 通道的Euclidean颜色转换");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) [选项] 菜单已添加");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) “#工作线程” 设置已移至[选项]");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) “输出文件夹” 设置已添加到[选项]以定义SHP的输出文件夹");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 支持保存/加载调色板设置。在“加载/保存调色板”对话框中选择“图像形状调色板设置”作为文件格式。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 添加了控制台命令行功能。运行“ImageShaper.exe ？”寻求帮助。");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.19--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) 预览显示Alpha通道正确");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) “预览背景图像” 设置已添加到[选项]");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.20--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 对调色板进行更改时，将自动预览所选图像的预览更新（例如，颜色设置为“忽略”）");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 聚焦数据网格后，CTRL+V 将剪贴板图像复制到其中");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.21--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 预览不再锁定显示文件的访问权限。现在可以在ImageShaper打开时替换/更改图像文件。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 新选项“更改所选单元格的顺序”已添加到数据网格上下文菜单中。");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.22--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 恢复 SHP 文件名仅在对图像文件进行操作时才删除尾随编号。加载 SHP 文件时，生成的文件名完全相同。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(错误修复) 在极少数情况下，“选择画布”在创建帧时会导致边界索引错误");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.23--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 添加了新格式 “Uncompressed_Full_Frame”，该格式以完整大小存储每个 SHP 帧。“优化画布”不适用于这种情况");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 添加了命令行选项以支持设置“拆分结果”");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.24--", Color.Blue, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 新功能“加载和拆分图像”添加到右键单击上下文菜单中，它允许加载带有帧的图像作为图像内部的面板。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 新工具“FireFLH Finder”添加到菜单栏“Tools”中，这使得很容易找到FireFLH，PrimaryFirePixelOffset，DamageFireOffset#和MuzzeFlash#的正确值。");
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 每当图像更改时，自定义图像控件（例如预览窗口）不再重置视图/滚动条。");

            ab.AddEmptyLine();
            ab.AddText("--Version 01.01.00.25--  (当前版本)", Color.Gold, f);
            ab.AddEmptyLine(1);
            ab.AddText("\t-(更新) 多语言支持已经上线！");

            //TODO add auto Shadow generator
            ab.AddEmptyLine(1);
            ab.AddText("\n\t这是汉化的最新版本，如需要更新版本请联系3376018813@qq.com");

            ab.AddEmptyLine(1);
            ab.Show();
        }

        private void saveProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.DefaultExt = "isp";
            sfd.InitialDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            sfd.Filter = "ImageShaper项目|*.isp";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                if (File.Exists(sfd.FileName)) File.Delete(sfd.FileName);

                List<DGVCell> cells = new List<DGVCell>();
                foreach (DataGridViewRow row in this.dataGridView_Files.Rows)
                    foreach (DataGridViewCell cell in row.Cells)
                        if (cell.Value != null)
                            cells.Add(new DGVCell(cell));
                Cinimanager.SaveProject(sfd.FileName, cells.ToArray(), PaletteManager.Palettes);
            }
        }

        private void loadProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            ofd.Filter = "ImageShaper项目|*.isp";
            ofd.Multiselect = false;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                this.uC_Palette1.SetPalettes(Cinimanager.LoadPaletteSetup(ofd.FileName));
                DGVCell[] cells = Cinimanager.LoadProject(ofd.FileName);
                this.dataGridView_Files.Rows.Clear();
                foreach (DGVCell cell in cells)
                {
                    if (cell.ColumnIndex == 0)
                    {
                        DataGridViewRow row = (DataGridViewRow)this.dataGridView_Files.RowTemplate.Clone();
                        object[] values = new object[this.dataGridView_Files.ColumnCount];
                        values[cell.ColumnIndex] = cell.Value;
                        row.CreateCells(this.dataGridView_Files, values);
                        this.dataGridView_Files.Rows.Add(row);
                    }
                    else
                    {
                        if (cell.RowIndex < this.dataGridView_Files.RowCount)
                            this.dataGridView_Files[cell.ColumnIndex, cell.RowIndex].Value = cell.Value;
                    }
                }
                AddLastEmptyRow();
            }
        }

        private void AddLastEmptyRow()
        {
            if ((this.dataGridView_Files.RowCount > 0) && (!IsEmptyRow(this.dataGridView_Files.Rows[this.dataGridView_Files.RowCount - 1])))
            {
                DataGridViewRow row = (DataGridViewRow)this.dataGridView_Files.RowTemplate.Clone();
                this.dataGridView_Files.Rows.Add(row);
            }
        }
        private void RemoveLastEmptyRow()
        {
            if ((this.dataGridView_Files.RowCount > 0) && (IsEmptyRow(this.dataGridView_Files.Rows[this.dataGridView_Files.RowCount - 1])))
                this.dataGridView_Files.Rows.RemoveAt(this.dataGridView_Files.RowCount - 1);
        }

        private void PlaySound()
        {
            string sound = "SHPfinished.wav";

            string soundfile = Path.Combine(GetProgramPath, sound);
            try
            {
                using (System.Media.SoundPlayer sp = new System.Media.SoundPlayer(soundfile))
                {
                    sp.Play();
                }
            }
            catch { }
        }


        bool RunAsCommand = false;
        internal void RunCommand(string[] args)
        {
            RunAsCommand = true;
            bool setbits = false;
            bool setCompression = false;
            bool closewhenfinished = true;
            for (int i = 0; i < args.Length; i++)
            {
                string argvalue = "";
                if (args[i].Contains('=')) argvalue = args[i].Split('=')[1];

                if (args[i].StartsWith("-o="))
                    this.toolStripMenuItem_Outputfolder.ToolStrip_UC_FolderSelector.Value = argvalue;

                if (args[i].StartsWith("-p="))
                {
                    Console.WriteLine("加载调色板 [" + argvalue + "]");
                    this.uC_Palette1.LoadPalette(argvalue);
                }

                if (args[i].StartsWith("-c="))
                {
                    setCompression = true;
                    switch (argvalue.ToLower())
                    {
                        case "0":
                        case "undefined": this.comboBox_Compression.SelectedIndex = 0; break;
                        case "1":
                        case "uncompressed": this.comboBox_Compression.SelectedIndex = 1; break;
                        case "2":
                        case "rle_zero": this.comboBox_Compression.SelectedIndex = 2; break;
                        case "3":
                        case "detect_best_size": this.comboBox_Compression.SelectedIndex = 3; break;
                        case "4":
                        case "uncompressed_full_frame": this.comboBox_Compression.SelectedIndex = 4; break;
                    }
                }

                if (args[i].StartsWith("-i="))
                {
                    AddFilesToDataGridSync(GetCommandFiles(argvalue), 0, -1, setbits, setCompression);
                }

                //general settings that don't affect file loading order/settings
                if (args[i].StartsWith("-z"))
                    closewhenfinished = false;

                if (args[i].StartsWith("-optcan="))
                {
                    switch (argvalue)
                    {
                        case "0":
                        case "off":
                        case "no": this.checkBox_OptimizeCanvas.Checked = false; break;
                        case "1":
                        case "on":
                        case "yes": this.checkBox_OptimizeCanvas.Checked = true; break;
                    }
                }

                if (args[i].StartsWith("-centered="))
                {
                    switch (argvalue)
                    {
                        case "0":
                        case "off":
                        case "no": this.checkBox_KeepCentered.Checked = false; break;
                        case "1":
                        case "on":
                        case "yes": this.checkBox_KeepCentered.Checked = true; break;
                    }
                }

                if (args[i].StartsWith("-stopwobblebug="))
                {
                    switch (argvalue)
                    {
                        case "0":
                        case "off":
                        case "no": this.checkBox_PreventWobbleBug.Checked = false; break;
                        case "1":
                        case "on":
                        case "yes": this.checkBox_PreventWobbleBug.Checked = true; break;
                    }
                }

                if (args[i].StartsWith("-split="))
                {
                    int splitvalue = 1;
                    if ((int.TryParse(argvalue, out splitvalue)) && (splitvalue > 0) && (splitvalue <= this.numericUpDown_SplitResult.Maximum))
                        this.numericUpDown_SplitResult.Value = splitvalue;
                }
            }
            CreatSHP(closewhenfinished);
            if (!this.IsDisposed)
                this.ShowDialog();
        }
        private static string[] GetCommandFiles(string p)
        {
            List<string> files = new List<string>();
            string path = System.IO.Path.GetDirectoryName(p);
            string filename = System.IO.Path.GetFileName(p);

            try
            {
                files.AddRange(System.IO.Directory.GetFiles(path, filename));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return files.ToArray();
        }

        private void FireFLHFinderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form_FireFLHFinder flhfinder = new Form_FireFLHFinder();
            flhfinder.Icon = this.Icon;
            flhfinder.StartPosition = FormStartPosition.Manual;
            flhfinder.Location = this.PointToScreen(this.customMenuStrip1.Location);
            flhfinder.Palette = PaletteManager.GetPalette(0);
            flhfinder.InitialDirectory = this.toolStripMenuItem_Outputfolder.ToolStrip_UC_FolderSelector.Value;
            flhfinder.Show();
        }

        private void uC_Palette1_Load(object sender, EventArgs e)
        {

        }

        private void dataGridView_Files_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void customMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void toolStripLabel_DefaultCompression_Click(object sender, EventArgs e)
        {

        }

        private void toolsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void checkBox_PreventWobbleBug_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
