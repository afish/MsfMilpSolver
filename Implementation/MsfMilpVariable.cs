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
        public double? ConstantValue { get; set; }
        public Domain Domain { get; }

        public IMilpManager MilpManager
        {
            get { return _baseMilpManager; }
            set { _baseMilpManager = value; }
        }
        public Decision Decision
        {
            get { return _decision; }
            internal set { _decision = value; }
        }

        public string Name { get; }

        public Term Term
        {
            get { return _term; }
            internal set { _term = value; }
        }

        public MsfMilpVariable(IMilpManager milpManager, string name, Domain domain)
        {
            MilpManager = milpManager;
            Name = name;
            Domain = domain;
        }

        public override string ToString()
        {
            return $"[Name = {Name}, Domain = {Domain}, ConstantValue = {ConstantValue}, Term = {Term}, Decision = {Decision}]";
        }
    }
}
