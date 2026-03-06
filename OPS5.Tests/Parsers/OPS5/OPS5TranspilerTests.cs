using FluentAssertions;
using OPS5.Engine.Contracts;
using OPS5.Engine.Parsers.OPS5;
using NSubstitute;
using Xunit;

namespace OPS5.Engine.Tests.Parsers.OPS5
{
    public class OPS5TranspilerTests
    {
        private readonly IOPS5Logger _logger = Substitute.For<IOPS5Logger>();

        private OPS5TranspileResult Transpile(string input)
        {
            var transpiler = new OPS5Transpiler(_logger);
            return transpiler.Transpile(input, "test.ops5");
        }

        // ==================== Literalize → Class ====================

        [Fact]
        public void Literalize_SimpleClass_GeneratesClassSyntax()
        {
            var result = Transpile("(literalize block name color mass)");
            result.ClassesText.Trim().Should().Be("Class block (name, color, mass);");
        }

        [Fact]
        public void Literalize_SingleAttribute_GeneratesClassSyntax()
        {
            var result = Transpile("(literalize goal status)");
            result.ClassesText.Trim().Should().Be("Class goal (status);");
        }

        [Fact]
        public void Literalize_HyphenatedAttributes()
        {
            var result = Transpile("(literalize block name on-top-of)");
            result.ClassesText.Trim().Should().Be("Class block (name, on-top-of);");
        }

        [Fact]
        public void Literalize_MultipleClasses()
        {
            string input = @"
                (literalize block name color)
                (literalize goal status object)";
            var result = Transpile(input);
            result.ClassesText.Should().Contain("Class block (name, color);");
            result.ClassesText.Should().Contain("Class goal (status, object);");
        }

        // ==================== Make → Data ====================

        [Fact]
        public void Make_SimpleObject_GeneratesDataSyntax()
        {
            var result = Transpile("(make block ^name B1 ^color red)");
            result.DataText.Trim().Should().Be("Make block (name B1, color red);");
        }

        [Fact]
        public void Make_NumericValue()
        {
            var result = Transpile("(make block ^name B1 ^mass 500)");
            result.DataText.Trim().Should().Be("Make block (name B1, mass 500);");
        }

        [Fact]
        public void Make_MultipleObjects()
        {
            string input = @"
                (make block ^name B1 ^color red)
                (make block ^name B2 ^color blue)";
            var result = Transpile(input);
            result.DataText.Should().Contain("Make block (name B1, color red);");
            result.DataText.Should().Contain("Make block (name B2, color blue);");
        }

        // ==================== Production → Rule ====================

        [Fact]
        public void Production_SimpleRule_GeneratesRuleSyntax()
        {
            string input = @"(p find-red
                (block ^name <x> ^color red)
                -->
                (write <x>))";
            var result = Transpile(input);

            result.RulesText.Should().Contain("Rule find-red");
            result.RulesText.Should().Contain("block (name <x>, color = red);");
            result.RulesText.Should().Contain("-->");
            result.RulesText.Should().Contain("Write (<x>);");
            result.RulesText.Should().Contain(");");
        }

        [Fact]
        public void Production_NegatedCondition()
        {
            string input = @"(p no-top
                (block ^name <b>)
                -(block ^on-top-of <b>)
                -->
                (halt))";
            var result = Transpile(input);

            result.RulesText.Should().Contain("Not block (on-top-of <b>);");
        }

        [Fact]
        public void Production_PredicateCondition()
        {
            string input = @"(p heavy
                (block ^mass > 500)
                -->
                (halt))";
            var result = Transpile(input);

            result.RulesText.Should().Contain("mass > 500");
        }

        [Fact]
        public void Production_MultipleConditions()
        {
            string input = @"(p match-colors
                (block ^name <b1> ^color <c>)
                (block ^name <b2> ^color <c>)
                -->
                (halt))";
            var result = Transpile(input);

            result.RulesText.Should().Contain("block (name <b1>, color <c>);");
            result.RulesText.Should().Contain("block (name <b2>, color <c>);");
        }

        // ==================== RHS Actions ====================

        [Fact]
        public void RHS_Make_GeneratesMakeAction()
        {
            string input = @"(p make-goal
                (block ^name <b>)
                -->
                (make goal ^status active ^object <b>))";
            var result = Transpile(input);

            result.RulesText.Should().Contain("Make goal (status active, object <b>);");
        }

        [Fact]
        public void RHS_Modify_GeneratesModifyAction()
        {
            string input = @"(p update-color
                (block ^name <b> ^color red)
                -->
                (modify 1 ^color blue))";
            var result = Transpile(input);

            result.RulesText.Should().Contain("Modify 1 (color blue);");
        }

        [Fact]
        public void RHS_Remove_GeneratesRemoveAction()
        {
            string input = @"(p cleanup
                (goal ^status done)
                -->
                (remove 1))";
            var result = Transpile(input);

            result.RulesText.Should().Contain("Remove 1;");
        }

        [Fact]
        public void RHS_Write_GeneratesWriteAction()
        {
            string input = @"(p report
                (block ^name <b>)
                -->
                (write |Block: | <b>))";
            var result = Transpile(input);

            result.RulesText.Should().Contain("Write (\"Block: \" <b>);");
        }

        [Fact]
        public void RHS_Halt_GeneratesHaltAction()
        {
            string input = @"(p stop
                (status ^complete true)
                -->
                (halt))";
            var result = Transpile(input);

            result.RulesText.Should().Contain("Halt;");
        }

        [Fact]
        public void RHS_Compute_GeneratesCalcExpression()
        {
            string input = @"(p calc-volume
                (block ^length <l> ^width <w>)
                -->
                (compute <area> (* <l> <w>)))";
            var result = Transpile(input);

            result.RulesText.Should().Contain("Set <area> = Calc(<l> <w> *);");
        }

        [Fact]
        public void RHS_Compute_NestedExpression()
        {
            string input = @"(p calc-volume
                (block ^length <l> ^width <w> ^height <h>)
                -->
                (compute <volume> (* <l> (* <w> <h>))))";
            var result = Transpile(input);

            // (* <l> (* <w> <h>)) → <l> <w> <h> * *
            result.RulesText.Should().Contain("Set <volume> = Calc(<l> <w> <h> * *);");
        }

        // ==================== Conjunctions and Disjunctions ====================

        [Fact]
        public void Condition_Conjunction()
        {
            string input = @"(p range-check
                (block ^mass { > 100 < 500 })
                -->
                (halt))";
            var result = Transpile(input);

            result.RulesText.Should().Contain("mass > 100");
            result.RulesText.Should().Contain("mass < 500");
        }

        [Fact]
        public void Condition_Disjunction()
        {
            string input = @"(p color-check
                (block ^color << red blue green >>)
                -->
                (halt))";
            var result = Transpile(input);

            result.RulesText.Should().Contain("color << red blue green >>");
        }

        // ==================== Comments ====================

        [Fact]
        public void Comments_AreIgnored()
        {
            string input = @"; This is a comment
                (literalize block name)
                ; Another comment
                (make block ^name B1)";
            var result = Transpile(input);

            result.ClassesText.Should().Contain("Class block");
            result.DataText.Should().Contain("Make block");
        }

        // ==================== Combined File ====================

        [Fact]
        public void FullProgram_ProducesAllThreeOutputs()
        {
            string input = @"
                (literalize block name color)
                (literalize goal status object)
                (make block ^name B1 ^color red)
                (make block ^name B2 ^color blue)
                (p find-red
                    (block ^name <b> ^color red)
                    -->
                    (make goal ^status found ^object <b>))";
            var result = Transpile(input);

            result.ClassesText.Should().NotBeNullOrWhiteSpace();
            result.DataText.Should().NotBeNullOrWhiteSpace();
            result.RulesText.Should().NotBeNullOrWhiteSpace();
            result.Diagnostics.Should().BeEmpty();

            result.ClassesText.Should().Contain("Class block");
            result.ClassesText.Should().Contain("Class goal");
            result.DataText.Should().Contain("Make block (name B1");
            result.DataText.Should().Contain("Make block (name B2");
            result.RulesText.Should().Contain("Rule find-red");
        }

        // ==================== Call → Execute ====================

        [Fact]
        public void RHS_Call_GeneratesExecuteAction()
        {
            string input = @"(p test-call
                (block ^name <b>)
                -->
                (call external-func <b>))";
            var result = Transpile(input);

            result.Diagnostics.Should().BeEmpty();
            result.RulesText.Should().Contain("Execute external-func <b>;");
        }

        [Fact]
        public void RHS_Call_NoArgs_GeneratesExecute()
        {
            string input = @"(p test-call
                (block ^name <b>)
                -->
                (call run-cleanup))";
            var result = Transpile(input);

            result.RulesText.Should().Contain("Execute run-cleanup;");
        }

        // ==================== Accept / AcceptLine ====================

        [Fact]
        public void RHS_BindAccept_GeneratesAcceptAction()
        {
            string input = @"(p get-input
                (goal ^status waiting)
                -->
                (bind <x> (accept)))";
            var result = Transpile(input);

            result.Diagnostics.Should().BeEmpty();
            result.RulesText.Should().Contain("Accept <x>;");
        }

        [Fact]
        public void RHS_BindAcceptLine_GeneratesAcceptLineAction()
        {
            string input = @"(p get-line
                (goal ^status waiting)
                -->
                (bind <x> (acceptline)))";
            var result = Transpile(input);

            result.Diagnostics.Should().BeEmpty();
            result.RulesText.Should().Contain("AcceptLine <x>;");
        }

        [Fact]
        public void RHS_BindAccept_CaseInsensitive()
        {
            string input = @"(p get-input
                (goal ^status waiting)
                -->
                (bind <x> (ACCEPT)))";
            var result = Transpile(input);

            result.Diagnostics.Should().BeEmpty();
            result.RulesText.Should().Contain("Accept <x>;");
        }

        [Fact]
        public void RHS_StandaloneAccept_LogsDiagnostic()
        {
            string input = @"(p test-accept
                (goal ^status waiting)
                -->
                (accept))";
            var result = Transpile(input);

            result.Diagnostics.Should().Contain(d => d.Contains("accept") && d.Contains("without (bind)"));
        }

        // ==================== OpenFile / CloseFile ====================

        [Fact]
        public void RHS_OpenFile_Out_GeneratesOpenFile()
        {
            string input = @"(p test-openfile
                (goal ^status ready)
                -->
                (openfile myfile |results.txt| out))";
            var result = Transpile(input);

            result.Diagnostics.Should().BeEmpty();
            result.RulesText.Should().Contain("OpenFile myfile \"results.txt\" Out;");
        }

        [Fact]
        public void RHS_OpenFile_In_GeneratesOpenFile()
        {
            string input = @"(p test-openfile
                (goal ^status ready)
                -->
                (openfile myfile |data.txt| in))";
            var result = Transpile(input);

            result.Diagnostics.Should().BeEmpty();
            result.RulesText.Should().Contain("OpenFile myfile \"data.txt\" In;");
        }

        [Fact]
        public void RHS_CloseFile_GeneratesCloseFile()
        {
            string input = @"(p test-closefile
                (goal ^status done)
                -->
                (closefile myfile))";
            var result = Transpile(input);

            result.Diagnostics.Should().BeEmpty();
            result.RulesText.Should().Contain("CloseFile myfile;");
        }

        [Fact]
        public void RHS_Write_WithLogicalName_GeneratesWriteTo()
        {
            string input = @"(p test-write-file
                (block ^name <b>)
                -->
                (write myfile |Block: | <b>))";
            var result = Transpile(input);

            result.Diagnostics.Should().BeEmpty();
            result.RulesText.Should().Contain("Write (\"Block: \" <b>) To myfile;");
        }

        [Fact]
        public void RHS_BindAccept_WithFile_GeneratesAcceptFrom()
        {
            string input = @"(p test-read-file
                (goal ^status reading)
                -->
                (bind <x> (accept myfile)))";
            var result = Transpile(input);

            result.Diagnostics.Should().BeEmpty();
            result.RulesText.Should().Contain("Accept <x> From myfile;");
        }

        [Fact]
        public void RHS_Write_WithTabTo_EmitsTabTo()
        {
            string input = @"(p test-tabto
                (block ^name <b>)
                -->
                (write |Name:| (tabto 20) <b>))";
            var result = Transpile(input);

            result.Diagnostics.Should().BeEmpty();
            result.RulesText.Should().Contain("Write (\"Name:\" TabTo 20 <b>);");
        }

        [Fact]
        public void RHS_BindSubstr_EmitsSetSubstr()
        {
            string input = @"(p test-substr
                (block ^name <b>)
                -->
                (bind <r> (substr <b> 3 5)))";
            var result = Transpile(input);

            result.Diagnostics.Should().BeEmpty();
            result.RulesText.Should().Contain("Set <r> = Substr(<b> 3 5);");
        }

        [Fact]
        public void RHS_BindSubstr_WithInf_EmitsSetSubstr()
        {
            string input = @"(p test-substr-inf
                (block ^name <b>)
                -->
                (bind <r> (substr <b> 3 inf)))";
            var result = Transpile(input);

            result.Diagnostics.Should().BeEmpty();
            result.RulesText.Should().Contain("Set <r> = Substr(<b> 3 inf);");
        }

        // ==================== Unsupported Features ====================

        [Fact]
        public void UnsupportedAction_LogsDiagnostic()
        {
            string input = @"(p test-cbind
                (block ^name <b>)
                -->
                (cbind <x> <b>))";
            var result = Transpile(input);

            result.Diagnostics.Should().Contain(d => d.Contains("Unsupported") && d.Contains("cbind"));
        }

        [Fact]
        public void UnknownTopLevelForm_LogsDiagnostic()
        {
            string input = "(vector-attribute block sizes)";
            var result = Transpile(input);

            result.Diagnostics.Should().Contain(d => d.Contains("Unknown top-level form"));
        }
    }
}
