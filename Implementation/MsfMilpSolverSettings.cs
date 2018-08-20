using MilpManager.Abstraction;

namespace MsfMilpManager.Implementation
{
	public class MsfMilpSolverSettings : MilpManagerSettings
	{
		public MsfMilpSolverSettings(bool recreateModelAtStart = true)
		{
		    RecreateModelAtStart = recreateModelAtStart;
		}

		public bool RecreateModelAtStart { get; }
	}
}