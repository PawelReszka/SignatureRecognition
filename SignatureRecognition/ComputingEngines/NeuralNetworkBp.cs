using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FeatureEngines;
using FeatureEngines.Helpers;
using SignatureRecognition.Lib;

namespace ComputingEngines
{
    public class NeuralNetworkBp : IComputingEngine
    {
        private NeuralNetwork<string> network;

        public NeuralNetworkBp(double firstLayerParameter, double secondLayerParameter)
        {
            FirstLayerParameter = firstLayerParameter;
            SecondLayerParameter = secondLayerParameter;
        }

        [DefaultValue(0.33)]
        public double FirstLayerParameter { get; set;}

        [DefaultValue(0.11)]
        public double SecondLayerParameter { get; set; }


        public int Train(DataSet dataSet)
        {
            var data = dataSet.Signatures.Select(i => i.Data.Cast<double>().ToArray()).ToArray();
            var dict = new Dictionary<string, double[]>();
            for (int i = 0; i < data.Count(); i++)
            {
                dict.Add((i+1).ToString(), data[i]);
            }
            network = new NeuralNetwork<string>(new Layer3<string>(8*21,  (int)((double)((8*21  + 4)*FirstLayerParameter)),
                (int)((double)((8 * 21 + 4) * SecondLayerParameter)), 4), dict);
            return 1;
        }

        public double Verify(Feature feature)
        {
            string MatchedHigh = "?", MatchedLow = "?";
            double OutputValueHight = 0, OutputValueLow = 0;

            var input = feature.Data.Cast<double>().ToArray();

            network.Recognize(input, ref MatchedHigh, ref OutputValueHight,
                ref MatchedLow, ref OutputValueLow);

            return OutputValueHight;
        }
    }
}
