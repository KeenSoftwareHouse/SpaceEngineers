#if !XB1

namespace Sandbox.Game.GUI.DebugInputComponents.HonzaDebugInputComponent
{
    partial class LiveWatch
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
            this.Watch = new System.Windows.Forms.DataGridView();
            this.Prop = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Value = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            ((System.ComponentModel.ISupportInitialize)(this.Watch)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Watch
            // 
            this.Watch.AllowUserToAddRows = false;
            this.Watch.AllowUserToDeleteRows = false;
            this.Watch.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.Watch.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Prop,
            this.Value});
            this.Watch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Watch.Location = new System.Drawing.Point(0, 0);
            this.Watch.Name = "Watch";
            this.Watch.ReadOnly = true;
            this.Watch.RowHeadersVisible = false;
            this.Watch.Size = new System.Drawing.Size(399, 324);
            this.Watch.TabIndex = 0;
            // 
            // Prop
            // 
            this.Prop.HeaderText = "Prop";
            this.Prop.Name = "Prop";
            this.Prop.ReadOnly = true;
            // 
            // Value
            // 
            this.Value.HeaderText = "Value";
            this.Value.Name = "Value";
            this.Value.ReadOnly = true;
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(399, 320);
            this.propertyGrid1.TabIndex = 1;
            this.propertyGrid1.Click += new System.EventHandler(this.propertyGrid1_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.Watch);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.propertyGrid1);
            this.splitContainer1.Size = new System.Drawing.Size(399, 648);
            this.splitContainer1.SplitterDistance = 324;
            this.splitContainer1.TabIndex = 2;
            // 
            // LiveWatch
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(399, 648);
            this.Controls.Add(this.splitContainer1);
            this.Name = "LiveWatch";
            this.Text = "LiveWatch";
            ((System.ComponentModel.ISupportInitialize)(this.Watch)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public System.Windows.Forms.DataGridView Watch;
        private System.Windows.Forms.DataGridViewTextBoxColumn Prop;
        private System.Windows.Forms.DataGridViewTextBoxColumn Value;
        private System.Windows.Forms.SplitContainer splitContainer1;
        public System.Windows.Forms.PropertyGrid propertyGrid1;

    }
}

#endif