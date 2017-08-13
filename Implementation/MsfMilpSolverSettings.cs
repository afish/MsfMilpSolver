using MilpManager.Abstraction;

namespace MsfMilpManager.Implementation
{
	public class MsfMilpSolverSettings : MilpManagerSettings
	{
		public MsfMilpSolverSettings()
		{
			RecreateModelAtStart = true;
		}

		public bool RecreateModelAtStart { get; set; }
	}
}