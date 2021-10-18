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
            public async Task Fixer_single_fields_with_correct_TabIndex_should_not_trigger()
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
            this.SuspendLayout();
            // 
            // treeView1
            // 
            this.treeView1.TabIndex = 0;
            // 
            // Form1
            // 
            this.Controls.Add(this.treeView1);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TreeView treeView1;
    }
}
";

                var test = new VerifyCS.Test()
                {
                    TestCode = code,
                    FixedCode = code,
                    CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                };
                await test.RunAsync();
            }

            [TestMethod]
            public async Task Fixer_single_fields_with_incorrect_TabIndex_should_not_trigger()
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
            this.SuspendLayout();
            // 
            // treeView1
            // 
            this.treeView1.TabIndex = 1;
            // 
            // Form1
            // 
            this.Controls.Add(this.treeView1);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TreeView treeView1;
    }
}
";

                DiagnosticResult[] expected = new[]
                {
                    // /0/Test0.cs(19,13): warning WF1002: Control 'this.treeView1' has ordinal index of 0 but sets a different TabIndex of 1
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(19, 13, 19, 47).WithArguments("this.treeView1", "0", "1"),
                };

                var test = new VerifyCS.Test()
                {
                    TestCode = code,
                    FixedCode = code,
                    CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                };
                test.ExpectedDiagnostics.AddRange(expected);
                test.FixedState.ExpectedDiagnostics.AddRange(expected);

                await test.RunAsync();
            }

            [TestMethod]
            public async Task Fixer_local_with_incorrect_TabIndex_should_not_trigger()
            {
                string code = @"
using System.Windows.Forms;

namespace WinFormsApp1
{
    partial class Form1 : Form
    {
        private void InitializeComponent()
        {
            System.Windows.Forms.Button button3 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button3
            // 
            button3.TabIndex = 1;
            // 
            // Form1
            // 
            this.Controls.Add(button3);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TreeView treeView1;
    }
}
";

                DiagnosticResult[] expected = new[]
                {
                    // /0/Test0.cs(19,13): warning WF1002: Control 'button3' has ordinal index of 0 but sets a different TabIndex of 1
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(19, 13, 19, 40).WithArguments("button3", "0", "1"),
                };

                var test = new VerifyCS.Test()
                {
                    TestCode = code,
                    FixedCode = code,
                    CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                };
                test.ExpectedDiagnostics.AddRange(expected);
                test.FixedState.ExpectedDiagnostics.AddRange(expected);

                await test.RunAsync();
            }

            [TestMethod]
            public async Task Fixer_non_sequential_container_add_with_incorrect_TabIndex_should_not_trigger()
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
            this.treeView1.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.TabIndex = 2;
            // 
            // button3
            // 
            button3.TabIndex = 0;
            // 
            // Form1
            // 
            this.Controls.Add(this.button2);
            this.Controls.Add(button3);
            //
            this.Controls.Add(this.button1);
            this.Controls.Add(this.treeView1);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}
";

                DiagnosticResult[] expected = new[]
                {
                    // /0/Test0.cs(34,13): warning WF1002: Control 'this.button2' has ordinal index of 0 but sets a different TabIndex of 2
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(34, 13, 34, 45).WithArguments("this.button2", "0", "2"),
                    // /0/Test0.cs(35,13): warning WF1002: Control 'button3' has ordinal index of 1 but sets a different TabIndex of 0
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(35, 13, 35, 40).WithArguments("button3", "1", "0"),
                    // /0/Test0.cs(37,13): warning WF1002: Control 'this.button1' has ordinal index of 2 but sets a different TabIndex of 1
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(37, 13, 37, 45).WithArguments("this.button1", "2", "1"),
                    // /0/Test0.cs(38,13): warning WF1002: Control 'this.treeView1' has ordinal index of 3 but sets a different TabIndex of 0
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(38, 13, 38, 47).WithArguments("this.treeView1", "3", "0"),
                };

                var test = new VerifyCS.Test()
                {
                    TestCode = code,
                    FixedCode = code,
                    CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                };
                test.ExpectedDiagnostics.AddRange(expected);
                test.FixedState.ExpectedDiagnostics.AddRange(expected);

                await test.RunAsync();
            }

            [TestMethod]
            public async Task Fixer_fields_and_locals_with_incorrect_TabIndex_should_trigger_move_down_one()
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
            System.Windows.Forms.Button button3 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button3
            // 
            button3.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.TabIndex = 2;
            // 
            // Form1
            // 
            this.Controls.Add(this.button1);
            this.Controls.Add(button3);
            this.Controls.Add(this.button2);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}
";
                string fixedCode = @"
using System.Windows.Forms;

namespace WinFormsApp1
{
    partial class Form1 : Form
    {
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            System.Windows.Forms.Button button3 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button3
            // 
            button3.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.TabIndex = 2;
            // 
            // Form1
            // 
            this.Controls.Add(button3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.button2);
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}
";

                DiagnosticResult[] expected = new[]
                {
                    // /0/Test0.cs(29,13): warning WF1002: Control 'this.button1' has ordinal index of 0 but sets a different TabIndex of 1
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(29, 13, 29, 45).WithSpan(29, 13, 29, 45).WithSpan(30, 13, 30, 40).WithSpan(31, 13, 31, 45).WithArguments("this.button1", "0", "1"),
                    // /0/Test0.cs(30,13): warning WF1002: Control 'button3' has ordinal index of 1 but sets a different TabIndex of 0
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(30, 13, 30, 40).WithSpan(29, 13, 29, 45).WithSpan(30, 13, 30, 40).WithSpan(31, 13, 31, 45).WithArguments("button3", "1", "0"),
                };

                var test = new VerifyCS.Test()
                {
                    TestCode = code,
                    CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                    FixedState =
                    {
                        Sources = { fixedCode },
                    }
                };
                test.ExpectedDiagnostics.AddRange(expected);

                await test.RunAsync();
            }

            [TestMethod]
            public async Task Fixer_fields_and_locals_with_incorrect_TabIndex_should_trigger_move_down_more_than_one()
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
            this.treeView1.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.TabIndex = 2;
            // 
            // button3
            // 
            button3.TabIndex = 0;
            // 
            // Form1
            // 
            this.Controls.Add(this.button2);
            this.Controls.Add(button3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.treeView1);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}
";
                string fixedCode = @"
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
            this.treeView1.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.TabIndex = 2;
            // 
            // button3
            // 
            button3.TabIndex = 0;
            // 
            // Form1
            // 
            this.Controls.Add(button3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.treeView1);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}
";

                DiagnosticResult[] expected = new[]
                {
                    // /0/Test0.cs(34,13): warning WF1002: Control 'this.button2' has ordinal index of 0 but sets a different TabIndex of 2
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(34, 13, 34, 45).WithSpan(34, 13, 34, 45).WithSpan(35, 13, 35, 40).WithSpan(36, 13, 36, 45).WithSpan(37, 13, 37, 47).WithArguments("this.button2", "0", "2"),
                    // /0/Test0.cs(35,13): warning WF1002: Control 'button3' has ordinal index of 1 but sets a different TabIndex of 0
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(35, 13, 35, 40).WithSpan(34, 13, 34, 45).WithSpan(35, 13, 35, 40).WithSpan(36, 13, 36, 45).WithSpan(37, 13, 37, 47).WithArguments("button3", "1", "0"),
                    // /0/Test0.cs(36,13): warning WF1002: Control 'this.button1' has ordinal index of 2 but sets a different TabIndex of 1
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(36, 13, 36, 45).WithSpan(34, 13, 34, 45).WithSpan(35, 13, 35, 40).WithSpan(36, 13, 36, 45).WithSpan(37, 13, 37, 47).WithArguments("this.button1", "2", "1"),
                    // /0/Test0.cs(37,13): warning WF1002: Control 'this.treeView1' has ordinal index of 3 but sets a different TabIndex of 0
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(37, 13, 37, 47).WithSpan(34, 13, 34, 45).WithSpan(35, 13, 35, 40).WithSpan(36, 13, 36, 45).WithSpan(37, 13, 37, 47).WithArguments("this.treeView1", "3", "0"),
                };

                var test = new VerifyCS.Test()
                {
                    TestCode = code,
                    CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                    FixedState =
                    {
                        Sources = { fixedCode },
                    }
                };
                test.ExpectedDiagnostics.AddRange(expected);
                test.FixedState.ExpectedDiagnostics.Add(
                    // /0/Test0.cs(37,13): warning WF1002: Control 'this.treeView1' has ordinal index of 3 but sets a different TabIndex of 0
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(37, 13, 37, 47).WithSpan(34, 13, 34, 40).WithSpan(35, 13, 35, 45).WithSpan(36, 13, 36, 45).WithSpan(37, 13, 37, 47).WithArguments("this.treeView1", "3", "0")
                    );

                await test.RunAsync();
            }

            [TestMethod]
            public async Task Fixer_fields_and_locals_with_incorrect_TabIndex_should_trigger_move_up()
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
            this.treeView1.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.TabIndex = 2;
            // 
            // button3
            // 
            button3.TabIndex = 0;
            // 
            // Form1
            // 
            this.Controls.Add(button3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.treeView1);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}
";
                string fixedCode = @"
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
            this.treeView1.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.TabIndex = 1;
            // 
            // button2
            // 
            this.button2.TabIndex = 2;
            // 
            // button3
            // 
            button3.TabIndex = 0;
            // 
            // Form1
            // 
            this.Controls.Add(this.treeView1);
            this.Controls.Add(button3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.button2);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}
";

                DiagnosticResult[] expected = new[]
                {
                    // /0/Test0.cs(35,13): warning WF1002: Control 'button3' has ordinal index of 1 but sets a different TabIndex of 0
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(35, 13, 35, 40).WithSpan(34, 13, 34, 47).WithSpan(35, 13, 35, 40).WithSpan(36, 13, 36, 45).WithSpan(37, 13, 37, 45).WithArguments("button3", "1", "0"),
                    // /0/Test0.cs(36,13): warning WF1002: Control 'this.button1' has ordinal index of 2 but sets a different TabIndex of 1
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(36, 13, 36, 45).WithSpan(34, 13, 34, 47).WithSpan(35, 13, 35, 40).WithSpan(36, 13, 36, 45).WithSpan(37, 13, 37, 45).WithArguments("this.button1", "2", "1"),
                    // /0/Test0.cs(37,13): warning WF1002: Control 'this.button2' has ordinal index of 3 but sets a different TabIndex of 2
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(37, 13, 37, 45).WithSpan(34, 13, 34, 47).WithSpan(35, 13, 35, 40).WithSpan(36, 13, 36, 45).WithSpan(37, 13, 37, 45).WithArguments("this.button2", "3", "2")
                };

                var test = new VerifyCS.Test()
                {
                    TestCode = code,
                    CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
                    FixedState =
                    {
                        Sources = { fixedCode },
                    }
                };
                test.ExpectedDiagnostics.Add(
                    // /0/Test0.cs(37,13): warning WF1002: Control 'this.treeView1' has ordinal index of 3 but sets a different TabIndex of 0
                    VerifyCS.Diagnostic(ControlTabOrderAnalyzer.InconsistentTabIndexRuleIdDescriptor).WithSpan(37, 13, 37, 47).WithSpan(34, 13, 34, 40).WithSpan(35, 13, 35, 45).WithSpan(36, 13, 36, 45).WithSpan(37, 13, 37, 47).WithArguments("this.treeView1", "3", "0")
                    );
                test.FixedState.ExpectedDiagnostics.AddRange(expected);

                await test.RunAsync();
            }
        }
    }
}
