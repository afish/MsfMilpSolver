using System;
using Microsoft.SolverFoundation.Services;
using MilpManager.Abstraction;
using Domain = MilpManager.Abstraction.Domain;

namespace MsfMilpManager.Implementation
{
    [Serializable]
    public class MsfMilpVariable : IVariable
    {
        [NonSerialized]
        private Decision _decision;

        [NonSerialized]
        private Term _term;

        [NonSerialized]
        private IMilpManager _baseMilpManager;

        public IMilpManager MilpManager
        {
            get { return _baseMilpManager; }
            internal set { _baseMilpManager = value; }
        }

        public string Name { get; private set; }
        public Domain Domain { get; private set; }
        public Decision Decision
        {
            get { return _decision; }
            internal set { _decision = value; }
        }

        public Term Term
        {
            get { return _term; }
            internal set { _term = value; }
        }

        protected internal MsfMilpVariable(IMilpManager milpManager, string name, Domain domain)
        {
            MilpManager = milpManager;
            Name = name;
            Domain = domain;
        }
    }
}
