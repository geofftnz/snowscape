namespace Snowscape
{
    partial class Startup
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
            this.RunGeneratorButton = new System.Windows.Forms.Button();
            this.RunViewerButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // RunGeneratorButton
            // 
            this.RunGeneratorButton.Location = new System.Drawing.Point(13, 13);
            this.RunGeneratorButton.Name = "RunGeneratorButton";
            this.RunGeneratorButton.Size = new System.Drawing.Size(259, 30);
            this.RunGeneratorButton.TabIndex = 0;
            this.RunGeneratorButton.Text = "Run Terrain Generator";
            this.RunGeneratorButton.UseVisualStyleBackColor = true;
            this.RunGeneratorButton.Click += new System.EventHandler(this.RunGeneratorButton_Click);
            // 
            // RunViewerButton
            // 
            this.RunViewerButton.Location = new System.Drawing.Point(13, 50);
            this.RunViewerButton.Name = "RunViewerButton";
            this.RunViewerButton.Size = new System.Drawing.Size(259, 27);
            this.RunViewerButton.TabIndex = 1;
            this.RunViewerButton.Text = "Run Terrain Viewer";
            this.RunViewerButton.UseVisualStyleBackColor = true;
            this.RunViewerButton.Click += new System.EventHandler(this.RunViewerButton_Click);
            // 
            // Startup
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 89);
            this.Controls.Add(this.RunViewerButton);
            this.Controls.Add(this.RunGeneratorButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "Startup";
            this.Text = "Snowscape";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button RunGeneratorButton;
        private System.Windows.Forms.Button RunViewerButton;

    }
}

