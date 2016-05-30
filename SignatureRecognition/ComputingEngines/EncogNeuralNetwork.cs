using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Encog.Engine.Network.Activation;
using Encog.ML.Data;
using Encog.ML.Data.Basic;
using Encog.ML.Train;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Layers;
using Encog.Neural.Networks.Training.Propagation.Resilient;
using FeatureEngines;
using FeatureEngines.Helpers;

namespace ComputingEngines
{
    public class EncogNeuralNetwork : IComputingEngine
    {
        [DefaultValue(0.33)]
        public double FirstLayerParameter { get; set; }

        [DefaultValue(0.11)]
        public double SecondLayerParameter { get; set; }

        private BasicNetwork Network { get; set; }

        public EncogNeuralNetwork(double firstLayerParameter, double secondLayerParameter)
        {
            FirstLayerParameter = firstLayerParameter;
            SecondLayerParameter = secondLayerParameter;
        }

        public int Train(DataSet dataSet)
        {
            Network = new BasicNetwork();
            Network.AddLayer(new BasicLayer(null, true, 8 * 21));
            var first = ((8 * 21 + 4) * FirstLayerParameter);
            Network.AddLayer(new BasicLayer(new ActivationSigmoid(), true, (int)first));
            var second = ((8 * 21 + 4) * SecondLayerParameter);
            Network.AddLayer(new BasicLayer(new ActivationSigmoid(), true, (int)second));
            Network.AddLayer(new BasicLayer(null, false, 1));
           // Network.AddLayer(new );
            Network.Structure.FinalizeStructure(); 
            Network.Reset();
            //IMLData x = new BasicNeuralData();
            var set = new double[dataSet.Signatures.Count + dataSet.Forgeries.Count][];
            var ideal = new double[dataSet.Signatures.Count + dataSet.Forgeries.Count][];
            for (int i = 0; i < dataSet.Signatures.Count; i++)
            {
                set[i] = dataSet.Signatures[i].Data.Cast<double>().ToArray();
                ideal[i] = new double[] {1};
            }
            for (int i = dataSet.Signatures.Count; i < dataSet.Signatures.Count  + dataSet.Forgeries.Count; i++)
            {
                set[i] = dataSet.Forgeries[i- dataSet.Signatures.Count].Data.Cast<double>().ToArray();
                ideal[i] = new double[] { 0 };
            }

            IMLDataSet trainingSet = new BasicMLDataSet(set, ideal);

            IMLTrain train = new ResilientPropagation(Network, trainingSet);

            int epoch = 1;
            var errors = new List<double>();
            do
            {
                train.Iteration();
                // Console.WriteLine(@"Epoch #" + epoch + @" Error:" + train.Error);
                epoch++;
                errors.Add(train.Error);

            } while ( epoch < 10000);

            train.FinishTraining();

            return 1;
        }

        public double Verify(Feature feature)
        {
            IMLData input = new BasicMLData(feature.Data.Cast<double>().ToArray());
            IMLData output = Network.Compute(input);
            
            return output[0];
        }
    }
}