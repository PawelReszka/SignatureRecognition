using System.Collections.Generic;
using FeatureEngines;

namespace Helpers
{
    public class DataSet
    {
        public int Id { get; set; }
        
        public  List<Feature> Signatures { get; set; } = new List<string>();

        public  List<string> Forgeries { get; set; } = new List<string>();
    }
}