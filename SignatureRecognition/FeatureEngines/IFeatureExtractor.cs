using FeatureEngines.Helpers;

namespace FeatureEngines
{
    public interface IFeatureExtractor
    {
        Feature Extract(string file);

        DataSet GetTeachingDataset(string[] files, string[] forgeries = null);
    }
}