using FeatureEngines;
using FeatureEngines.Helpers;

namespace ComputingEngines
{
    public interface IComputingEngine
    {
        int Train(DataSet dataSet);

        double Verify(Feature feature);
    }
}