
namespace MvkLauncher
{
    partial class FormLauncher
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.openGLControl1 = new SharpGL.OpenGLControl();
            ((System.ComponentModel.ISupportInitialize)(this.openGLControl1)).BeginInit();
            this.SuspendLayout();
            // 
            // openGLControl1
            // 
            this.openGLControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.openGLControl1.DrawFPS = false;
            this.openGLControl1.Location = new System.Drawing.Point(0, 0);
            this.openGLControl1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.openGLControl1.Name = "openGLControl1";
            this.openGLControl1.OpenGLVersion = SharpGL.Version.OpenGLVersion.OpenGL2_1;
            this.openGLControl1.RenderContextType = SharpGL.RenderContextType.NativeWindow;
            this.openGLControl1.RenderTrigger = SharpGL.RenderTrigger.Manual;
            this.openGLControl1.Size = new System.Drawing.Size(800, 450);
            this.openGLControl1.TabIndex = 0;
            this.openGLControl1.OpenGLInitialized += new System.EventHandler(this.OpenGLControl1_OpenGLInitialized);
            this.openGLControl1.OpenGLDraw += new SharpGL.RenderEventHandler(this.OpenGLControl1_OpenGLDraw);
            this.openGLControl1.Resized += new System.EventHandler(this.OpenGLControl1_Resized);
            this.openGLControl1.Load += new System.EventHandler(this.openGLControl1_Load);
            this.openGLControl1.Enter += new System.EventHandler(this.OpenGLControl1_Enter);
            this.openGLControl1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OpenGLControl1_KeyDown);
            this.openGLControl1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.OpenGLControl1_KeyPress);
            this.openGLControl1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.OpenGLControl1_KeyUp);
            this.openGLControl1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OpenGLControl1_MouseDown);
            this.openGLControl1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OpenGLControl1_MouseMove);
            this.openGLControl1.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OpenGLControl1_MouseUp);
            this.openGLControl1.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.OpenGLControl1_PreviewKeyDown);
            // 
            // FormLauncher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.openGLControl1);
            this.Name = "FormLauncher";
            this.Text = "Form1";
            this.Deactivate += new System.EventHandler(this.FormLauncher_Deactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormLauncher_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormLauncher_FormClosed);
            this.Load += new System.EventHandler(this.FormLauncher_Load);
            ((System.ComponentModel.ISupportInitialize)(this.openGLControl1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private SharpGL.OpenGLControl openGLControl1;
    }
}