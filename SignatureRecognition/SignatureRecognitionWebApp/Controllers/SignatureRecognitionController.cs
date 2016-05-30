using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using ComputingEngines;
using FeatureEngines;
using FeatureEngines.Helpers;
using FeatureEngines.RadonTransform;
using SignatureRecognitionWebApp.Models;
using WebGrease.Css.Extensions;

namespace SignatureRecognitionWebApp.Controllers
{
    public class SignatureRecognitionController : Controller
    {
        List<string> FeatureAlghoritms = new List<string>() { "aaa", "RadonTransformFeatureExtractor" };
        List<string> TeachAlghoritms = new List<string>() { "aaa", "NeuralNetworkBp", "EncogNeuralNetwork" };

        private static IComputingEngine _engine { get; set; }
        private static IFeatureExtractor _extractor { get; set; }
        // GET: SignatureRecognition
        public ActionResult Index()
        {
            ViewBag.FeatureAlgs = FeatureAlghoritms;
            ViewBag.TeachAlgs = TeachAlghoritms;
            return View();
        }

        [Route("initFeatureAlg/{algorithmName}")]
        public PartialViewResult InitFeatureAlgorithm(string algorithmName)
        {
            var fields = Type.GetType("FeatureEngines.RadonTransform." + algorithmName+", FeatureEngines").GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var names = Array.ConvertAll(fields, field => new FieldModel() { FieldName = field.Name, Value = ((DefaultValueAttribute)(field.GetCustomAttributes().First())).Value.ToString().Replace(",", ".") });
            ViewBag.Fields = names;
            return PartialView();
        }

        [Route("initComputingAlg/{algorithmName}")]
        public PartialViewResult InitComputingAlgorithm(string algorithmName)
        {
            var fields = Type.GetType("ComputingEngines." + algorithmName + ", ComputingEngines").GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var names = Array.ConvertAll(fields, field => new FieldModel() { FieldName = field.Name, Value = ((DefaultValueAttribute)(field.GetCustomAttributes().First())).Value.ToString().Replace(",", ".")});
            ViewBag.Fields = names;
            return PartialView();
        }

        [HttpPost]
        [Route("Teach")]
        public string Teach(AlgorithmDataModel teachingData)
        {
            DataSet dataSet;
            var files = teachingData.Files.Select(i => teachingData.Directory + i).ToArray();
            var forgeries = teachingData.Forgeries?.Select(i => teachingData.ForgeriesDirectory + i).ToArray();
            switch (teachingData.FeatureAlgorithmName)
            {
                case "RadonTransformFeatureExtractor":
                    _extractor = new RadonTransformFeatureExtractor(int.Parse(teachingData.FeatureAlgorithmParameters[0]));
                    dataSet = _extractor.GetTeachingDataset(files, forgeries);
                    break;
                default:
                    throw new ArgumentException("FeatureAlgorithmName");
            }

            switch (teachingData.ComputingAlgorithmName)
            {
                case "NeuralNetworkBp":
                    _engine = new NeuralNetworkBp(double.Parse(teachingData.ComputingAlgorithmParameters[0].Replace(".",",")),
                        double.Parse(teachingData.ComputingAlgorithmParameters[1].Replace(".", ",")));
                    _engine.Train(dataSet);
                    break;
                case "EncogNeuralNetwork":
                    _engine = new EncogNeuralNetwork(double.Parse(teachingData.ComputingAlgorithmParameters[0].Replace(".", ",")),
                        double.Parse(teachingData.ComputingAlgorithmParameters[1].Replace(".", ",")));
                    _engine.Train(dataSet);
                    break;
                default:
                    throw new ArgumentException("ComputingAlgorithmName");
            }
            
            return "OK";
        }

        [HttpPost]
        [Route("verify")]
        public string Verify(VerifyDataModel model)
        {
            Feature toVerify = _extractor.Extract(model.File);
            var result = _engine.Verify(toVerify);

            return model.File.Split(new[] {'\\'}, StringSplitOptions.None).Last() + "    " + result;
        }
    }
}