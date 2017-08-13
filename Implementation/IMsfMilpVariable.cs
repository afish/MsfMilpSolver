using Microsoft.SolverFoundation.Services;
using MilpManager.Abstraction;

namespace MsfMilpManager.Implementation
{
	public interface IMsfMilpVariable : IVariable
	{
		Term Term { get; set; }
		Decision Decision { get; set; }
	}
}