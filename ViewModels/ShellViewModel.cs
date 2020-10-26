using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using JetBrains.Annotations;
using Microsoft.Win32;
using Microsoft.Xaml.Behaviors;
using Prism.Commands;
using Prism.Mvvm;
using Microsoft.VisualBasic;
using Interaction = Microsoft.VisualBasic.Interaction;

namespace FaceRecognition.ViewModels
{
    [UsedImplicitly]
    public class ShellViewModel : BindableBase
    {
        private  Config config;
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private VideoCapture videoCapture;
        private CascadeClassifier haarCascade;
        private Image<Bgr, byte> bgrFrame;
        private string faceName;
        private string title;
        private Image<Gray, byte> detectedFace = null;
        private VectorOfMat imageList = new VectorOfMat();
        private List<string> nameList = new List<string>();
        private VectorOfInt labelList = new VectorOfInt();
        private EigenFaceRecognizer recognizer;
        private List<FaceData> faceList = new List<FaceData>();

        private Bitmap cameraCapture;
        public Bitmap CameraCapture
        {
            get { return cameraCapture; }
            set
            {
                cameraCapture = value;

                DisplayedImage = Convert(cameraCapture);
                

            }
        }
        public BitmapImage Convert(Image img)
        {
            using (var memory = new MemoryStream())
            {
                img.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        } 



        private Bitmap cameraCaptureFace;
        public Bitmap CameraCaptureFace
        {
            get { return cameraCaptureFace; }
            set
            {
                cameraCaptureFace = value;
             
                

            }
        }



        public ShellViewModel(Config config)
        {
            this.config = config;
            Title = "Face Recognition";
            timer.Interval = TimeSpan.FromMilliseconds(config.TimerResponseValue);
            timer.Tick += (sender, args) =>
            {
                ProcessFrame();
            };

            if (imageList.Size != 0)
            {
                //Eigen Face Algorithm
                FaceRecognizer.PredictionResult result = recognizer.Predict(detectedFace.Resize(100, 100, Inter.Cubic));
                FaceName = nameList[result.Label];
                cameraCaptureFace = detectedFace.ToBitmap();
            }
            else
            {
                FaceName = "Please Add Face";
                detectedFace = null;
            }
          


            GetFacesList(config);
            videoCapture = new VideoCapture(config.ActiveCameraIndex);
            videoCapture.SetCaptureProperty(CapProp.Fps, 30);
            videoCapture.SetCaptureProperty(CapProp.FrameHeight, 450);
            videoCapture.SetCaptureProperty(CapProp.FrameWidth, 370);
            timer.Start();
        }

        private void GetFacesList(Config config1)
        {
           
            if (!File.Exists(config1.HaarCascadePath))
            {
                var text = "Cannot find Haar cascade data file:\n\n";
                text += config1.HaarCascadePath;
                MessageBox.Show(
                    text,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            
            haarCascade = new CascadeClassifier(config1.HaarCascadePath);
            faceList.Clear();
            string line;
            FaceData faceInstance = null;

            if (!Directory.Exists(config1.FacePhotoPath))
            {
                Directory.CreateDirectory(config1.FacePhotoPath);
            }

            if (!File.Exists(config1.FaceListTextFile))
            {
                string text = "Yüz Veri klosörü bulunamadı:\n\n";
                text += config1.FaceListTextFile + "\n\n";
                text += "Uygulamayı ilk kez çalıştırdınız, sizin için boş bir dosya oluşturulacaktır.";
                MessageBoxResult result = MessageBox.Show(text, "Uyarı",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                switch (result)
                {
                    case MessageBoxResult.OK:
                        String dirName = Path.GetDirectoryName(config1.FaceListTextFile);
                        Directory.CreateDirectory(dirName);
                        File.Create(config1.FaceListTextFile).Close();
                        break;
                }
            }
            StreamReader reader = new StreamReader(config1.FaceListTextFile);
            int i = 0;
            while ((line = reader.ReadLine()) != null)
            {
                string[] lineParts = line.Split(':');
                faceInstance = new FaceData();
                faceInstance.FaceImage = new Image<Gray, byte>(config1.FacePhotoPath + lineParts[0] + config1.ImageFileExtension);
                faceInstance.PersonName = lineParts[1];
                faceList.Add(faceInstance);
            }
            foreach (var face in faceList)
            {
                imageList.Push(face.FaceImage.Mat);
                nameList.Add(face.PersonName);
                labelList.Push(new[] { i++ });
            }
            reader.Close();

            // Train recogniser
            if (imageList.Size > 0)
            {
                recognizer = new EigenFaceRecognizer(imageList.Size);
                recognizer.Train(imageList, labelList);
            }
        }

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public string FaceName
        {
            get => faceName;
            set => SetProperty(ref faceName, value);
        }

        public DelegateCommand OpenViewFileCommand => new DelegateCommand(OnOpenViewFileCommand);
        public DelegateCommand AddFaceCommand => new DelegateCommand(OnAddFace);

        private void ProcessFrame()
        {
            bgrFrame = videoCapture.QueryFrame().ToImage<Bgr, byte>();

            if (bgrFrame == null)
            {
                return;
            }

            try
            {
                var grayFrame = bgrFrame.Convert<Gray, byte>();
                var faces = haarCascade.DetectMultiScale(grayFrame , 1.2 , 10, new System.Drawing.Size(50, 50), new System.Drawing.Size(200, 200));

                FaceName = "No face detected";
                foreach (var face in faces)
                {
                    bgrFrame.Draw(face, new Bgr(255, 255, 0), 2);
                    detectedFace = bgrFrame.Copy(face).Convert<Gray, byte>();
                    FaceRecognition();
                    break;
                }
                CameraCapture = bgrFrame.ToBitmap();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private void OnOpenViewFileCommand()
        {
            throw new NotImplementedException();
        }

        private void OnAddFace()
        {
            if (detectedFace == null)
            {
                MessageBox.Show("No face detected.");
                return;
            }
          
            AddNewFace(config);
         
        }

        public void AddNewFace(Config config)
        {
            detectedFace = detectedFace.Resize(100, 100, Inter.Cubic);


            
            detectedFace.Save(config.FacePhotoPath + "face" + (faceList.Count + 1) + config.ImageFileExtension);
            StreamWriter writer = new StreamWriter(config.FaceListTextFile, true);

            string personName = Interaction.InputBox("İsim" , "Yeni Yüz Ekle");
            writer.WriteLine(String.Format("face{0}:{1}", (faceList.Count + 1), personName));
            writer.Close();
            GetFacesList(config);
            MessageBox.Show("Successful.");
        }
        private void FaceRecognition()
        {
            if (imageList.Size != 0)
            {
                //Eigen Face Algorithm
                FaceRecognizer.PredictionResult result = recognizer.Predict(detectedFace.Resize(100, 100, Inter.Cubic));
                FaceName = nameList[result.Label];
                CameraCapture = detectedFace.ToBitmap();
            }
            else
            {
                FaceName = "Please Add Face";
            }
        }

        private BitmapImage displayedImage;
        public BitmapImage DisplayedImage
        {
            get => displayedImage;
            set => SetProperty(ref displayedImage, value);
        }
        private BitmapImage displayedImageFace;
        public BitmapImage DisplayedImageFace
        {
            get => displayedImageFace;
            set => SetProperty(ref displayedImageFace, value);
        }


    }
}