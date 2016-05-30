namespace SignatureRecognitionWebApp.Models
{
    public class AlgorithmDataModel
    {
        public string[] Files { get; set; }

        public string[] Forgeries { get; set; }

        public string Directory { get; set; }

        public string ForgeriesDirectory { get; set; }

        public string FeatureAlgorithmName { get; set; }
        
        public string ComputingAlgorithmName { get; set; }

        public string[] FeatureAlgorithmParameters { get; set; }

        public string[] ComputingAlgorithmParameters { get; set; }    
    }
}