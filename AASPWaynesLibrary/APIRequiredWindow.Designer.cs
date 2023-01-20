namespace AASPWaynesLibrary
{
    partial class APIRequiredWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(APIRequiredWindow));
            this.appIdTB = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.objectTB = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.autoAppAccountCB = new System.Windows.Forms.CheckBox();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // appIdTB
            // 
            this.appIdTB.Location = new System.Drawing.Point(76, 307);
            this.appIdTB.Name = "appIdTB";
            this.appIdTB.Size = new System.Drawing.Size(295, 23);
            this.appIdTB.TabIndex = 1;
            // 
            // textBox2
            // 
            this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox2.Location = new System.Drawing.Point(12, 12);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.ReadOnly = true;
            this.textBox2.Size = new System.Drawing.Size(359, 264);
            this.textBox2.TabIndex = 2;
            this.textBox2.Text = resources.GetString("textBox2.Text");
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 310);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 15);
            this.label1.TabIndex = 3;
            this.label1.Text = "Client ID:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 339);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 15);
            this.label2.TabIndex = 4;
            this.label2.Text = "Object ID:";
            // 
            // objectTB
            // 
            this.objectTB.Location = new System.Drawing.Point(76, 336);
            this.objectTB.Name = "objectTB";
            this.objectTB.Size = new System.Drawing.Size(295, 23);
            this.objectTB.TabIndex = 5;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(148, 365);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "Ok";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // autoAppAccountCB
            // 
            this.autoAppAccountCB.AutoSize = true;
            this.autoAppAccountCB.Checked = true;
            this.autoAppAccountCB.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoAppAccountCB.Location = new System.Drawing.Point(12, 282);
            this.autoAppAccountCB.Name = "autoAppAccountCB";
            this.autoAppAccountCB.Size = new System.Drawing.Size(303, 19);
            this.autoAppAccountCB.TabIndex = 7;
            this.autoAppAccountCB.Text = "Automatically Create Dataverse Application Account";
            this.autoAppAccountCB.UseVisualStyleBackColor = true;
            // 
            // linkLabel1
            // 
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.Location = new System.Drawing.Point(12, 42);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(322, 15);
            this.linkLabel1.TabIndex = 8;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "https://admin.powerplatform.microsoft.com/environments";
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.Link_Clicked);
            // 
            // APIRequiredWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(383, 400);
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.autoAppAccountCB);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.objectTB);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.appIdTB);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "APIRequiredWindow";
            this.Text = "API Required";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form_Closing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private TextBox textBox2;
        private Label label1;
        private Label label2;
        private Button button1;
        public TextBox appIdTB;
        public TextBox objectTB;
        public CheckBox autoAppAccountCB;
        private LinkLabel linkLabel1;
    }
}