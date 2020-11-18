using System;
using System.IO;
using System.Linq;
using Microsoft.SolverFoundation.Services;
using MilpManager.Abstraction;
using Domain = MilpManager.Abstraction.Domain;

namespace MsfMilpManager.Implementation
{
	public class MsfMilpSolver : PersistableMilpSolver, IModelSaver<MpsSettings>, IModelSaver<FreeMpsSettings>, IModelSaver<SmpsSettings>, IModelSaver<OmlSettings>
	{
		private int _constraintIndex;

		private Solution _solution;

		public new readonly MsfMilpSolverSettings Settings;

		public MsfMilpSolver(MsfMilpSolverSettings settings) : base(settings)
		{
			Settings = settings;

			if (settings.RecreateModelAtStart)
			{
				Context.ClearModel();
				Context.CreateModel();
			}
		}

		public SolverContext Context => SolverContext.GetContext();

		public Model Solver => Context.CurrentModel;

		protected override IVariable InternalFromConstant(string name, int value, Domain domain)
		{
			var variable = new MsfMilpVariable(this, name,
				domain)
			{
				Term = value
			};

			return variable;
		}

		protected override IVariable InternalFromConstant(string name, double value, Domain domain)
		{
			var variable = new MsfMilpVariable(this, name, domain) { Term = value };
			return variable;
		}

		protected override IVariable InternalCreate(string name, Domain domain)
		{
			var variable = new MsfMilpVariable(this, name, domain);

			if (Settings.FixBrokenRanges)
			{
				var msfDomain = domain == Domain.PositiveOrZeroReal || domain == Domain.PositiveOrZeroConstantReal || domain == Domain.AnyReal || domain == Domain.AnyConstantReal
								? Microsoft.SolverFoundation.Services.Domain.Real
								: Microsoft.SolverFoundation.Services.Domain.Integer;
				variable.Decision = new Decision(msfDomain, name);
				Solver.AddDecision(variable.Decision);
				variable.Term = variable.Decision;

				if (domain == Domain.BinaryConstantInteger || domain == Domain.BinaryInteger ||
					domain == Domain.PositiveOrZeroConstantInteger || domain == Domain.PositiveOrZeroInteger ||
						domain == Domain.PositiveOrZeroConstantReal || domain == Domain.PositiveOrZeroReal)
				{
					AddConstraint(variable.Term >= 0);
				}

				if (domain == Domain.BinaryConstantInteger || domain == Domain.BinaryInteger)
				{
					AddConstraint(variable.Term <= 1);
				}
			}
			else
			{
				var msfDomain = (domain == Domain.BinaryConstantInteger ||
							 domain == Domain.BinaryInteger)
				? Microsoft.SolverFoundation.Services.Domain.Boolean
				: (domain == Domain.PositiveOrZeroConstantInteger || domain == Domain.PositiveOrZeroInteger)
					? Microsoft.SolverFoundation.Services.Domain.IntegerNonnegative
					: (domain == Domain.PositiveOrZeroConstantReal || domain == Domain.PositiveOrZeroReal)
						? Microsoft.SolverFoundation.Services.Domain.RealNonnegative
						: (domain == Domain.AnyConstantInteger || domain == Domain.AnyInteger)
							? Microsoft.SolverFoundation.Services.Domain.Integer
							: Microsoft.SolverFoundation.Services.Domain.Real;
				variable.Decision = new Decision(msfDomain, name);
				Solver.AddDecision(variable.Decision);
				variable.Term = variable.Decision;
			}

			return variable;
		}

		protected override IVariable InternalSumVariables(IVariable first, IVariable second, Domain domain)
		{
			var firstVariable = (IMsfMilpVariable) first;
			var secondVariable = (IMsfMilpVariable) second;
			return new MsfMilpVariable(this, NewVariableName(), domain)
			{
				Term = firstVariable.Term + secondVariable.Term
			};
		}

		protected override IVariable InternalNegateVariable(IVariable variable, Domain domain)
		{
			var casted = (IMsfMilpVariable) variable;
			return new MsfMilpVariable(this, NewVariableName(), domain)
			{
				Term = -casted.Term
			};
		}

		protected override IVariable InternalMultiplyVariableByConstant(IVariable variable, IVariable constant, Domain domain)
		{
			return new MsfMilpVariable(this, NewVariableName(), domain)
			{
				Term = ((IMsfMilpVariable) variable).Term*((IMsfMilpVariable) constant).Term
			};
		}

		protected override IVariable InternalDivideVariableByConstant(IVariable variable, IVariable constant, Domain domain)
		{
			return new MsfMilpVariable(this, NewVariableName(), domain)
			{
				Term = ((IMsfMilpVariable) variable).Term/((IMsfMilpVariable) constant).Term
			};
		}

		public override void SetLessOrEqual(IVariable variable, IVariable bound)
		{
			AddConstraint(((IMsfMilpVariable) variable).Term <= ((IMsfMilpVariable) bound).Term);
		}

		protected override void InternalAddGoal(string name, IVariable operation)
		{
			Solver.AddGoal(name, GoalKind.Maximize, ((IMsfMilpVariable)operation).Term);
		}

		public override string GetGoalExpression(string name)
		{
			return Solver.Goals.First(g => g.Name == name).Expression;
		}

		public override void SaveModel(SaveFileSettings settings)
		{
			using (var fileWriter = File.CreateText(settings.Path))
			{
				Context.SaveModel(FileFormat.FreeMPS, fileWriter);
			}
		}

		public void SaveModel(MpsSettings settings)
		{
			Context.SaveModel(FileFormat.MPS, settings.TextWriter);
		}

		public void SaveModel(FreeMpsSettings settings)
		{
			Context.SaveModel(FileFormat.FreeMPS, settings.TextWriter);
		}

		public void SaveModel(SmpsSettings settings)
		{
			Context.SaveModel(FileFormat.SMPS, settings.TextWriter);
		}

		public void SaveModel(OmlSettings settings)
		{
			Context.SaveModel(FileFormat.OML, settings.TextWriter);
		}

		protected override object GetObjectsToSerialize()
		{
			return null;
		}

		protected override void InternalDeserialize(object data)
		{
			_constraintIndex = Context.CurrentModel.Constraints.Count() + 1;
			var msfMilpVariables = Variables.Values.Cast<IMsfMilpVariable>().ToArray();
			foreach (var variable in msfMilpVariables)
			{
				variable.Decision = Context.CurrentModel.Decisions.FirstOrDefault(d => d.Name == variable.Name);
				if (null == (object)variable.Decision)
				{
					Variables.Remove(variable.Name);
					continue;
				}
				variable.Term = variable.Decision * 1;
			}
		}

		protected override void InternalLoadModelFromFile(string modelPath)
		{
			Context.ClearModel();
			Context.LoadModel(FileFormat.FreeMPS, modelPath);
		}

		private void AddConstraint(Term term)
		{
			Solver.AddConstraint("c_" + _constraintIndex++, term);
		}

		public override void SetGreaterOrEqual(IVariable variable, IVariable bound)
		{
			AddConstraint(((IMsfMilpVariable) variable).Term >= ((IMsfMilpVariable) bound).Term);
		}

		public override void SetEqual(IVariable variable, IVariable bound)
		{
			AddConstraint(((IMsfMilpVariable) variable).Term == ((IMsfMilpVariable) bound).Term);
		}

		public override void Solve()
		{
			_solution = Context.Solve();
		}

		public override double GetValue(IVariable variable)
		{
			return ((IMsfMilpVariable)variable).Decision.ToDouble();
		}

		public override SolutionStatus GetStatus()
		{
			if (_solution == null)
			{
				throw new InvalidOperationException("Models must be solved first");
			}

			var quality = _solution.Quality;
			if (quality == SolverQuality.Infeasible || quality == SolverQuality.InfeasibleOrUnbounded || quality == SolverQuality.LocalInfeasible)
			{
				return SolutionStatus.Infeasible;
			}
			if (quality == SolverQuality.Unbounded)
			{
				return SolutionStatus.Unbounded;
			}
			if (quality == SolverQuality.Optimal || quality == SolverQuality.LocalOptimal)
			{
				return SolutionStatus.Optimal;
			}
			if (quality == SolverQuality.Feasible)
			{
				return SolutionStatus.Feasible;
			}

			return SolutionStatus.Unknown;

		}
	}
}