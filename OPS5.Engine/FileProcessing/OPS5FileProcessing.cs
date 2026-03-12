using AttributeLibrary;
using OPS5.Engine.Contracts;
using OPS5.Engine.Contracts.Parser;
using OPS5.Engine.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OPS5.Engine.FileProcessing
{
    internal class OPS5FileProcessing : IFileProcessing
    {
        private readonly IOPS5Logger _logger;
        private readonly IConfig _config;
        private readonly IOPS5Parser _ops5Parser;
        private readonly IUtils _parserUtils;
        private readonly IRHSActionFactory _rhsActionFactory;

        private ISourceFiles _sourceFiles;
        private IWMClasses _WMClasses;
        private IRules _rules;
        private IWorkingMemory _workingMemory;
        private IAlphaMemory _alphaMemory;
        private IBetaMemory _betaMemory;
        private IObjectIDs _objectIDs;


        public OPS5FileProcessing(IOPS5Logger logger,
                              IConfig config,
                              IWorkingMemory workingMemory,
                              IWMClasses WMClasses,
                              IAlphaMemory alphaMemory,
                              IBetaMemory betaMemory,
                              IRules rules,
                              IOPS5Parser ops5Parser,
                              IUtils parserUtils,
                              ISourceFiles sourceFiles,
                              IObjectIDs objectIDs,
                              IRHSActionFactory rhsActionFactory)
        {
            _logger = logger;
            _config = config;
            _workingMemory = workingMemory;
            _WMClasses = WMClasses;
            _alphaMemory = alphaMemory;
            _betaMemory = betaMemory;
            _rules = rules;
            _ops5Parser = ops5Parser;
            _parserUtils = parserUtils;
            _sourceFiles = sourceFiles;
            _objectIDs = objectIDs;
            _rhsActionFactory = rhsActionFactory;
        }


        /// <summary>
        /// Processes the named source file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<bool> ProcessFile(string fileName)
        {
            _logger.WriteInfo($"Reading file {_config.ClientAppPath}{fileName}", 0);
            string uFileName = fileName.ToUpper();
            SortFileNames(fileName, _config.ClientAppPath, true);

            try
            {
                string file = File.ReadAllText(_config.ClientAppPath + fileName);

                if (uFileName.EndsWith(".OPS5"))
                {
                    ProcessOPS5File(file, fileName);
                }

            }
            catch (Exception ex)
            {
                _logger.WriteError($"Error while processing {_config.ClientAppPath + fileName} mapped as {uFileName} {ex.Message}", "ProcessFile");
            }
            return _logger.ErrorCount == 0;
        }

        private void SortFileNames(string fileName, string filePath, bool loaded)
        {
            string uFileName = fileName.ToUpper();

            if (uFileName.EndsWith(".OPS5") && loaded)
            {
                _sourceFiles.OPS5File.FileName = fileName;
                _sourceFiles.OPS5File.FilePath = filePath;
                _sourceFiles.OPS5File.Comment = "";
                _sourceFiles.OPS5File.Loaded = true;
                _sourceFiles.OPS5File.Saved = true;
            }
        }

        private void ProcessOPS5File(string file, string fileName)
        {
            _config.Ops5 = true;
            var result = _ops5Parser.Parse(file, fileName);

            if (result.Classes.Classes.Count > 0)
                ProcessClassModels(result.Classes, fileName);

            if (result.Defaults.Count > 0)
                ProcessDefaults(result.Defaults);

            if (result.VectorAttributes.Count > 0)
                ProcessVectorAttributes(result.VectorAttributes);

            if (result.Rules.Rules.Count > 0)
                ProcessRuleModels(result.Rules, fileName);

            if (result.Data.Actions.Count > 0)
                ProcessDataModels(result.Data, fileName);

            _logger.WriteInfo($"Completed OPS5 file {fileName}", 0);
        }

        private void ProcessClassModels(ClassFileModel classFileModel, string fileName)
        {
            foreach (ClassModel classModel in classFileModel.Classes)
            {
                IWMClass theClass = LiteraliseClass(classModel.ClassName, classModel.Atoms, fileName);
                if (classModel.Disabled)
                    theClass.Enabled = false;
                theClass.Comment = classModel.Comment;
            }
            _logger.WriteInfo($"Completed classes in {fileName}", 0);
        }

        private void ProcessRuleModels(RuleFileModel ruleFileModel, string fileName)
        {
            try
            {
                foreach (RuleModel ruleModel in ruleFileModel.Rules)
                {
                    IRule prod = _rules.AddRule(ruleModel);
                    prod.Comment = ruleModel.Comment;
                    IBetaNode beta = _workingMemory.BetaRoot;
                    foreach (ConditionModel cond in ruleModel.Conditions)
                    {
                        if (_WMClasses.ClassExists(cond.ClassName))
                        {
                            Condition newCondition = new Condition(cond.Order, cond.ClassName, cond.Line, cond.Negative);
                            foreach (ConditionTest test in cond.Tests)
                            {
                                if (!_WMClasses.GetClass(cond.ClassName).AttributeExists(test.Attribute))
                                {
                                    throw new Exception($"\n\nERROR - Class {newCondition.ClassName} does not contain Attribute {test.Attribute} in {cond.Line}.\n\n");
                                }
                                newCondition.Tests.Add(test);
                            }
                            newCondition.IsAny = cond.IsAny;
                            newCondition.Alias = cond.Alias;
                            prod.AddCondition(newCondition);
                            if (prod.Enabled)
                            {
                                beta = SetUpNetwork(prod, newCondition, beta, cond.Negative);
                            }
                        }
                        else
                        {
                            _logger.WriteError($"\n\nERROR - Class {cond.ClassName} referenced in {prod.Name} has not been declared.", "File Processor");
                        }
                    }

                    foreach (ActionModel action in ruleModel.Actions)
                    {
                        if (action.ClassName != "" && !_WMClasses.ClassExists(action.ClassName))
                            _logger.WriteError($"Attempted to {action.Command} object of non-existent Class {action.ClassName} in line {action.Line} of Rule {ruleModel.RuleName}", "File Processor");
                        else
                        {
                            IRHSAction rhsAction = default!;
                            switch (action.Command)
                            {
                                case "MAKE":
                                case "MODIFY":
                                    rhsAction = _rhsActionFactory.NewRHSAction(action.Line, action.Actions); //Actions are objects
                                    break;

                                case "REMOVE":
                                case "WRITE":
                                case "HALT":
                                case "SET":
                                case "OPENFILE":
                                case "CLOSEFILE":
                                case "ACCEPT":
                                case "ACCEPTLINE":
                                case "CBIND":
                                case "CALL":
                                    rhsAction = _rhsActionFactory.NewRHSAction(action.Line, action.Atoms); //Atoms are strings
                                    break;
                            }
                            if (rhsAction != null)
                                prod.AddAction(rhsAction);

                        }
                    }

                    if (prod.Enabled)
                        prod.PNode = beta;
                    _logger.WriteInfo($"Set Beta Node {beta.ID} as P Node for Rule {prod.Name}", 2);

                }
            }
            catch (Exception ex)
            {
                _logger.WriteError($"{ex.Message} processing Rule File {fileName}", "Rules Processor");
            }
            _logger.WriteInfo($"Completed file {fileName}", 0);
        }

        private void ProcessDefaults(List<DefaultModel> defaults)
        {
            foreach (var def in defaults)
            {
                if (_WMClasses.ClassExists(def.ClassName))
                    _WMClasses.GetClass(def.ClassName).SetDefaults(def.Defaults);
                else
                    _logger.WriteError($"Default declaration references non-existent class '{def.ClassName}'", "File Processor");
            }
        }

        private void ProcessVectorAttributes(List<VectorAttributeModel> vectorAttributes)
        {
            foreach (var va in vectorAttributes)
            {
                if (_WMClasses.ClassExists(va.ClassName))
                {
                    var wmClass = _WMClasses.GetClass(va.ClassName);
                    foreach (string attr in va.Attributes)
                        wmClass.SetAttributeType(attr, "VECTOR");
                }
                else
                    _logger.WriteError($"Vector-attribute declaration references non-existent class '{va.ClassName}'", "File Processor");
            }
        }

        private void ProcessDataModels(DataFileModel dataFileModel, string fileName)
        {
            foreach (DataActionModel action in dataFileModel.Actions)
            {
                if (action.Command.Contains("MAKE"))
                    Make(action.Atoms, fileName);
            }
            _logger.WriteInfo($"Completed file {fileName}", 0);
        }

        /// <summary>
        /// Adds a new class to the Engine, with attributes
        /// </summary>
        /// <param name="className"></param>
        /// <param name="attributes"></param>
        /// <returns></returns>
        private IWMClass LiteraliseClass(string className, List<string> attributes, string classFile)
        {
            IWMClass newClass = _WMClasses.Add(className, attributes);
            newClass.ClassFile = classFile;
            return newClass;
        }


        private IBetaNode SetUpNetwork(IRule rule, Condition condition, IBetaNode betanode, bool negative)
        {
            IAlphaNode alphaNode = _workingMemory.AlphaRoot;
            if (!negative)
                rule.ObjectCount++; //increment expected position in token to bind value from object if not negative

            //Set up Alpha network
            foreach (ConditionTest test in condition.Tests)
            {
                string val = test.Value.ToUpper();
                if (val.StartsWith("<<") || !(val.StartsWith("<") || val.StartsWith("CALC ") || val.StartsWith("POP ") || val.StartsWith("PEEK ")))
                {
                    //Is a Disjunction
                    //Therefore create an Alpha node
                    alphaNode = _alphaMemory.BuildShareAlpha(alphaNode, test);
                }
            }

            //Set up Beta network
            List<ConditionTest> newTests = new List<ConditionTest>();
            foreach (ConditionTest test in condition.Tests)
            {
                if (test.Value.StartsWith("<")) //Check that it is variable binding
                {
                    if (test.Operator == "=")
                    {
                        if (test.Concatenation)
                        {
                            //It is a test against a concatenation
                            newTests.Add(test);
                        }
                        else
                        {
                            if (rule.Bindings.ContainsKey(test.Value))
                            {
                                //Already have binding, so this is a test
                                newTests.Add(test);
                            }
                            else
                            {
                                //Haven't encountered this binding before, so add it to bindings
                                if (test.Attribute.ToUpper().StartsWith("CALC "))
                                {
                                    List<string> calc = _parserUtils.ParseCommand(test.Attribute);
                                    Binding newBinding = new Binding(calc, "CALC");
                                    rule.Bindings.Add(test.Value.ToUpper(), newBinding);
                                }
                                else if (test.Attribute.ToUpper().StartsWith("POP ") || test.Attribute.ToUpper().StartsWith("PEEK "))
                                {
                                    Binding newBinding = new Binding(test.Attribute);
                                    rule.Bindings.Add(test.Value.ToUpper(), newBinding);
                                }
                                else
                                {
                                    List<string> atoms = _parserUtils.ParseCommand(test.Attribute);
                                    if (atoms.Count == 1)
                                    {
                                        Binding newBinding = new Binding(rule.ObjectCount, test.Attribute);
                                        rule.Bindings.Add(test.Value, newBinding);
                                    }
                                    else
                                    {
                                        Binding newBinding = new Binding(test.Attribute);
                                        rule.Bindings.Add(test.Value.ToUpper(), newBinding);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        newTests.Add(test);
                    }
                }
            }
            IBetaNode betaNode = _betaMemory.BuildShareBeta(betanode, alphaNode, newTests, rule.Bindings, negative, condition.IsAny);
            //Then set current Beta
            return betaNode;
        }


        public void Make(List<string> atoms)
        {
            Make(atoms, "");
        }

        private void Make(List<string> atoms, string fileName)
        {
            if (atoms.Count > 2)
            {
                string className = atoms[1];
                atoms.RemoveRange(0, 2);
                _workingMemory.AddObject(className, atoms.ToArray());
            }
            else
                _logger.WriteError("Invalid Syntax for MAKE statement", fileName);
        }

    }
}
