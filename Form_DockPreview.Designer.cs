﻿namespace ImageShaper
{
    partial class Form_DockPreview
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form_DockPreview));
            this.uC_ImageCanvas1 = new ImageShaper.UC_ImageCanvas();
            this.SuspendLayout();
            // 
            // uC_ImageCanvas1
            // 
            this.uC_ImageCanvas1.CaptureFocusOnMouseOver = true;
            this.uC_ImageCanvas1.CenterOffset = new System.Drawing.Point(0, 0);
            this.uC_ImageCanvas1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uC_ImageCanvas1.Location = new System.Drawing.Point(0, 0);
            this.uC_ImageCanvas1.LocationFromCenter = false;
            this.uC_ImageCanvas1.MinimumSize = new System.Drawing.Size(10, 9);
            this.uC_ImageCanvas1.Name = "uC_ImageCanvas1";
            this.uC_ImageCanvas1.PreviewPixel = null;
            this.uC_ImageCanvas1.ShowCenterCross = false;
            this.uC_ImageCanvas1.ShowColorInfo = true;
            this.uC_ImageCanvas1.Size = new System.Drawing.Size(292, 252);
            this.uC_ImageCanvas1.TabIndex = 1;
            this.uC_ImageCanvas1.ZoomLevel = 1;
            // 
            // Form_DockPreview
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 252);
            this.Controls.Add(this.uC_ImageCanvas1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(150, 141);
            this.Name = "Form_DockPreview";
            this.Text = "预览";
            this.Load += new System.EventHandler(this.Form_DockPreview_Load);
            this.ResumeLayout(false);

        }

        #endregion

        internal UC_ImageCanvas uC_ImageCanvas1;
    }
}