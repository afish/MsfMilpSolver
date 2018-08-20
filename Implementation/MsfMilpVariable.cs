using System;
using System.Collections.Generic;
using Microsoft.SolverFoundation.Services;
using MilpManager.Abstraction;
using Domain = MilpManager.Abstraction.Domain;

namespace MsfMilpManager.Implementation
{
	[Serializable]
	public class MsfMilpVariable : IMsfMilpVariable
	{
		[NonSerialized]
		private Decision _decision;

		[NonSerialized]
		private Term _term;

		[NonSerialized]
		private IMilpManager _baseMilpManager;
		public double? ConstantValue { get; set; }
		public string Expression { get; set; }
		public Domain Domain { get; set; }
	    public ICollection<string> Constraints { get; } = new List<string>();

        public IMilpManager MilpManager
		{
			get => _baseMilpManager;
            set => _baseMilpManager = value;
        }
		public Decision Decision
		{
			get => _decision;
		    set => _decision = value;
		}

		public string Name { get; set;  }

		public Term Term
		{
			get => _term;
		    set => _term = value;
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
