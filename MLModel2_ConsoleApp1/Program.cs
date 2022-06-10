﻿
// This file was auto-generated by ML.NET Model Builder. 

using System;

namespace MLModel2_ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create single instance of sample data from first line of dataset for model input
            MLModel2.ModelInput sampleData = new MLModel2.ModelInput()
            {
                Col1 = @"δεν μου αρεσε",
            };

            // Make a single prediction on the sample data and print results
            var predictionResult = MLModel2.Predict(sampleData);
            Console.WriteLine($"Col0: {@"Αποτέλεσμα"}");
            Console.WriteLine($"\n\nPredicted Col0: {predictionResult.Prediction}\n\n");
            Console.WriteLine("=============== End of process, hit any key to finish ===============");
            Console.ReadKey();
        }
    }
}
