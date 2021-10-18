// Copyright (c) Igor Velikorossov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsForms.Analyzers;
using VerifyCS = WindowsForms.Test.CSharpCodeFixVerifier<
    WindowsForms.Analyzers.ControlTabOrderAnalyzer,
    WindowsForms.ControlTabOrderAnalyzerCodeFixProvider>;

namespace WindowsForms.Analyzers.Tests
{
    partial class ControlTabOrderAnalyzerTests
    {
        partial class InconsistentTabIndexRule
        {
            [TestMethod]
            public async Task Analyzer_no_fields_no_locals_should_produce_no_diagnostics()
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
            public async Task Analyzer_fields_no_TabIndex_should_produce_no_diagnostics()
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
            this.Controls.Add(this.button1);
            this.Controls.Add(this.treeView1);
            this.Name = ""Form1"";
            this.Text = ""Form1"";
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TreeView treeView1 = new System.Windows.Forms.TreeView();
        private System.Windows.Forms.Button button1 = new System.Windows.Forms.Button();
        private System.Windows.Forms.Button button2 = new System.Windows.Forms.Button();
    }
}
";

                await VerifyCS.VerifyAnalyzerAsync(code);
            }

            [TestMethod]
            public async Task Analyzer_fields_with_correct_TabIndex_should_produce_no_diagnostics()
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

                await VerifyCS.VerifyAnalyzerAsync(code);
            }

            [TestMethod]
            public async Task Analyzer_local_no_TabIndex_should_produce_no_diagnostics()
            {
                string code = @"
using System.Windows.Forms;

namespace WinFormsApp1
{
    partial class Form1 : Form
    {
        private void InitializeComponent()
        {
            System.Windows.Forms.Button button1 = new System.Windows.Forms.Button();
            System.Windows.Forms.Button button2 = new System.Windows.Forms.Button();
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(578, 398);
            this.Controls.Add(button1);
            this.Controls.Add(button2);
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
            public async Task Analyzer_local_with_correct_TabIndex_should_produce_no_diagnostics()
            {
                string code = @"
using System.Windows.Forms;

namespace WinFormsApp1
{
    partial class Form1 : Form
    {
        private void InitializeComponent()
        {
            System.Windows.Forms.Button button1 = new System.Windows.Forms.Button();
            button1.TabIndex = 0;
            System.Windows.Forms.Button button2 = new System.Windows.Forms.Button();
            button2.TabIndex = 1;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(578, 398);
            this.Controls.Add(button1);
            this.Controls.Add(button2);
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
            public async Task Analyzer_fields_and_locals_with_incorrect_TabIndex_should_produce_diagnostics()
            {
                string code = @"
using System.Windows.Forms;

namespace WinFormsApp1
{
    partial class Form1 : Form
    {
        private void InitializeComponent()
        {
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            System.Windows.Forms.Button button3 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // treeView1
            // 
            this.treeView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeView1.Location = new System.Drawing.Point(12, 12);
            this.treeView1.Name = ""treeView1"";
            this.treeView1.Size = new System.Drawing.Size(473, 373);
            this.treeView1.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(491, 12);
            this.button1.Name = ""button1"";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = ""button1"";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Location = new System.Drawing.Point(491, 41);
            this.button2.Name = ""button2"";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = ""button2"";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            button3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            button3.Location = new System.Drawing.Point(491, 41);
            button3.Name = ""button3"";
            button3.Size = new System.Drawing.Size(75, 23);
            button3.TabIndex = 0;
            button3.Text = ""button3"";
            button3.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(578, 398);
            this.Controls.Add(this.button2);
            this.Controls.Add(button3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.treeView1);
            this.Name = ""Form1"";
            this.Text = ""Form1"";
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}
";

                await VerifyCS.VerifyAnalyzerAsync(code,
                    // /0/Test0.cs(61,13): warning WF0010: Control 'this.button2' has ordinal index of 0 but sets a different TabIndex of 2.
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(61, 13, 61, 45).WithSpan(61, 13, 61, 45).WithSpan(62, 13, 62, 40).WithSpan(63, 13, 63, 45).WithSpan(64, 13, 64, 47).WithArguments("this.button2", "0", "2"),
                    // /0/Test0.cs(62,13): warning WF0010: Control 'button3' has ordinal index of 1 but sets a different TabIndex of 0.
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(62, 13, 62, 40).WithSpan(61, 13, 61, 45).WithSpan(62, 13, 62, 40).WithSpan(63, 13, 63, 45).WithSpan(64, 13, 64, 47).WithArguments("button3", "1", "0"),
                    // /0/Test0.cs(63,13): warning WF0010: Control 'this.button1' has ordinal index of 2 but sets a different TabIndex of 1.
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(63, 13, 63, 45).WithSpan(61, 13, 61, 45).WithSpan(62, 13, 62, 40).WithSpan(63, 13, 63, 45).WithSpan(64, 13, 64, 47).WithArguments("this.button1", "2", "1"),
                    // /0/Test0.cs(64,13): warning WF0010: Control 'this.treeView1' has ordinal index of 3 but sets a different TabIndex of 0.
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(64, 13, 64, 47).WithSpan(61, 13, 61, 45).WithSpan(62, 13, 62, 40).WithSpan(63, 13, 63, 45).WithSpan(64, 13, 64, 47).WithArguments("this.treeView1", "3", "0")
                    );
            }
        }
    }
}
