using MilpManager.Abstraction;

namespace MsfMilpManager.Implementation
{
	public class MsfMilpSolverSettings : MilpManagerSettings
	{
		public MsfMilpSolverSettings(bool recreateModelAtStart = true, bool fixBrokenRanges = true)
		{
		    RecreateModelAtStart = recreateModelAtStart;
			FixBrokenRanges = fixBrokenRanges;
		}

		public bool RecreateModelAtStart { get; }
		public bool FixBrokenRanges { get; }
	}
}