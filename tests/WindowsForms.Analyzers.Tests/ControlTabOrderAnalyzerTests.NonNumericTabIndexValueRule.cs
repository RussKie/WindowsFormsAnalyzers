// Copyright (c) Igor Velikorossov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsForms.Analyzers;
using VerifyCS = WindowsForms.Test.CSharpCodeFixVerifier<
    WindowsForms.Analyzers.ControlTabOrderAnalyzer,
    WindowsForms.ControlTabOrderAnalyzerCodeFixProvider>;

namespace WindowsForms.Analyzers.Tests
{
    partial class ControlTabOrderAnalyzerTests
    {
        partial class NonNumericTabIndexValueRule
        {
            [TestMethod]
            public async Task Analyzer_non_numeric_TabIndex_should_produce_diagnostics()
            {
                var test = @"
using System.Windows.Forms;

namespace WinFormsApp1
{
    partial class Form1 : Form
    {
        private void InitializeComponent()
        {
            System.Windows.Forms.Button button1 = new System.Windows.Forms.Button();
            System.Windows.Forms.Button button2 = new System.Windows.Forms.Button();
            button2.TabIndex = INDEX;
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

        private const int INDEX = 1;
    }
}
";

                await VerifyCS.VerifyAnalyzerAsync(test,
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.NonNumericTabIndexValueRuleIdDescriptor).WithSpan(12, 32, 12, 37).WithArguments("button2", "INDEX"));
            }
        }
    }
}
