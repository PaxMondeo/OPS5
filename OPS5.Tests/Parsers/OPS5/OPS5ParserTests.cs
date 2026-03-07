using FluentAssertions;
using NSubstitute;
using OPS5.Engine.Contracts;
using OPS5.Engine.Parsers.OPS5;
using Xunit;

namespace OPS5.Engine.Tests.Parsers.OPS5
{
    public class OPS5ParserTests
    {
        private readonly IOPS5Logger _logger = Substitute.For<IOPS5Logger>();

        private OPS5ParseResult Parse(string input)
        {
            var parser = new OPS5Parser(_logger);
            return parser.Parse(input, "test.ops5");
        }

        // ==================== Literalize ====================

        [Fact]
        public void Parse_Literalize_CreatesClassModel()
        {
            var result = Parse("(literalize block name color size)");
            result.Classes.Classes.Should().HaveCount(1);
            var cls = result.Classes.Classes[0];
            cls.ClassName.Should().Be("block");
            cls.Atoms.Should().BeEquivalentTo(new[] { "name", "color", "size" });
            cls.IsBase.Should().BeTrue();
        }

        [Fact]
        public void Parse_MultipleLiteralizes_CreatesMultipleClasses()
        {
            var result = Parse(
                "(literalize block name color)\n" +
                "(literalize goal type status)");
            result.Classes.Classes.Should().HaveCount(2);
            result.Classes.Classes[0].ClassName.Should().Be("block");
            result.Classes.Classes[1].ClassName.Should().Be("goal");
        }

        [Fact]
        public void Parse_Literalize_WithHyphenatedName()
        {
            var result = Parse("(literalize on-top-of upper lower)");
            result.Classes.Classes[0].ClassName.Should().Be("on-top-of");
            result.Classes.Classes[0].Atoms.Should().BeEquivalentTo(new[] { "upper", "lower" });
        }

        // ==================== Make ====================

        [Fact]
        public void Parse_Make_CreatesDataAction()
        {
            var result = Parse("(make block ^name A ^color red)");
            result.Data.Actions.Should().HaveCount(1);
            var action = result.Data.Actions[0];
            action.Command.Should().Be("MAKE");
            action.Atoms.Should().Contain("block");
            action.Atoms.Should().Contain("name");
            action.Atoms.Should().Contain("A");
            action.Atoms.Should().Contain("color");
            action.Atoms.Should().Contain("red");
        }

        [Fact]
        public void Parse_Make_WithNumericValue()
        {
            var result = Parse("(make block ^name A ^size 10)");
            var action = result.Data.Actions[0];
            action.Atoms.Should().Contain("10");
        }

        [Fact]
        public void Parse_Make_WithPipeString()
        {
            var result = Parse("(make block ^name |Block A|)");
            var action = result.Data.Actions[0];
            action.Atoms.Should().Contain("Block A");
        }

        // ==================== Productions - Basic ====================

        [Fact]
        public void Parse_SimpleProduction_CreatesRuleModel()
        {
            var result = Parse(
                "(literalize block name color)\n" +
                "(p test-rule\n" +
                "  (block ^name A)\n" +
                "  -->\n" +
                "  (write |found A|))");
            result.Rules.Rules.Should().HaveCount(1);
            var rule = result.Rules.Rules[0];
            rule.RuleName.Should().Be("test-rule");
            rule.Conditions.Should().HaveCount(1);
            rule.Actions.Should().HaveCount(1);
        }

        [Fact]
        public void Parse_Condition_HasClassTest()
        {
            var result = Parse(
                "(p test-rule (block ^name A) --> (halt))");
            var cond = result.Rules.Rules[0].Conditions[0];
            cond.ClassName.Should().Be("block");
            cond.Tests[0].Attribute.Should().Be("CLASS");
            cond.Tests[0].Value.Should().Be("block");
        }

        [Fact]
        public void Parse_Condition_LiteralValueTest()
        {
            var result = Parse(
                "(p test-rule (block ^name A) --> (halt))");
            var cond = result.Rules.Rules[0].Conditions[0];
            cond.Tests.Should().HaveCount(2); // CLASS + name
            cond.Tests[1].Attribute.Should().Be("NAME");
            cond.Tests[1].Operator.Should().Be("=");
            cond.Tests[1].Value.Should().Be("A");
        }

        [Fact]
        public void Parse_Condition_VariableBinding()
        {
            var result = Parse(
                "(p my-rule (block ^name <x>) --> (halt))");
            var cond = result.Rules.Rules[0].Conditions[0];
            cond.Tests[1].Attribute.Should().Be("NAME");
            cond.Tests[1].Operator.Should().Be("=");
            cond.Tests[1].Value.Should().Be("<X.MY-RULE>");
        }

        [Fact]
        public void Parse_Condition_PredicateOperator()
        {
            var result = Parse(
                "(p my-rule (block ^size > 5) --> (halt))");
            var cond = result.Rules.Rules[0].Conditions[0];
            cond.Tests[1].Attribute.Should().Be("SIZE");
            cond.Tests[1].Operator.Should().Be(">");
            cond.Tests[1].Value.Should().Be("5");
        }

        // ==================== Negated Conditions ====================

        [Fact]
        public void Parse_NegatedCondition()
        {
            var result = Parse(
                "(p my-rule -(block ^name done) --> (halt))");
            var cond = result.Rules.Rules[0].Conditions[0];
            cond.Negative.Should().BeTrue();
            cond.ClassName.Should().Be("block");
        }

        // ==================== Condition Aliases ====================

        [Fact]
        public void Parse_DuplicateClassName_GeneratesAlias()
        {
            var result = Parse(
                "(p my-rule (block ^name A) (block ^name B) --> (halt))");
            var rule = result.Rules.Rules[0];
            rule.Conditions.Should().HaveCount(2);
            rule.Conditions[0].Alias.Should().BeNull();
            rule.Conditions[1].Alias.Should().Be("block_2");
        }

        // ==================== Conjunctions ====================

        [Fact]
        public void Parse_Conjunction_MultipleTests()
        {
            var result = Parse(
                "(p my-rule (block ^size { > 5 < 10 }) --> (halt))");
            var cond = result.Rules.Rules[0].Conditions[0];
            // CLASS + two conjunction tests
            cond.Tests.Should().HaveCount(3);
            cond.Tests[1].Attribute.Should().Be("SIZE");
            cond.Tests[1].Operator.Should().Be(">");
            cond.Tests[1].Value.Should().Be("5");
            cond.Tests[2].Attribute.Should().Be("SIZE");
            cond.Tests[2].Operator.Should().Be("<");
            cond.Tests[2].Value.Should().Be("10");
        }

        // ==================== Disjunctions ====================

        [Fact]
        public void Parse_Disjunction_StoresValues()
        {
            var result = Parse(
                "(p my-rule (block ^color << red green blue >>) --> (halt))");
            var cond = result.Rules.Rules[0].Conditions[0];
            cond.Tests[1].Attribute.Should().Be("COLOR");
            cond.Tests[1].Value.Should().Be("<<red green blue>>");
        }

        // ==================== RHS Make ====================

        [Fact]
        public void Parse_RHSMake_CreatesActionWithClassName()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (make result ^op add ^val 15))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("MAKE");
            action.ClassName.Should().Be("RESULT");
        }

        // ==================== RHS Modify ====================

        [Fact]
        public void Parse_RHSModify_ResolvesConditionRef()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (modify 1 ^name B))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("MODIFY");
            action.ClassName.Should().Be("block");
        }

        // ==================== RHS Remove ====================

        [Fact]
        public void Parse_RHSRemove_ResolvesConditionRef()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (remove 1))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("REMOVE");
            action.ClassName.Should().Be("block");
            action.Atoms.Should().Contain("REMOVE");
        }

        // ==================== RHS Write ====================

        [Fact]
        public void Parse_RHSWrite_StringLiteral()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (write |hello world|))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("WRITE");
            action.Atoms.Should().Contain("hello world");
        }

        [Fact]
        public void Parse_RHSWrite_WithVariable()
        {
            var result = Parse(
                "(p my-rule (block ^name <x>) --> (write <x>))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Atoms.Should().Contain("<X.MY-RULE>");
        }

        [Fact]
        public void Parse_RHSWrite_WithLogicalName()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (write outfile |hello|))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Atoms.Should().Contain("TO");
            action.Atoms.Should().Contain("outfile");
        }

        [Fact]
        public void Parse_RHSWrite_CrlfIgnored()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (write |hello| (crlf)))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Atoms.Should().Contain("hello");
        }

        // ==================== RHS Halt ====================

        [Fact]
        public void Parse_RHSHalt()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (halt))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("HALT");
        }

        // ==================== RHS Compute ====================

        [Fact]
        public void Parse_RHSCompute_SimpleAddition()
        {
            var result = Parse(
                "(p add-rule (block ^val <a>) -->\n" +
                "  (compute <result> (+ <a> 5)))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("SET");
            action.Atoms[1].Should().Be("<RESULT.ADD-RULE>");
            action.Atoms[3].Should().Be("CALC");
            action.Atoms[4].Should().Contain("<A.ADD-RULE>");
            action.Atoms[4].Should().Contain("+");
        }

        [Fact]
        public void Parse_RHSCompute_NestedExpression()
        {
            var result = Parse(
                "(p my-rule (block ^val <a>) -->\n" +
                "  (compute <x> (+ (* <a> 2) 3)))");
            var action = result.Rules.Rules[0].Actions[0];
            // Postfix: <a> 2 * 3 +
            action.Atoms[4].Should().Contain("*");
            action.Atoms[4].Should().Contain("+");
        }

        // ==================== RHS Bind ====================

        [Fact]
        public void Parse_RHSBind_SimpleValue()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (bind <x> 42))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("SET");
            action.Atoms[1].Should().Be("<X.MY-RULE>");
            action.Atoms[3].Should().Be("42");
        }

        [Fact]
        public void Parse_RHSBind_Genatom()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (bind <x> (genatom)))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("SET");
            action.Atoms[3].Should().Be("Genatom");
        }

        [Fact]
        public void Parse_RHSBind_Accept()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (bind <x> (accept)))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("ACCEPT");
            action.Atoms[1].Should().Be("<X.MY-RULE>");
        }

        [Fact]
        public void Parse_RHSBind_AcceptLine()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (bind <x> (acceptline)))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("ACCEPTLINE");
        }

        [Fact]
        public void Parse_RHSBind_AcceptFromFile()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (bind <x> (accept infile)))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("ACCEPT");
            action.Atoms.Should().Contain("FROM");
            action.Atoms.Should().Contain("infile");
        }

        [Fact]
        public void Parse_RHSBind_Substr()
        {
            var result = Parse(
                "(p my-rule (block ^name <x>) --> (bind <y> (substr <x> 1 3)))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("SET");
            action.Atoms[3].Should().Be("SUBSTR");
        }

        [Fact]
        public void Parse_RHSBind_Calc()
        {
            var result = Parse(
                "(p my-rule (block ^val <a>) --> (bind <x> (+ <a> 1)))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("SET");
            action.Atoms[3].Should().Be("CALC");
        }

        // ==================== RHS OpenFile/CloseFile ====================

        [Fact]
        public void Parse_RHSOpenFile()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (openfile outfile |test.txt| out))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("OPENFILE");
            action.Atoms[1].Should().Be("outfile");
            action.Atoms[2].Should().Be("test.txt");
            action.Atoms[3].Should().Be("Out");
        }

        [Fact]
        public void Parse_RHSCloseFile()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (closefile outfile))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("CLOSEFILE");
            action.Atoms[1].Should().Be("outfile");
        }

        // ==================== RHS Call ====================

        [Fact]
        public void Parse_RHSCall()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (call myprog arg1 arg2))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Command.Should().Be("EXECUTE");
            action.Atoms.Should().Contain("myprog");
        }

        // ==================== RHS TabTo ====================

        [Fact]
        public void Parse_RHSWrite_TabTo()
        {
            var result = Parse(
                "(p my-rule (block ^name A) --> (write (tabto 10) |hello|))");
            var action = result.Rules.Rules[0].Actions[0];
            action.Atoms.Should().Contain("TABTO");
            action.Atoms.Should().Contain("10");
        }

        // ==================== Multiple Conditions ====================

        [Fact]
        public void Parse_MultipleConditions_CorrectOrder()
        {
            var result = Parse(
                "(p my-rule (block ^name A) (goal ^type find) --> (halt))");
            var rule = result.Rules.Rules[0];
            rule.Conditions.Should().HaveCount(2);
            rule.Conditions[0].Order.Should().Be(1);
            rule.Conditions[0].ClassName.Should().Be("block");
            rule.Conditions[1].Order.Should().Be(2);
            rule.Conditions[1].ClassName.Should().Be("goal");
        }

        // ==================== Multiple Actions ====================

        [Fact]
        public void Parse_MultipleActions()
        {
            var result = Parse(
                "(p my-rule (block ^name A) -->\n" +
                "  (modify 1 ^name B)\n" +
                "  (write |modified|))");
            var rule = result.Rules.Rules[0];
            rule.Actions.Should().HaveCount(2);
            rule.Actions[0].Command.Should().Be("MODIFY");
            rule.Actions[1].Command.Should().Be("WRITE");
        }

        // ==================== Comments ====================

        [Fact]
        public void Parse_CommentsSkipped()
        {
            var result = Parse(
                "; this is a comment\n" +
                "(literalize block name)\n" +
                "; another comment\n" +
                "(make block ^name A)");
            result.Classes.Classes.Should().HaveCount(1);
            result.Data.Actions.Should().HaveCount(1);
        }

        // ==================== Error Handling ====================

        [Fact]
        public void Parse_UnknownTopLevelForm_LogsError()
        {
            var result = Parse("(unknown-form arg1 arg2)");
            _logger.Received().WriteError(
                Arg.Is<string>(s => s.Contains("Unknown top-level form")),
                Arg.Any<string>());
        }

        [Fact]
        public void Parse_UnsupportedAction_LogsError()
        {
            Parse("(p my-rule (block ^name A) --> (cbind <x>))");
            _logger.Received().WriteError(
                Arg.Is<string>(s => s.Contains("Unsupported")),
                Arg.Any<string>());
        }

        // ==================== Full Program ====================

        [Fact]
        public void Parse_CompleteProgram_AllPartsPopulated()
        {
            var result = Parse(
                "(literalize block name color)\n" +
                "(literalize goal type status)\n" +
                "(make block ^name A ^color red)\n" +
                "(make goal ^type find ^status active)\n" +
                "(p find-block\n" +
                "  (goal ^type find ^status active)\n" +
                "  (block ^name <x> ^color red)\n" +
                "  -->\n" +
                "  (write |Found:| <x> (crlf))\n" +
                "  (modify 1 ^status done))");

            result.Classes.Classes.Should().HaveCount(2);
            result.Data.Actions.Should().HaveCount(2);
            result.Rules.Rules.Should().HaveCount(1);

            var rule = result.Rules.Rules[0];
            rule.RuleName.Should().Be("find-block");
            rule.Conditions.Should().HaveCount(2);
            rule.Actions.Should().HaveCount(2);
        }
    }
}
