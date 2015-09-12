using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.SolverFoundation.Services;
using MilpManager.Abstraction;
using Domain = MilpManager.Abstraction.Domain;

namespace MsfMilpManager.Implementation
{
    public class MsfMilpSolver : BaseMilpSolver
    {
        private int _constraintIndex;
        private Solution _solution;

        public MsfMilpSolver(int integerWidth = 10)
            : base(integerWidth)
        {
            Context.ClearModel();
            Context.CreateModel();
        }

        public SolverContext Context
        {
            get { return SolverContext.GetContext(); }
        }

        public Model Solver
        {
            get { return Context.CurrentModel; }
        }

        protected override IVariable InternalFromConstant(string name, int value, Domain domain)
        {
            var variable = new MsfMilpVariable(this, name,
                domain)
            {
                Term = 1*value
            };

            return variable;
        }

        protected override IVariable InternalFromConstant(string name, double value, Domain domain)
        {
            var variable = new MsfMilpVariable(this, name, domain) { Term = 1 * value };
            return variable;
        }

        protected override IVariable InternalCreate(string name, Domain domain)
        {
            var variable = new MsfMilpVariable(this, name, domain);
            var msfDomain = (domain == Domain.BinaryConstantInteger ||
                             domain == Domain.BinaryInteger)
                ? Microsoft.SolverFoundation.Services.Domain.Boolean
                : (domain == Domain.PositiveOrZeroConstantInteger || domain == Domain.PositiveOrZeroInteger)
                    ? Microsoft.SolverFoundation.Services.Domain.IntegerRange(0, Int64.MaxValue)
                    : (domain == Domain.PositiveOrZeroConstantReal || domain == Domain.PositiveOrZeroReal)
                        ? Microsoft.SolverFoundation.Services.Domain.RealNonnegative
                        : (domain == Domain.AnyConstantInteger || domain == Domain.AnyInteger)
                            ? Microsoft.SolverFoundation.Services.Domain.IntegerRange(Int64.MinValue, Int64.MaxValue)
                            : Microsoft.SolverFoundation.Services.Domain.Real;
            variable.Decision = new Decision(msfDomain, name);
            Solver.AddDecision(variable.Decision);
            variable.Term = 1*variable.Decision;

            return variable;
        }

        protected override IVariable InternalSumVariables(IVariable first, IVariable second, Domain domain)
        {
            var firstVariable = (MsfMilpVariable) first;
            var secondVariable = (MsfMilpVariable) second;
            return new MsfMilpVariable(this, NewVariableName(), domain)
            {
                Term = firstVariable.Term + secondVariable.Term
            };
        }

        protected override IVariable InternalNegateVariable(IVariable variable, Domain domain)
        {
            var casted = (MsfMilpVariable) variable;
            return new MsfMilpVariable(this, NewVariableName(), domain)
            {
                Term = -casted.Term
            };
        }

        protected override IVariable InternalMultiplyVariableByConstant(IVariable variable, IVariable constant, Domain domain)
        {
            return new MsfMilpVariable(this, NewVariableName(), domain)
            {
                Term = ((MsfMilpVariable) variable).Term*((MsfMilpVariable) constant).Term
            };
        }

        protected override IVariable InternalDivideVariableByConstant(IVariable variable, IVariable constant, Domain domain)
        {
            return new MsfMilpVariable(this, NewVariableName(), domain)
            {
                Term = ((MsfMilpVariable) variable).Term/((MsfMilpVariable) constant).Term
            };
        }

        public override void SetLessOrEqual(IVariable variable, IVariable bound)
        {
            AddConstraint(((MsfMilpVariable) variable).Term <= ((MsfMilpVariable) bound).Term);
        }

        public override void AddGoal(string name, IVariable operation)
        {
            Solver.AddGoal(name, GoalKind.Maximize, ((MsfMilpVariable)operation).Term);
        }

        public override string GetGoalExpression(string name)
        {
            return Solver.Goals.First(g => g.Name == name).Expression;
        }

        public override void SaveModelToFile(string modelPath)
        {
            using (var file = File.CreateText(modelPath))
            {
                Context.SaveModel(FileFormat.FreeMPS, file);
                file.Close();
            }
        }

        public override void LoadModelFromFile(string modelPath, string solverDataPath)
        {
            Context.ClearModel();
            Context.LoadModel(FileFormat.FreeMPS, modelPath);
            using (var serializationStream = File.Open(solverDataPath, FileMode.Open))
            {
                Variables = (IDictionary<string, IVariable>)new BinaryFormatter().Deserialize(serializationStream);
                _constraintIndex = Context.CurrentModel.Constraints.Count() + 1;
                VariableIndex = Context.CurrentModel.Decisions.Count() + 1;
            }
            var msfMilpVariables = Variables.Values.Cast<MsfMilpVariable>().ToArray();
            foreach (var variable in msfMilpVariables)
            {
                variable.Decision = Context.CurrentModel.Decisions.FirstOrDefault(d => d.Name == variable.Name);
                if (null == (object) variable.Decision)
                {
                    Variables.Remove(variable.Name);
                    continue;
                }
                variable.Term = variable.Decision*1;
                variable.MilpManager = this;
            }
        }

        public override void SaveSolverDataToFile(string solverOutput)
        {
            using (var serializationStream = File.Open(solverOutput, FileMode.Create))
            {
                new BinaryFormatter().Serialize(serializationStream, Variables);
            }
        }

        private void AddConstraint(Term term)
        {
            Solver.AddConstraint("c_" + _constraintIndex++, term);
        }

        public override void SetGreaterOrEqual(IVariable variable, IVariable bound)
        {
            AddConstraint(((MsfMilpVariable) variable).Term >= ((MsfMilpVariable) bound).Term);
        }

        public override void SetEqual(IVariable variable, IVariable bound)
        {
            AddConstraint(((MsfMilpVariable) variable).Term == ((MsfMilpVariable) bound).Term);
        }

        public override void Solve()
        {
            _solution = Context.Solve();
        }

        public override double GetValue(IVariable variable)
        {
            return double.Parse(((MsfMilpVariable)variable).Decision.ToString());
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