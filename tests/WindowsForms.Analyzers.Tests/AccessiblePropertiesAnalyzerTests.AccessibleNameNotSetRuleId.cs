// Copyright (c) Igor Velikorossov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsForms.Analyzers;
using VerifyCS = WindowsForms.Test.CSharpAnalyzerVerifier<WindowsForms.Analyzers.AccessiblePropertiesAnalyzer>;

namespace WindowsForms.Test
{
    public partial class AccessiblePropertiesAnalyzerTests
    {
        [TestClass]
        public class AccessibleNameNotSetRuleId
        {
            [TestMethod]
            public async Task No_fields_no_locals_should_produce_diagnostics_if_no_AccessibleName()
            {
                string code = @"
using System.Windows.Forms;

namespace WinFormsApp1
{
    partial class Form1 : Form
    {
        private void InitializeComponent()
        {
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(578, 398);
            this.Name = ""Form1"";
            this.Text = ""Form1"";
            this.ResumeLayout(false);
        }
    }
}
";

                await VerifyCS.VerifyAnalyzerAsync(code);
            }

            [TestMethod]
            public async Task Fields_no_locals_should_produce_diagnostics_if_no_AccessibleName()
            {
                string code = @"
using System.Windows.Forms;

namespace WinFormsApp1
{
    partial class Form1 : Form
    {
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button2.TabIndex = 2;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(578, 398);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.treeView1);
            this.Controls.Add(this.button2);
            this.Name = ""Form1"";
            this.Text = ""Form1"";
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TreeView treeView1 = new System.Windows.Forms.TreeView();
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}
";

                await VerifyCS.VerifyAnalyzerAsync(code,
                    // /0/Test0.cs(12,13): warning WF1003: 'this.button2.AccessibleName' property must be set.
                    VerifyCS.Diagnostic(AccessiblePropertiesAnalyzer.AccessibleNameNotSetRuleIdDescriptor).WithSpan(12, 13, 12, 34).WithArguments("this.button2"));
            }
        }
    }
}
