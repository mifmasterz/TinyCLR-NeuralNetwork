using System;
using System.Collections;
using System.IO;
//using System.Linq;
using System.Text;

namespace TinyNN
{
    /// <summary>
    /// A tiny neural network with one hidden layer and configurable parameters.
    /// </summary>
    public class TinyNeuralNetwork
    {
        internal float[] _weights;
        internal float[] _biases;
        internal float[] _hiddenLayer;
        internal float[] _outputLayer;
        internal int _inputCount;
        internal Random _random;

        /// <summary>
        /// Creates an instance of an untrained neural network.
        /// </summary>
        /// <param name="inputCount">Number of inputs or features.</param>
        /// <param name="hiddentCount">Number of hidden neurons in a hidden layer.</param>
        /// <param name="outputCount">Number of outputs or classes.</param>
        /// <param name="seed">A seed for random generator to produce predictable results.</param>
        public TinyNeuralNetwork(int inputCount, int hiddentCount, int outputCount, int seed = default)
        {
            _random = new Random(seed);
            _inputCount = inputCount;
            //_weights = Enumerable.Range(0, hiddentCount * (inputCount + outputCount)).Select(_ => (float)_random.NextDouble() - 0.5f).ToArray();
            var weightTemp = new ArrayList();
            for(var i = 0; i<= hiddentCount * (inputCount + outputCount); i++)
            {
                weightTemp.Add((float)_random.NextDouble() - 0.5f);
            }
            _weights = (float[])weightTemp.ToArray(typeof(double));

            //_biases = Enumerable.Range(0, 2).Select(_ => (float)_random.NextDouble() - 0.5f).ToArray(); // Tinn only supports one hidden layer so there are two biases. 
            var biasTemp = new ArrayList();
            for (var i = 0; i <= 2; i++)
            {
                biasTemp.Add((float)_random.NextDouble() - 0.5f);
            }
            _biases = (float[])biasTemp.ToArray(typeof(double));

            _hiddenLayer = new float[hiddentCount];
            _outputLayer = new float[outputCount];
        }

        private TinyNeuralNetwork(float[] weights, float[] biases, float[] hiddenLayer, float[] outputLayer, int inputCount, int seed = default)
        {
            _weights = weights;
            _biases = biases;
            _hiddenLayer = hiddenLayer;
            _outputLayer = outputLayer;
            _inputCount = inputCount;
            _random = new Random(seed);
        }
        #region file methods
        public static void WriteAllText(string path, string contents)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException("empth path", nameof(path));

            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.Write(contents);
            }
        }
        private static string[] InternalReadAllLines(string path, Encoding encoding)
        {
           string? line;
           var lines = new ArrayList();

            using (StreamReader sr = new StreamReader(path))
                while ((line = sr.ReadLine()) != null)
                    lines.Add(line);
            var myArr = (string[])lines.ToArray(typeof(string));
            return myArr;
        }

        public static string[] ReadAllLines(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                throw new ArgumentException("empty path", nameof(path));

            return InternalReadAllLines(path, Encoding.UTF8);
        }
        #endregion
        /// <summary>
        /// Loads a pretrained neural network from a `*.tinn` file.
        /// </summary>
        /// <param name="path">An absolute or a relative path to the `*.tinn` file.</param>
        /// <param name="seed">A seed for random generator to produce predictable results.</param>
        /// <returns>An instance of a pretrained <see cref="TinyNeuralNetwork"/>.</returns>
        public static TinyNeuralNetwork Load(string path, int seed = default)
        {
            var content = ReadAllLines(path);

            var counts = content[0].Split(' ');//.Select(int.Parse).ToArray();
            int inputCount, hiddenCount, outputCount;
            int.TryParse(counts[0], out inputCount);
            int.TryParse(counts[1], out hiddenCount);
            int.TryParse(counts[2], out outputCount);

            var temp = new ArrayList();
            //var parameters = content.Skip(1).Select(float.Parse).ToArray();
            for (int i = 1; i < content.Length; i++)
            {

                temp.Add(double.Parse(content[i]));
            }
            var parameters = (float[])temp.ToArray(typeof(double));
            var weights = new float[hiddenCount * (inputCount + outputCount)];
            var biases = new float[2];
            var hiddenLayer = new float[hiddenCount];
            var outputLayer = new float[outputCount];
            var biasCount = 2;
            for (var i = 0; i < biasCount; i++)
            {
                biases[i] = parameters[i];
            }
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = parameters[i + biasCount];
            }

            var network = new TinyNeuralNetwork(weights, biases, hiddenLayer, outputLayer, inputCount, seed);

            return network;
        }

        public static TinyNeuralNetwork LoadFromString(ref string s, int seed = default)
        {
            /*
            int inputCount=0, hiddenCount=0, outputCount=0;
            var temp = new ArrayList();
            var row = 0;
            char c = '\n';

            int l = s.Length;
            int i = 0, j = s.IndexOf(c, 0, l);
            if (j == -1) // No such substring
            {
                // Return original and break
                throw new Exception("model file is corrupt");
            }

            while (j != -1)
            {
                if (j - i > 0) // Non empty? 
                {
                    var content = s.Substring(i, j - i);
                    if (row == 0)
                    {  
                        var counts = content.Split(' ');
                       
                        int.TryParse(counts[0], out inputCount);
                        int.TryParse(counts[1], out hiddenCount);
                        int.TryParse(counts[2], out outputCount);
                    }
                    else
                    {
                        temp.Add(double.Parse(content));
                    }
                    //xx.Add(); // Return non-empty match
                    row++;
                }
                i = j + 1;
                j = s.IndexOf(c, i, l - i);
            }

            if (i < l) // Has remainder?
            {
                var content = s.Substring(i, j - i);
                if (row == 0)
                {
                    var counts = content.Split(' ');
             
                    int.TryParse(counts[0], out inputCount);
                    int.TryParse(counts[1], out hiddenCount);
                    int.TryParse(counts[2], out outputCount);
                }
                else
                {
                    temp.Add(double.Parse(content));
                }
                row++;
                //xx.Add(s.Substring(i, l - i)); // Return remaining trail
            }
            */
            var content = s.Split('\n');

            var counts = content[0].Split(' ');//.Select(int.Parse).ToArray();
            int inputCount, hiddenCount, outputCount;
            int.TryParse(counts[0], out inputCount);
            int.TryParse(counts[1], out hiddenCount);
            int.TryParse(counts[2], out outputCount);

            var temp = new ArrayList();
            //var parameters = content.Skip(1).Select(float.Parse).ToArray();
            for (int i = 1; i < content.Length; i++)
            { 
                temp.Add(double.Parse(content[i]));
            }
            var parameters = (double[])temp.ToArray(typeof(double));
            var weights = new float[hiddenCount * (inputCount + outputCount)];
            var biases = new float[2];
            var hiddenLayer = new float[hiddenCount];
            var outputLayer = new float[outputCount];
            var biasCount = 2;
            for (var z = 0; z < biasCount; z++)
            {
                biases[z] = (float)parameters[z];
            }
            for (int z = 0; z < weights.Length; z++)
            {
                weights[z] = (float)parameters[z + biasCount];
            }

            var network = new TinyNeuralNetwork(weights, biases, hiddenLayer, outputLayer, inputCount, seed);

            return network;
        }

        /// <summary>
        /// Predicts outputs from a given input.
        /// </summary>
        /// <param name="input">A float array matching the length of input count.</param>
        /// <returns>An array of predicted probabilities for each class. </returns>
        public float[] Predict(float[] input)
        {
            PropagateForward(input);

            return _outputLayer;
        }

        /// <summary>
        /// Trains neural network on a single data record.
        /// </summary>
        /// <param name="input">Records input or feature values.</param>
        /// <param name="expectedOutput">Actual record's class in a categorical format.</param>
        /// <param name="learningRate">Learning rate of a training.</param>
        /// <returns>Aggregated error value indicating how far off the neural network is on the training data set.</returns>
        public float Train(float[] input, float[] expectedOutput, float learningRate)
        {
            PropagateForward(input);
            PropogateBackward(input, expectedOutput, learningRate);

            return GetTotalError(expectedOutput, _outputLayer);
        }

        /// <summary>
        /// Saves a trained neural network to a `*.tinn` file.
        /// </summary>
        /// <param name="path">An absolute or a relative path to the `*.tinn` file.</param>
        public void Save(string path)
        {
            var builder = new StringBuilder();
            builder.Append($"{_inputCount} {_hiddenLayer.Length} {_outputLayer.Length}\n");
            foreach (var bias in _biases)
            {
                builder.Append($"{bias}\n");
            }
            foreach (var weight in _weights)
            {
                builder.Append($"{weight}\n");
            }

            WriteAllText(path, builder.ToString());
        }

        private void PropagateForward(float[] input)
        {
            // Calculate hidden layer neuron values.
            for (var i = 0; i < _hiddenLayer.Length; i++)
            {
                var sum = 0.0f;
                for (var j = 0; j < _inputCount; j++)
                {
                    sum += input[j] * _weights[i * _inputCount + j];
                }
                _hiddenLayer[i] = ActivationFunction(sum + _biases[0]);
            }

            // Calculate output layer neuron values.
            for (int i = 0; i < _outputLayer.Length; i++)
            {
                var sum = 0.0f;

                for (int j = 0; j < _hiddenLayer.Length; j++)
                    sum += _hiddenLayer[j] * _weights[(_hiddenLayer.Length * _inputCount) + (i * _hiddenLayer.Length + j)];
                _outputLayer[i] = ActivationFunction(sum + _biases[1]);
            }
        }
        private void PropogateBackward(float[] input, float[] expectedOutput, float learningRate)
        {
            for (var i = 0; i < _hiddenLayer.Length; i++)
            {
                var sum = 0.0f;
                // Calculate total error change with respect to output.
                for (var j = 0; j < _outputLayer.Length; j++)
                {
                    float a = LossFunctionPartialDerivative(_outputLayer[j], expectedOutput[j]);
                    float b = ActivationFunctionPartialDerivative(_outputLayer[j]);
                    sum += a * b * _weights[(_hiddenLayer.Length * _inputCount) + (j * _hiddenLayer.Length + i)];
                    // Correct weights in hidden to output layer.
                    _weights[(_hiddenLayer.Length * _inputCount) + (j * _hiddenLayer.Length + i)] -= learningRate * a * b * _hiddenLayer[i];
                }
                // Correct weights in input to hidden layer.
                for (int j = 0; j < _inputCount; j++)
                {
                    _weights[i * _inputCount + j] -= learningRate * sum * ActivationFunctionPartialDerivative(_hiddenLayer[i]) * input[j];
                }
            }
        }
        private float ActivationFunction(float value) => 1.0f / (1.0f + (float)Math.Exp(-value));
        private float ActivationFunctionPartialDerivative(float value) => value * (1f - value);
        private float LossFunction(float expected, float actual) => 0.5f * (expected - actual) * (expected - actual);
        private float LossFunctionPartialDerivative(float expected, float actual) => expected - actual;
        private float GetTotalError(float[] expected, float[] actual)
        {
            var sum = 0.0f;
            for (var i = 0; i < expected.Length; i++)
            {
                sum += LossFunction(expected[i], actual[i]);
            }

            return sum;
        }
    }
}
