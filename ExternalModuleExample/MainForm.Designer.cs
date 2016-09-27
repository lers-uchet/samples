namespace ExternalModuleExample
{
    partial class MainForm
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
			this.MainView = new System.Windows.Forms.DataGridView();
			this.MeasurePointName = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.MeasurePointType = new System.Windows.Forms.DataGridViewTextBoxColumn();
			((System.ComponentModel.ISupportInitialize)(this.MainView)).BeginInit();
			this.SuspendLayout();
			// 
			// MainView
			// 
			this.MainView.AllowUserToAddRows = false;
			this.MainView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MainView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.MainView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.MeasurePointName,
            this.MeasurePointType});
			this.MainView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.MainView.Location = new System.Drawing.Point(12, 12);
			this.MainView.Name = "MainView";
			this.MainView.Size = new System.Drawing.Size(765, 376);
			this.MainView.TabIndex = 0;
			// 
			// MeasurePointName
			// 
			this.MeasurePointName.HeaderText = "Название точки учёта";
			this.MeasurePointName.Name = "MeasurePointName";
			this.MeasurePointName.ToolTipText = "MeasurePointName";
			// 
			// MeasurePointType
			// 
			this.MeasurePointType.HeaderText = "Тип точки учёта";
			this.MeasurePointType.Name = "MeasurePointType";
			this.MeasurePointType.ToolTipText = "MeasurePointType";
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(789, 400);
			this.Controls.Add(this.MainView);
			this.Name = "MainForm";
			this.Text = "MainForm";
			((System.ComponentModel.ISupportInitialize)(this.MainView)).EndInit();
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView MainView;
		private System.Windows.Forms.DataGridViewTextBoxColumn MeasurePointName;
		private System.Windows.Forms.DataGridViewTextBoxColumn MeasurePointType;
	}
}