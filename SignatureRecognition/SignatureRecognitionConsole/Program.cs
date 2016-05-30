using System;
using System.Configuration;
using System.IO;
using System.Linq;
using ComputingEngines;
using FeatureEngines;
using FeatureEngines.Helpers;
using FeatureEngines.RadonTransform;

namespace SignatureRecognitionConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = ConfigurationManager.AppSettings["FileDirectory"];
           

            var dataSet = new DataSet();
            dataSet.Id = 1;//int.Parse(currDir.Split('\\').Last());

            var files = Directory.GetFiles(dir+"/singatures").ToList();
            //files.Remove(files.Last());

            IFeatureExtractor featureEngine = new RadonTransformFeatureExtractor();

            foreach (var file in files)
            {
                dataSet.Signatures.Add(featureEngine.Extract(file));
            }
            double max = 0;

            foreach (var st in dataSet.Signatures)
            {
                for (int i = 0; i < 8; i++)
                {
                    for(int j=0; j< 21;j++)
                        if (max < st.Data[i, j])
                            max = st.Data[i, j];
                }
            }

            foreach (var st in dataSet.Signatures)
            {
                for (int i = 0; i < 8; i++)
                {
                    for (int j = 0; j < 21; j++)
                        st.Data[i, j] /= max;
                }
            }

            //var network = new NeuralNetworkBp();
            //network.Train(dataSet);

            //var toCheck = Directory.GetFiles(dir +"/test").First();
            //var check = featureEngine.Extract(toCheck);

            //for (int i = 0; i < 8; i++)
            //{
            //    for (int j = 0; j < 21; j++)
            //        check.Data[i, j] /= max;
            //}

            //network.Verify(check);


            //var forgeryName = Directory.GetFiles(dir + "/forgery").First();
            //var forgery = featureEngine.Extract(forgeryName);

            //network.Verify(forgery);
        }
    }
}
