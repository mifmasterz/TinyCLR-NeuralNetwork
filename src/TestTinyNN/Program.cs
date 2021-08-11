using GHIElectronics.TinyCLR.Devices.Display;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.I2c;
using GHIElectronics.TinyCLR.Devices.Storage;
using GHIElectronics.TinyCLR.Drivers.FocalTech.FT5xx6;
using GHIElectronics.TinyCLR.IO;
using GHIElectronics.TinyCLR.Pins;
using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using TestTinyNN.Properties;
using TinyNN;

namespace TestTinyNN
{
    class Program
    {
        #region Neiral Network
        const string datasetUri = "http://archive.ics.uci.edu/ml/machine-learning-databases/semeion/semeion.data";
        const string dataSetFileName = "semeion.data";
        const int inputCount = 256;
        const int hiddentCount = 28;
        const int outputCount = 10;

        const int learningIterations = 10;
        static float learningRate = 1f;
        const float learningRateDecay = 0.99f;
        #endregion



        const int WidthBox = 400;
        const int HeightBox = 400;
        const int NumBlock = 16;
        static int BlockSize;
        static float[] InputData;
        static float[][] Matrix;
        static Font font;
        static Graphics screen;
        static Helpers.Rectangle DrawingBox;
        static Helpers.Rectangle ResultBox;
        static Helpers.Rectangle ButtonReset;
        static Helpers.Rectangle ButtonRecognize;
        static TinyNeuralNetwork model;

    
        static void Main()
        {
            InputData = new float[NumBlock * NumBlock];
            DrawingBox = new Helpers.Rectangle(20, 20, WidthBox, HeightBox);
            ButtonRecognize = new Helpers.Rectangle(500, 20, 100, 50);
            ButtonReset = new Helpers.Rectangle(500, 100, 100, 50);
            ResultBox = new Helpers.Rectangle(500, 180, 100, 100);
            BlockSize = WidthBox / NumBlock;
            Matrix = new float[NumBlock][];
            for(int i = 0; i < NumBlock; i++)
            {
                Matrix[i] = new float[NumBlock];
                for(var y = 0; y < NumBlock; y++)
                {
                    Matrix[i][y] = 0;
                }
            }
            var modelstr = Resources.GetString(Resources.StringResources.network);
            
            //enlarge heap
            //GHIElectronics.TinyCLR.Native.Memory.ExtendHeap();
            

            model = TinyNeuralNetwork.LoadFromString(ref modelstr);
            GpioPin backlight = GpioController.GetDefault().OpenPin(SC20260.GpioPin.PA15);
            backlight.SetDriveMode(GpioPinDriveMode.Output);
            backlight.Write(GpioPinValue.High);

            var displayController = DisplayController.GetDefault();

            // Enter the proper display configurations
            displayController.SetConfiguration(new ParallelDisplayControllerSettings
            {
                Width = 800,
                Height = 480,
                DataFormat = DisplayDataFormat.Rgb565,
                Orientation = DisplayOrientation.Degrees0, //Rotate display.
                PixelClockRate = 24000000,
                PixelPolarity = false,
                DataEnablePolarity = false,
                DataEnableIsFixed = false,
                HorizontalFrontPorch = 16,
                HorizontalBackPorch = 46,
                HorizontalSyncPulseWidth = 1,
                HorizontalSyncPolarity = false,
                VerticalFrontPorch = 7,
                VerticalBackPorch = 23,
                VerticalSyncPulseWidth = 1,
                VerticalSyncPolarity = false,
            });

            displayController.Enable(); //This line turns on the display I/O and starts
                                        //  refreshing the display. Native displays are
                                        //  continually refreshed automatically after this
                                        //  command is executed.

            screen = Graphics.FromHdc(displayController.Hdc);

            //var image = Resources.GetBitmap(Resources.BitmapResources.smallJpegBackground);
            font = Resources.GetFont(Resources.FontResources.small);
            
            Redraw();

            //touch

            var gpioController = GpioController.GetDefault();
            var i2cController = I2cController.FromName(SC20260.I2cBus.I2c1);
            var touch = new FT5xx6Controller(i2cController.GetDevice(FT5xx6Controller.GetConnectionSettings()), gpioController.OpenPin(SC20260.GpioPin.PJ14));
            touch.Orientation = FT5xx6Controller.TouchOrientation.Degrees0; //Rotate touch coordinates.
            touch.TouchMove += (_, e) => {
                if (DrawingBox.Contains(e.X, e.Y))
                {
                    var ax = (int)((e.X - DrawingBox.X) / BlockSize);
                    var ay = (int)((e.Y - DrawingBox.Y) / BlockSize);
                    if (ax >= NumBlock || ay >= NumBlock) return; 

                    Matrix[ax][ay] = 1;
                    Redraw();
                }
            };
            touch.TouchUp += (_, e) => {
                //button clicked
                if (ButtonRecognize.Contains(e.X, e.Y))
                {
                    var counter = 0;
                    //do inference
                    for (var y = 0; y < NumBlock; y++)
                    {
                        for (int x = 0; x < NumBlock; x++)
                        {
                            InputData[counter] = Matrix[x][y];
                            counter++;
                        }
                    }
                    var res = GetMaxIndex(model.Predict(InputData));
                    Redraw(res);
                }else
                if (ButtonReset.Contains(e.X, e.Y))
                {
                    Reset();
                    Redraw();
                }
                };
            touch.TouchDown += (_, e) => {
                if (DrawingBox.Contains(e.X, e.Y))
                {
                    var ax = (int) ((e.X - DrawingBox.X)/BlockSize);
                    var ay = (int) ((e.Y - DrawingBox.Y)/BlockSize);
                    Matrix[ax][ay] = 1;
                    Redraw();
                }
            };
            Thread.Sleep(Timeout.Infinite);
            //TrainingNN();
            //Thread.Sleep(-1);
        }
        static void Reset()
        {
            for (int i = 0; i < NumBlock; i++)
            {
                for (var y = 0; y < NumBlock; y++)
                {
                    Matrix[i][y] = 0;
                }
            }
        }
        static void Redraw(int drawResult=-1)
        {
            screen.Clear();

            //draw matrix
            screen.DrawRectangle(new Pen(Color.White), DrawingBox.X,DrawingBox.Y,DrawingBox.Width,DrawingBox.Height);
            for (int x = 0; x < NumBlock; x++)
            {
                for (var y = 0; y < NumBlock; y++)
                {
                    if (Matrix[x][y] > 0)
                    {
                        screen.FillRectangle(new SolidBrush(Color.Blue), DrawingBox.X + x*BlockSize, DrawingBox.Y+ y*BlockSize, BlockSize, BlockSize);
                    }
                }
            }
            //draw buttons
            screen.DrawRectangle(new Pen(Color.Yellow), ButtonRecognize.X, ButtonRecognize.Y, ButtonRecognize.Width, ButtonRecognize.Height);
            screen.DrawTextInRect("RECOGNIZE", ButtonRecognize.X, ButtonRecognize.Y, ButtonRecognize.Width, ButtonRecognize.Height, Graphics.DrawTextAlignment.AlignmentCenter, Color.Yellow, font);
 
            screen.DrawRectangle(new Pen(Color.Green), ButtonReset.X, ButtonReset.Y, ButtonReset.Width, ButtonReset.Height);
            screen.DrawTextInRect("RESET", ButtonReset.X, ButtonReset.Y, ButtonReset.Width, ButtonReset.Height, Graphics.DrawTextAlignment.AlignmentCenter, Color.Green, font);
            
            screen.DrawString("RESULT:", font, new SolidBrush(Color.White), ResultBox.X,ResultBox.Y-10);

            //draw result
            if (drawResult>-1)
            {
                screen.DrawString(drawResult.ToString(), font, new SolidBrush(Color.White), new RectangleF(ResultBox.X, ResultBox.Y, ResultBox.Width, ResultBox.Height));
            }
            screen.Flush();
        }
        #region Neural Network
        // Used for shuffling data set in between training iterations.
        static void Shuffle(DataTraining[] array)
        {
            var random = new Random(0);

            for (int i = 0; i < array.Length; i++)
            {
                var j = random.Next(array.Length);
                var tmp = array[i];
                array[i] = array[j];
                array[j] = tmp;
                //(array[i], array[j]) = (array[j], array[i]);
            }
        }

        // Used for transforming categorical values to numeric.
        static int GetMaxIndex(float[] values)
        {
            var maxValue = float.MinValue;
            var maxValueIndex = -1;
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i] >= maxValue)
                {
                    maxValue = values[i];
                    maxValueIndex = i;
                }
            }

            return maxValueIndex;
        }
        static void TrainingNN()
        {
            /*
            #region Get dataset
            if (!File.Exists(dataSetFileName))
            {
                Debug.WriteLine("Downloading MNIST dataset...");
                using var webClient = new WebClient();
                webClient.DownloadFile(datasetUri, dataSetFileName);
                Debug.WriteLine("Download completed.");
            }
            #endregion
            */

            #region Read dataset
            var sd = StorageController.FromName(SC20100.StorageController.SdCard);
            var drive = FileSystem.Mount(sd.Hdc);

            //Show a list of files in the root directory
            var directory = new DirectoryInfo(drive.Name);
            var files = directory.GetFiles();

            foreach (var f in files)
            {
                System.Diagnostics.Debug.WriteLine(f.Name);
            }
            var lineStr = TinyNeuralNetwork.ReadAllLines(dataSetFileName);
            var data = new DataTraining[lineStr.Length];
            int counter = 0;
            int row = 0;
            var tempInput = new ArrayList();
            var tempOutput = new ArrayList();
            foreach (var line in lineStr)
            {
                counter = 0;
                var splitted = line.Split(' ');
                tempInput.Clear();
                tempOutput.Clear();
                foreach (var item in splitted)
                {
                    if (counter < inputCount)
                    {
                        tempInput.Add(double.Parse(item));
                    }
                    else
                    {
                        tempOutput.Add(double.Parse(item));
                    }
                    counter++;
                }
                data[row] = new DataTraining()
                {
                    Input = (float[])tempInput.ToArray(typeof(double))
            ,
                    Output = (float[])tempOutput.ToArray(typeof(double))
                };
                row++;
            }
           
            #endregion

            #region Train neural network
            var network = new TinyNeuralNetwork(inputCount, hiddentCount, outputCount);
            var progress = new ProgressBar(learningIterations * data.Length, "Training...");

            for (var i = 0; i < learningIterations; i++)
            {
                foreach (var record in data)
                {
                    progress.Tick();
                    network.Train(record.Input, record.Output, learningRate);
                }

                Shuffle(data);
                learningRate *= learningRateDecay;
            }
            #endregion

            #region Test neural network
           
            var predictedNumbers = new int[data.Length];
            counter = 0;
            foreach (var item in data)
            {
                var res = GetMaxIndex(network.Predict(item.Input));
                predictedNumbers[counter] = res;
                counter++;
            }

            var actualNumbers = new int[data.Length];
            counter = 0;
            foreach (var item in data)
            {
                var res = GetMaxIndex(item.Output);
                actualNumbers[counter] = res;
                counter++;

            }

            var correctlyGuessed = 0;
            for (int i = 0; i < predictedNumbers.Length; i++)
            {
                if (predictedNumbers[i] == actualNumbers[i])
                {
                    correctlyGuessed++;
                }
            }
            var accuracy = (float)correctlyGuessed / actualNumbers.Length;
            //Debug.Clear();
            Debug.WriteLine($"Achieved {accuracy:P2} accuracy.");
            #endregion
        }
        #endregion

    }
    public class DataTraining
    {
        public float[] Input { get; set; }
        public float[] Output { get; set; }
    }
    class ProgressBar
    {
        int MaxTicks;
        int currentTick = 0;
        public ProgressBar(int maxTick, string message)
        {
            this.MaxTicks = maxTick;
        }

        public void Reset()
        {
            currentTick = 0;
        }

        public void Tick()
        {
            if (currentTick < MaxTicks)
                currentTick++;
            Debug.WriteLine($"current progress: {string.Format("{0:n2}", currentTick / MaxTicks)}");
        }

    }
}