using System.Collections.Generic;
using FeatureEngines;

namespace FeatureEngines.Helpers
{
    public class DataSet
    {
        public int Id { get; set; }
        
        public  List<Feature> Signatures { get; set; } = new List<Feature>();

        public  List<Feature> Forgeries { get; set; } = new List<Feature>();
    }
}