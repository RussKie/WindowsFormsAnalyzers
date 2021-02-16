// Copyright (c) Igor Velikorossov. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VerifyCS = WindowsForms.Test.CSharpCodeFixVerifier<
    WindowsForms.Analyzers.ControlTabOrderAnalyzer,
    WindowsForms.WinFormsAccessibilityCodeFixProvider>;

namespace WindowsForms.Test
{
    public partial class WinFormsAccessibilityTests
    {
        [TestClass]
        public class NonNumericTabIndexValueRule
        {
            [TestMethod]
            public async Task Non_numeric_TabIndex_should_produce_diagnostics()
            {
                var test = @"
namespace WinFormsApp1
{
    partial class Form1
    {
        private void InitializeComponent()
        {
            System.Windows.Forms.Button button1 = new System.Windows.Forms.Button();
            button1.TabIndex = 0;
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

                await VerifyCS.VerifyAnalyzerAsync(test);
            }
        }
    }
}
