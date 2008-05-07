namespace LINQEntityBaseExample
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
            this.txtInfo = new System.Windows.Forms.TextBox();
            this.chkKeepOriginals = new System.Windows.Forms.CheckBox();
            this.btnGo = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtInfo
            // 
            this.txtInfo.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.txtInfo.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtInfo.Location = new System.Drawing.Point(0, 51);
            this.txtInfo.Multiline = true;
            this.txtInfo.Name = "txtInfo";
            this.txtInfo.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtInfo.Size = new System.Drawing.Size(649, 514);
            this.txtInfo.TabIndex = 0;
            // 
            // chkKeepOriginals
            // 
            this.chkKeepOriginals.AutoSize = true;
            this.chkKeepOriginals.Dock = System.Windows.Forms.DockStyle.Top;
            this.chkKeepOriginals.Location = new System.Drawing.Point(0, 0);
            this.chkKeepOriginals.Name = "chkKeepOriginals";
            this.chkKeepOriginals.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.chkKeepOriginals.Size = new System.Drawing.Size(649, 22);
            this.chkKeepOriginals.TabIndex = 2;
            this.chkKeepOriginals.Text = "Keep original values when modifications are made (reduces SQL Update statement si" +
                "ze)";
            this.chkKeepOriginals.UseVisualStyleBackColor = true;
            // 
            // btnGo
            // 
            this.btnGo.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnGo.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnGo.Location = new System.Drawing.Point(0, 22);
            this.btnGo.Name = "btnGo";
            this.btnGo.Size = new System.Drawing.Size(649, 23);
            this.btnGo.TabIndex = 4;
            this.btnGo.Text = "Click Here to Go Run Demo";
            this.btnGo.UseVisualStyleBackColor = true;
            this.btnGo.Click += new System.EventHandler(this.btnGo_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(649, 565);
            this.Controls.Add(this.btnGo);
            this.Controls.Add(this.chkKeepOriginals);
            this.Controls.Add(this.txtInfo);
            this.Name = "MainForm";
            this.Text = "LINQ Entity Base Class Examples";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtInfo;
        private System.Windows.Forms.CheckBox chkKeepOriginals;
        private System.Windows.Forms.Button btnGo;

    }
}