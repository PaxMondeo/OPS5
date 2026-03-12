using System;
using System.Collections.Generic;
using System.Linq;

using OPS5.Engine.Contracts;

namespace OPS5.Engine
{
    internal class RHSActionFactory : IRHSActionFactory
    {
        IServiceProvider _serviceProvider;
        IOPS5Logger _logger;

        public RHSActionFactory(IServiceProvider serviceProvider, IOPS5Logger logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IRHSAction NewRHSAction(string actionText, List<object> action)
        {
            var a = _serviceProvider.GetService(typeof(IRHSAction));
            if (a == null)
                throw new Exception("Unable to instantiate new RHS Action");
            IRHSAction rhsAction = (IRHSAction)a;
            try
            {
                rhsAction.SetProperties(actionText, action);
            }
            catch (Exception)
            {
                _logger.WriteError($"Error attempting to set up RHS action {actionText}", "Action Factory");
                throw;
            }
            return rhsAction;
        }

        public IRHSAction NewRHSAction(string actionText, List<string> action)
        {
            var a = _serviceProvider.GetService(typeof(IRHSAction));
            if (a == null)
                throw new Exception("Unable to instantiate new RHS Action");
            IRHSAction rhsAction = (IRHSAction)a;
            rhsAction.SetProperties(actionText, action);
            return rhsAction;
        }

    }
    /// <summary>
    /// Righ Hand Side Action for a Rule
    /// </summary>
    internal class RHSAction : IRHSAction
    {
        private IOPS5Logger _logger;
        /// <summary>
        /// Text representation of the action, loaded from the source file
        /// </summary>
        public string ActionText { get; private set; } = string.Empty;
        /// <summary>
        /// Action object which contains hierarchical list of actions
        /// </summary>
        public List<object> Action { get; private set; } = new List<object>();


        public RHSAction(IOPS5Logger logger)
        {
            _logger = logger;
        }

        public void SetProperties(string actionText, List<string> action)
        {
            List<object> actions = new List<object>();
            foreach (string act in action)
                actions.Add(act);
            SetProperties(actionText, actions);
        }

        /// <summary>
        /// Creates a new RHS Action object containing a list of Action Objects (each object containing more actions)
        /// </summary>
        /// <param name="actionText"></param>
        /// <param name="action"></param>
        public void SetProperties(string actionText, List<object> action)
        {
            bool ok = true;
            if (action[0] is string a)
            {
                switch (a.ToUpper())
                {
                    case "MAKE":
                    case "MODIFY":
                    case "REMOVE":
                        if (action.Count % 2 != 0)
                        {
                            ok = false;
                            for (int x = 0; x < action.Count; x++)
                            {
                                if (action[x] is string b)
                                {
                                    switch (b.ToUpper())
                                    {
                                        case "CALC":
                                        case "SUBSTR":
                                            ok = true;
                                            break;
                                    }
                                }
                            }
                        }
                        break;
                }
            }
            if (ok)
            {
                Action = new List<object>();
                ActionText = actionText;
                Action = action;
            }
            else
                _logger.WriteError($"Syntax error in line {actionText}", "RHSAction.SetProperties");
        }
    }
}
