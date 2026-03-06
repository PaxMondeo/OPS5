using OPS5.Engine.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace OPS5.Engine.Calculators
{
    internal class OPS5Calculators : ICalculators
    {
        private Dictionary<string, ICalculator> _calculators = new Dictionary<string, ICalculator>(StringComparer.OrdinalIgnoreCase);
        private string _default = "RPN";

        public OPS5Calculators(IServiceProvider serviceProvider)
        {
            var calcs = serviceProvider.GetServices<ICalculator>();
            foreach(ICalculator c in calcs)
            {
                _calculators.Add(c.CalcType(), c);
            }
        }


        public void SetDefault(string calcType)
        {
            if (_calculators.ContainsKey(calcType))
            {
                _default = calcType;
            }
        }

        public ICalculator Default()
        {
            return _calculators[_default];
        }

        public ICalculator GetCalculator(string calcType)
        {
            return _calculators[calcType];
        }
    }


}
