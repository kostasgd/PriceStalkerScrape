﻿
// This file was auto-generated by ML.NET Model Builder. 

using System;

namespace MLModel2_ConsoleApp3
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create single instance of sample data from first line of dataset for model input
            MLModel2.ModelInput sampleData = new MLModel2.ModelInput()
            {
                Col0 = @"Ήχος",
            };

            // Make a single prediction on the sample data and print results
            var predictionResult = MLModel2.Predict(sampleData);

            Console.WriteLine("Using model to make single prediction -- Comparing actual Col1 with predicted Col1 from sample data...\n\n");


            Console.WriteLine($"Col0: {@"Ήχος"}");
            Console.WriteLine($"Col1: {@"positive"}");


            Console.WriteLine($"\n\nPredicted Col1: {predictionResult.PredictedLabel}\n\n");
            Console.WriteLine($"\n\nPredicted Col1: {predictionResult.ToString()}\n\n");
            Console.WriteLine("=============== End of process, hit any key to finish ===============");
            Console.ReadKey();
        }
    }
}
