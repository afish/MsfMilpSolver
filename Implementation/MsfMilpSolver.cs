using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.SolverFoundation.Services;
using MilpManager.Abstraction;
using MsfMilpManager.Implementation;
using Domain = MilpManager.Abstraction.Domain;

namespace MsfMilpSolver.Implementation
{
    public class MsfMilpSolver : BaseMilpSolver
    {
        private int _constraintIndex;
        private int _variableIndex;
        private Dictionary<string, IVariable> _variables = new Dictionary<string, IVariable>();
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

        public override IVariable FromConstant(int value, Domain domain)
        {
            var variable = new MsfMilpVariable(this, GetVariableName(),
                domain)
            {
                Term = 1*value
            };

            return variable;
        }

        public override IVariable FromConstant(double value, Domain domain)
        {
            var variable = new MsfMilpVariable(this, GetVariableName(), domain) { Term = 1 * value };
            return variable;
        }

        public override IVariable Create(string name, Domain domain)
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
            _variables[name] = variable;

            return variable;
        }

        public override IVariable SumVariables(IVariable first, IVariable second, Domain domain)
        {
            var firstVariable = (MsfMilpVariable) first;
            var secondVariable = (MsfMilpVariable) second;
            return new MsfMilpVariable(this, GetVariableName(), domain)
            {
                Term = firstVariable.Term + secondVariable.Term
            };
        }

        public override IVariable NegateVariable(IVariable variable, Domain domain)
        {
            var casted = (MsfMilpVariable) variable;
            return new MsfMilpVariable(this, GetVariableName(), domain)
            {
                Term = -casted.Term
            };
        }

        public override IVariable MultiplyVariableByConstant(IVariable variable, IVariable constant, Domain domain)
        {
            return new MsfMilpVariable(this, GetVariableName(), domain)
            {
                Term = ((MsfMilpVariable) variable).Term*((MsfMilpVariable) constant).Term
            };
        }

        public override IVariable DivideVariableByConstant(IVariable variable, IVariable constant, Domain domain)
        {
            return new MsfMilpVariable(this, GetVariableName(), domain)
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
            }
        }

        public override void LoadModelFromFile(string modelPath, string solverDataPath)
        {
            Context.ClearModel();
            Context.LoadModel(FileFormat.FreeMPS, modelPath);
            var deserialized =
                (Tuple<Dictionary<string, IVariable>, int, int>)
                    new BinaryFormatter().Deserialize(File.Open(solverDataPath, FileMode.Open));
            _variables = deserialized.Item1;
            _constraintIndex = deserialized.Item2;
            _variableIndex = deserialized.Item3;
            foreach (var variable in _variables.Values.Cast<MsfMilpVariable>())
            {
                variable.Decision = Context.CurrentModel.Decisions.First(d => d.Name == variable.Name);
                variable.Term = variable.Decision*1;
                variable.MilpManager = this;
            }
        }

        public override void SaveSolverDataToFile(string solverOutput)
        {
            new BinaryFormatter().Serialize(File.Open(solverOutput, FileMode.Create),
                Tuple.Create(_variables, _constraintIndex, _variableIndex));
        }

        private void AddConstraint(Term term, [CallerFilePath] string prefix = "")
        {
            Solver.AddConstraint("c_" + GetFilenameBase(prefix) + "_" + _constraintIndex++, term);
        }

        private string GetFilenameBase(string name)
        {
            return Path.GetFileNameWithoutExtension(name);
        }

        public override void SetGreaterOrEqual(IVariable variable, IVariable bound)
        {
            AddConstraint(((MsfMilpVariable) variable).Term >= ((MsfMilpVariable) bound).Term);
        }

        public override void SetEqual(IVariable variable, IVariable bound)
        {
            AddConstraint(((MsfMilpVariable) variable).Term == ((MsfMilpVariable) bound).Term);
        }

        public override IVariable GetByName(string name)
        {
            return _variables[name];
        }

        public override IVariable TryGetByName(string name)
        {
            IVariable result;
            _variables.TryGetValue(name, out result);
            return result;
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

        private string GetVariableName(string prefix = "")
        {
            return prefix + "v_" + "_" + _variableIndex++;
        }

        public override IVariable CreateAnonymous(Domain domain)
        {
            return Create(GetVariableName(), domain);
        }
    }
}