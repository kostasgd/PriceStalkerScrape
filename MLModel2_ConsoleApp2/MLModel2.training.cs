﻿﻿// This file was auto-generated by ML.NET Model Builder. 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms;
using Microsoft.ML;

namespace MLModel2_ConsoleApp2
{
    public partial class MLModel2
    {
        public static ITransformer RetrainPipeline(MLContext context, IDataView trainData)
        {
            var pipeline = BuildPipeline(context);
            var model = pipeline.Fit(trainData);

            return model;
        }

        /// <summary>
        /// build the pipeline that is used from model builder. Use this function to retrain model.
        /// </summary>
        /// <param name="mlContext"></param>
        /// <returns></returns>
        public static IEstimator<ITransformer> BuildPipeline(MLContext mlContext)
        {
            // Data process configuration with pipeline data transformations
            var pipeline = mlContext.Transforms.Categorical.OneHotEncoding(@"col0", @"col0", outputKind: OneHotEncodingEstimator.OutputKind.Indicator)      
                                    .Append(mlContext.Transforms.Concatenate(@"Features", new []{@"col0"}))      
                                    .Append(mlContext.Transforms.Conversion.MapValueToKey(outputColumnName:@"col1",inputColumnName:@"col1"))      
                                    .Append(mlContext.Transforms.NormalizeMinMax(@"Features", @"Features"))      
                                    .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(new SdcaMaximumEntropyMulticlassTrainer.Options(){L1Regularization=1F,L2Regularization=0.1F,LabelColumnName=@"col1",FeatureColumnName=@"Features"}))      
                                    .Append(mlContext.Transforms.Conversion.MapKeyToValue(outputColumnName:@"PredictedLabel",inputColumnName:@"PredictedLabel"));

            return pipeline;
        }
    }
}