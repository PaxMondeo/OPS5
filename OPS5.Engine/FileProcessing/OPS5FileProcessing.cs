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
        private readonly IIOCCParser _ioccParser;
        private readonly IIOCDParser _iocdParser;
        private readonly IIOCRParser _iocrParser;
        private readonly IOPS5Transpiler _ops5Transpiler;
        private readonly IUtils _parserUtils;
        private readonly IRHSActionFactory _rhsActionFactory;

        private ISourceFiles _sourceFiles;
        private IWMClasses _WMClasses;
        private IRules _rules;
        private IWorkingMemory _workingMemory;
        private IAlphaMemory _alphaMemory;
        private IBetaMemory _betaMemory;
        private IClassRelationships _classRelationships;
        private IObjectIDs _objectIDs;


        public OPS5FileProcessing(IOPS5Logger logger,
                              IConfig config,
                              IWorkingMemory workingMemory,
                              IWMClasses WMClasses,
                              IAlphaMemory alphaMemory,
                              IBetaMemory betaMemory,
                              IRules rules,
                              IIOCCParser ioccParser,
                              IIOCDParser iocdParser,
                              IIOCRParser iocrParser,
                              IOPS5Transpiler ops5Transpiler,
                              IUtils parserUtils,
                              ISourceFiles sourceFiles,
                              IObjectIDs objectIDs,
                              IClassRelationships classRelationships,
                              IRHSActionFactory rhsActionFactory)
        {
            _logger = logger;
            _config = config;
            _workingMemory = workingMemory;
            _WMClasses = WMClasses;
            _alphaMemory = alphaMemory;
            _betaMemory = betaMemory;
            _rules = rules;
            _ioccParser = ioccParser;
            _iocdParser = iocdParser;
            _iocrParser = iocrParser;
            _ops5Transpiler = ops5Transpiler;
            _parserUtils = parserUtils;
            _sourceFiles = sourceFiles;
            _objectIDs = objectIDs;
            _classRelationships = classRelationships;
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
            var result = _ops5Transpiler.Transpile(file, fileName);

            foreach (string diag in result.Diagnostics)
                _logger.WriteError(diag, fileName);

            if (!string.IsNullOrWhiteSpace(result.ClassesText))
                ProcessClassFile(result.ClassesText, fileName + ".classes");

            if (!string.IsNullOrWhiteSpace(result.RulesText))
                ProcessRuleFile(result.RulesText, fileName + ".rules");

            if (!string.IsNullOrWhiteSpace(result.DataText))
                ProcessDataFile(result.DataText, fileName + ".data");

            _logger.WriteInfo($"Completed OPS5 file {fileName}", 0);
        }

        private void ProcessClassFile(string file, string fileName)
        {
            IOCCFileModel ioccFileModel = _ioccParser.ParseIOCCFile(file, fileName);

            SetUpRelatedClasses(ioccFileModel.Classes);

            foreach (ClassModel classModel in ioccFileModel.Classes)
            {
                if (!classModel.IsBase)
                {
                    if (_WMClasses.ClassExists(classModel.BaseClass))
                    {
                        foreach (string attr in _WMClasses.GetClass(classModel.BaseClass).GetUserAttributes())
                            classModel.InheritedAtts.Add(attr);
                    }
                    else
                        _logger.WriteError($"Invalid Syntax found. Attempt to inherit from non existent base class {classModel.BaseClass} in {fileName}: {classModel.Line}", "Parser");
                }
                IWMClass theClass;
                if (classModel.IsBase)
                    theClass = LiteraliseClass(classModel.ClassName, classModel.Atoms, fileName);
                else
                {
                    classModel.Atoms = classModel.InheritedAtts.Concat(classModel.Atoms).ToList();
                    theClass = LiteraliseClass(classModel.ClassName, classModel.Atoms, fileName);
                    theClass.BasedOn = classModel.BaseClass;
                }
                if (classModel.Disabled)
                    theClass.Enabled = false;
                theClass.Comment = classModel.Comment;
                theClass.IsBaseClass = classModel.IsBase;
            }
            _logger.WriteInfo($"Completed file {fileName}", 0);
        }

        private void ProcessRuleFile(string file, string fileName)
        {
            try
            {
                IOCRFileModel iocrFileModel = _iocrParser.ParseIOCRFile(file, fileName);

                foreach (RuleModel ruleModel in iocrFileModel.Rules)
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
                                beta = SetUpNetwork(prod, newCondition, beta, cond.Negative, false, null);
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
                        else if (action.ClassName != "" && _WMClasses.GetClass(action.ClassName).ReadOnly)
                            _logger.WriteError($"Attempt to {action.Command} object of Read Only Class {action.ClassName} in line {action.Line} of Rule {ruleModel.RuleName}", "File Processor");
                        else
                        {
                            IRHSAction rhsAction = default!;
                            switch (action.Command)
                            {
                                case "MAKE":
                                case "MAKEMULTIPLE":
                                case "MODIFY":
                                    rhsAction = _rhsActionFactory.NewRHSAction(action.Line, action.Actions); //Actions are objects
                                    break;

                                case "REMOVE":
                                case "REMOVEALL":
                                case "WRITE":
                                case "HALT":
                                case "WAIT":
                                case "SET":
                                    rhsAction = _rhsActionFactory.NewRHSAction(action.Line, action.Atoms); //Atoms are strings
                                    break;
                            }
                            if (rhsAction != null)
                                prod.AddAction(rhsAction);

                        }
                    }

                    if (ruleModel.IsFindPath)
                    {
                        var cond = ruleModel.PathCondition;
                        Condition pathCondition = new Condition(cond.Order, cond.ClassName, cond.Line, cond.Negative);
                        foreach (ConditionTest test in cond.Tests)
                        {
                            if (!_WMClasses.GetClass(cond.ClassName).AttributeExists(test.Attribute))
                            {
                                throw new Exception($"\n\nERROR - Class {pathCondition.ClassName} does not contain Attribute {test.Attribute} in {cond.Line}.\n\n");
                            }
                            pathCondition.Tests.Add(test);
                        }

                        prod.Conditions.Add(pathCondition);
                        prod.Specificity += pathCondition.Tests.Count();
                        beta = SetUpFindPath(prod, beta, pathCondition, ruleModel.FindPathInfo);

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

        private void ProcessDataFile(string file, string fileName)
        {
            IOCDFileModel iocdFileModel = _iocdParser.ParseIOCDFile(file, fileName);
            foreach (DataActionModel action in iocdFileModel.Actions)
            {
                if (action.Command.Contains("MAKE"))
                    Make(action.Atoms, fileName);
            }
            _logger.WriteInfo($"Completed file {fileName}", 0);
        }


        private void SetUpRelatedClasses(List<ClassModel> classModels)
        {
            //Sort out related classes.
            //In the .iocc file, relationships are defined using a virtual attribute that indicates that another class is a child of this class
            //e.g. Sections: [ Section ]  indicates that the Section class is a child of this class and zero or more Sections belong to an object of this class
            //However, in OPS5 objects, the child class must have an attribute that points to the ID of its parent, e.g. PARENTID
            //Therefore we must check for such attribute definitions and replace them with attributes in the children

            foreach (ClassModel classModel in classModels)
            {
                List<string> fewerAtoms = new List<string>();
                for (int a = 0; a < classModel.Atoms.Count; a++)
                {
                    string attribute = classModel.Atoms[a].ToUpper();
                    Match match = Regex.Match(attribute, @":\s*\[\s*.+\s*\]");
                    if (match.Success)
                    {
                        string childClass = match.Value;
                        attribute = attribute.Substring(0, attribute.IndexOf(':')).Trim();
                        childClass = Regex.Replace(childClass, @":|\[|\]", "");
                        childClass = childClass.Trim();
                        ClassModel? childModel = classModels.Where(_ => _.ClassName.ToUpper() == childClass).FirstOrDefault();
                        if (childModel != null)
                        {
                            string childAttribute = classModel.ClassName.ToUpper() + "ID";
                            childModel.Atoms.Add(childAttribute);
                            _classRelationships.CreateRelationship(classModel.ClassName, childClass, attribute, childAttribute);
                        }
                    }
                    else
                    {
                        fewerAtoms.Add(attribute);
                    }
                }
                classModel.Atoms = fewerAtoms;
            }

        }

        /// <summary>
        /// Adds a new Class to the Engine
        /// classFile indicates which source file the class belongs to
        /// </summary>
        /// <param name="className"></param>
        /// <param name="classFile"></param>
        /// <returns></returns>
        private IWMClass LiteraliseClass(string className, string classFile)
        {
            if (!_sourceFiles.ClassFiles.ContainsKey(classFile.ToUpper()))
                _sourceFiles.ClassFiles.Add(classFile.ToUpper(), new SourceFile(classFile, _sourceFiles.ProjectFile.FilePath, "", "", true, false));
            else
                _sourceFiles.ClassFiles[classFile.ToUpper()].Loaded = true;

            IWMClass newClass = default!;
            if (className.Contains(":"))
            {
                string[] classes = className.Split(':');
                if (_WMClasses.ClassExists(classes[1]))
                {
                    newClass = _WMClasses.Add(classes[0], classes[1]);
                    newClass.ClassFile = classFile;
                }
                else
                {
                    _logger.WriteError($"\n\nERROR - attempt to base class {classes[0]} on non-existent class {classes[1]}", "LiteraliseClass");
                }
            }
            else
            {
                newClass = _WMClasses.Add(className);
                newClass.ClassFile = classFile;
            }
            return newClass;
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


        private IBetaNode SetUpNetwork(IRule rule, Condition condition, IBetaNode betanode, bool negative, bool isFindPath, IFindPathInfo? findPath)
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
            IBetaNode betaNode = _betaMemory.BuildShareBeta(betanode, alphaNode, newTests, rule.Bindings, negative, condition.IsAny, isFindPath, findPath);
            //Then set current Beta
            return betaNode;
        }


        private IBetaNode SetUpFindPath(IRule rule, IBetaNode betaParent, Condition condition, IFindPathInfo findPath)
        {
            IBetaNode betaNode = SetUpNetwork(rule, condition, betaParent, false, true, findPath);

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
