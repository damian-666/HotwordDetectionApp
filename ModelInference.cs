using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

public class ModelInference
{
    private static InferenceSession _session;

    public static void LoadModel(string modelPath)
    {
        _session=new InferenceSession(modelPath);
    }

    public static int Predict(float[] audioData)
    {
        var inputMeta = _session.InputMetadata;
        var inputName = inputMeta.Keys.First();
        var inputTensor = new DenseTensor<float>(audioData, new[] { 1, 64, 64, 1 });

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
        };

        using (var results = _session.Run(inputs))
        {
            var output = results.First().AsEnumerable<float>().ToArray();
            return Array.IndexOf(output, output.Max());
        }
    }
}