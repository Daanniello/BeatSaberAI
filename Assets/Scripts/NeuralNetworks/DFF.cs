using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

public class NeuralNetworkDFF : IComparable<NeuralNetworkDFF>
{
    private int[] layers;//layers
    private float[][] neurons;//neurons
    private float[][] biases;//biasses
    private float[][][] weights;//weights
    private activationEnum[] activations;//layers

    public float fitness = 0;//fitness

    public NeuralNetworkDFF(int[] layers, activationEnum[] activations)
    {
        this.activations = activations;
        this.layers = new int[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            this.layers[i] = layers[i];
        }
        InitNeurons();
        InitBiases();
        InitWeights();
    }

    private void InitNeurons()//create empty storage array for the neurons in the network.
    {
        List<float[]> neuronsList = new List<float[]>();
        for (int i = 0; i < layers.Length; i++)
        {
            neuronsList.Add(new float[layers[i]]);
        }
        neurons = neuronsList.ToArray();
    }

    private void InitBiases()//initializes and populates array for the biases being held within the network.
    {
        List<float[]> biasList = new List<float[]>();
        for (int i = 0; i < layers.Length; i++)
        {
            float[] bias = new float[layers[i]];
            for (int j = 0; j < layers[i]; j++)
            {
                bias[j] = UnityEngine.Random.Range(-0.5f, 0.5f);
            }
            biasList.Add(bias);
        }
        biases = biasList.ToArray();
    }

    private void InitWeights()//initializes random array for the weights being held in the network.
    {
        List<float[][]> weightsList = new List<float[][]>();
        for (int i = 1; i < layers.Length; i++)
        {
            List<float[]> layerWeightsList = new List<float[]>();
            int neuronsInPreviousLayer = layers[i - 1];
            for (int j = 0; j < neurons[i].Length; j++)
            {
                float[] neuronWeights = new float[neuronsInPreviousLayer];
                for (int k = 0; k < neuronsInPreviousLayer; k++)
                {
                    //float sd = 1f / ((neurons[i].Length + neuronsInPreviousLayer) / 2f);
                    neuronWeights[k] = UnityEngine.Random.Range(-0.5f, 0.5f);
                }
                layerWeightsList.Add(neuronWeights);
            }
            weightsList.Add(layerWeightsList.ToArray());
        }
        weights = weightsList.ToArray();
    }

    public float[] FeedForward(float[] inputs, bool shouldLog = false)//feed forward, inputs >==> outputs.
    {
        //Giving the input layer's neurons their values
        for (int i = 0; i < inputs.Length; i++)
        {
            neurons[0][i] = inputs[i];
        }
        for (int i = 1; i < layers.Length; i++)
        {
            int layer = i - 1;

            //Foreach layer after the input layer
            for (int j = 0; j < neurons[i].Length; j++)
            {
                float value = 0f;

                //Calculate the value for the neurons in the hidden and output layer
                for (int k = 0; k < neurons[i - 1].Length; k++)
                {
                    value += weights[i - 1][j][k] * neurons[i - 1][k];
                }

                //Activate the values with biases for each neuron
                neurons[i][j] = activate(value + biases[i][j], activations[i - 1]);
            }
        }

        if(shouldLog == true)
        {
            var InputLayerLog = "Input Layer: \n\n";
            foreach (var inputNeuron in neurons[0])
            {
                InputLayerLog += inputNeuron + "\n";
            }

            for (var i = 1; i < neurons.Length - 1; i++)
            {
                var HiddenLayerLog = $"Hidden Layer {i}: \n\n";
                foreach (var HiddenNeuron in neurons[i])
                {                   
                    HiddenLayerLog += HiddenNeuron + "\n";
                }
                GameObject.Find($"HiddenLayerLog{i}").GetComponent<TMPro.TextMeshPro>().SetText($"{HiddenLayerLog}");
            }

            var OutputLayerLog = "Output Layer: \n\n";
            foreach (var OutputNeuron in neurons[neurons.Length - 1])
            {
                OutputLayerLog += OutputNeuron + "\n";
            }


            GameObject.Find("InputLayerLog").GetComponent<TMPro.TextMeshPro>().SetText($"{InputLayerLog}");
            GameObject.Find("OutputLayerLog").GetComponent<TMPro.TextMeshPro>().SetText($"{OutputLayerLog}");

        }

        return neurons[neurons.Length - 1];
    }

    public float activate(float value, activationEnum activation)
    {
        switch (activation)
        {
            case activationEnum.sigmoid:
                return sigmoid(value);
            case activationEnum.tanh:
                return tanh(value);
            case activationEnum.relu:
                return relu(value);
            case activationEnum.leakyrelu:
                return leakyrelu(value);
            case activationEnum.sigmoidDer:
                return sigmoidDer(value);
            case activationEnum.tanhDer:
                return tanhDer(value);
            case activationEnum.reluDer:
                return reluDer(value);
            case activationEnum.leakyreluDer:
                return leakyreluDer(value);
            default:
                return (float)Math.Tanh(value);
        }
    }

    public float sigmoid(float x)//activation functions and their corrosponding derivatives
    {
        float k = (float)Math.Exp(x);
        return k / (1.0f + k);
    }
    public float tanh(float x)
    {
        return (float)Math.Tanh(x);
    }
    public float relu(float x)
    {
        return (0 >= x) ? 0 : x;
    }
    public float leakyrelu(float x)
    {
        return (0 >= x) ? 0.01f * x : x;
    }
    public float sigmoidDer(float x)
    {
        return x * (1 - x);
    }
    public float tanhDer(float x)
    {
        return 1 - (x * x);
    }
    public float reluDer(float x)
    {
        return (0 >= x) ? 0 : 1;
    }
    public float leakyreluDer(float x)
    {
        return (0 >= x) ? 0.01f : 1;
    }

    public enum activationEnum
    {
        sigmoid,
        tanh,
        relu,
        leakyrelu,
        sigmoidDer,
        tanhDer,
        reluDer,
        leakyreluDer
    }

    public void Mutate(int chance, float val)//used as a simple mutation function for any genetic implementations.
    {
        for (int i = 0; i < biases.Length; i++)
        {
            for (int j = 0; j < biases[i].Length; j++)
            {
                biases[i][j] = (UnityEngine.Random.Range(0f, chance) <= 5) ? biases[i][j] += UnityEngine.Random.Range(-val, val) : biases[i][j];
            }
        }

        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    weights[i][j][k] = (UnityEngine.Random.Range(0f, chance) <= 5) ? weights[i][j][k] += UnityEngine.Random.Range(-val, val) : weights[i][j][k];
                }
            }
        }
    }

    public int CompareTo(NeuralNetworkDFF other) //Comparing For NeuralNetworks performance.
    {
        if (other == null) return 1;

        if (fitness > other.fitness)
            return 1;
        else if (fitness < other.fitness)
            return -1;
        else
            return 0;
    }

    public NeuralNetworkDFF copy(NeuralNetworkDFF nn) //For creatinga deep copy, to ensure arrays are serialzed.
    {
        for (int i = 0; i < biases.Length; i++)
        {
            for (int j = 0; j < biases[i].Length; j++)
            {
                nn.biases[i][j] = biases[i][j];
            }
        }
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    nn.weights[i][j][k] = weights[i][j][k];
                }
            }
        }
        return nn;
    }

    public void Load(string path)//this loads the biases and weights from within a file into the neural network.
    {
        TextReader tr = new StreamReader(path);
        int NumberOfLines = (int)new FileInfo(path).Length;
        string[] ListLines = new string[NumberOfLines];
        int index = 1;
        for (int i = 1; i < NumberOfLines; i++)
        {
            ListLines[i] = tr.ReadLine();
        }
        tr.Close();
        if (new FileInfo(path).Length > 0)
        {
            for (int i = 0; i < biases.Length; i++)
            {
                for (int j = 0; j < biases[i].Length; j++)
                {
                    biases[i][j] = float.Parse(ListLines[index]);
                    index++;
                }
            }

            for (int i = 0; i < weights.Length; i++)
            {
                for (int j = 0; j < weights[i].Length; j++)
                {
                    for (int k = 0; k < weights[i][j].Length; k++)
                    {
                        weights[i][j][k] = float.Parse(ListLines[index]); ;
                        index++;
                    }
                }
            }
        }
    }

    public void Save(string path)//this is used for saving the biases and weights within the network to a file.
    {
        File.Create(path).Close();
        StreamWriter writer = new StreamWriter(path, true);

        for (int i = 0; i < biases.Length; i++)
        {
            for (int j = 0; j < biases[i].Length; j++)
            {
                writer.WriteLine(biases[i][j]);
            }
        }

        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    writer.WriteLine(weights[i][j][k]);
                }
            }
        }
        writer.Close();
    }
}
