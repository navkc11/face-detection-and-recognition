﻿using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using System.IO;
using System.Windows;
using System.ComponentModel;
using System.Timers;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Runtime.CompilerServices;
using Emgu.CV.Face;
using Emgu.CV.Util;
using Microsoft.Win32;
using FireSharp.Interfaces;
using FireSharp.Config;
using FireSharp.Response;


namespace FaceDetectionAndRecognition
{


    public partial class WFFaceRecognition : Window, INotifyPropertyChanged
    {
        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;
        private VideoCapture videoCapture;
        private CascadeClassifier haarCascade;
        private Image<Bgr, Byte> bgrFrame = null;
        private Image<Gray, Byte> detectedFace = null;
        private List<FaceData> faceList = new List<FaceData>();
        private VectorOfMat imageList = new VectorOfMat();
        private List<string> nameList = new List<string>();
        private VectorOfInt labelList = new VectorOfInt();

        private EigenFaceRecognizer recognizer;
        private Timer captureTimer;
        #region FaceName
        private string faceTz;
        public string FaceTz
        {
            get { return faceTz; }
            set


            {   /*פה אנחנו עושים בדיקה אם פנים תז לא מופיע אצלו את שתיי המשפטים 
                אז תלך לפייר בייס ותימצא לפי התעודת זהות שיש ברשימה את השם שנימצא עם התעודת זהות
                שמצא לפי הפנים וכך מקבלים את אותו תעודת זהות*/
                faceTz = value; // value - ערך משורה 285 FaceTz = nameList[result.Label]
                if (faceTz == "Please Add Face" || faceTz == "No face detected")
                {
                    lblFaceName.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { lblFaceName.Content = faceTz; }));
                    NotifyPropertyChanged();
                }
                else
                {
                    var result = client.Get("User/" + faceTz);
                    FaceData user = result.ResultAs<FaceData>();
                    lblFaceName.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { lblFaceName.Content = user.PersonName; }));
                    NotifyPropertyChanged();//מוודא שלא הוכנס ערך NULL
                }
            }
        }
        #endregion
        #region CameraCaptureImage
        private Bitmap cameraCapture;
        public Bitmap CameraCapture
        {
            get { return cameraCapture; }
            set
            {
                cameraCapture = value;
                imgCamera.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { imgCamera.Source = BitmapToImageSource(cameraCapture); }));
                NotifyPropertyChanged();
            }
        }
        #endregion
        #region CameraCaptureFaceImage
        private Bitmap cameraCaptureFace;
        public Bitmap CameraCaptureFace
        {
            get { return cameraCaptureFace; }
            set
            {
                cameraCaptureFace = value;
                imgDetectFace.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => { imgDetectFace.Source = BitmapToImageSource(cameraCaptureFace); }));
                // imgCamera.Source = BitmapToImageSource(cameraCapture);
                NotifyPropertyChanged();
            }
        }
        #endregion
        #endregion

        #region Constructor
        public WFFaceRecognition()
        {
            InitializeComponent();
            captureTimer = new Timer()
            {
                Interval = Config.TimerResponseValue

            };
            captureTimer.Elapsed += CaptureTimer_Elapsed;
        }//כאן אנו מקשריים את הפייר בייס
        IFirebaseConfig fcon = new FirebaseConfig()
        {
            AuthSecret = "", //פה כותבים את ה secret שלכם
            BasePath = "" //קישור לפייר בייס
        };

        IFirebaseClient client;
        #endregion

        #region Event
        protected virtual void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                client = new FireSharp.FirebaseClient(fcon);
            }
            catch
            {
                MessageBox.Show("There's a problem");
            }
            GetFacesList();
            videoCapture = new VideoCapture(Config.ActiveCameraIndex);
            videoCapture.SetCaptureProperty(CapProp.Fps, 30);
            videoCapture.SetCaptureProperty(CapProp.FrameHeight, 450);
            videoCapture.SetCaptureProperty(CapProp.FrameWidth, 370);
            captureTimer.Start();
        }
        private void CaptureTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ProcessFrame();
        }
        private void EditName_Click(object sender, RoutedEventArgs e)
        {
            WFAbout wfAbout = new WFAbout();
            wfAbout.ShowDialog();
        }
        private void NewFaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (detectedFace == null)
            {
                MessageBox.Show("No face detected.");
                return;
            }
            //Save detected face
            detectedFace = detectedFace.Resize(100, 100, Inter.Cubic);
            detectedFace.Save(Config.FacePhotosPath + "face" + (faceList.Count + 1) + Config.ImageFileExtension);
            StreamWriter writer = new StreamWriter(Config.FaceListTextFile, true);//מוודא שיש קובץ טקסט עם השות ואם לא הוא מייצר אותו
            string personName = Microsoft.VisualBasic.Interaction.InputBox("Your Name");//קולט ושומר את שם המשתמש בתור סטרינג
            string tz = Microsoft.VisualBasic.Interaction.InputBox("Your Teudat Zehut");//קולט ושומר את ת.ז של המשתמש בתור סטרינג
            writer.WriteLine(String.Format("face{0}:{1}", (faceList.Count + 1), tz));// כותב למעבד תמלילים את מספר הפנים ואת הת.ז הרלוונטי
            writer.Close(); // סוגר את מעבד התמלילים
            FaceData user = new FaceData()
            {
                TZ = tz,
                PersonName = personName,
            };
            var setter = client.Set("User/" + String.Format(tz), user); // מעלה את נתוני המשתמש לפייר בייס (שומר לפי ת.ז) י
            GetFacesList();
            MessageBox.Show("Successful.");
        }
        private void OpenVideoFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            if (openDialog.ShowDialog().Value == true)
            {
                captureTimer.Stop();
                videoCapture.Dispose();

                videoCapture = new VideoCapture(openDialog.FileName);
                captureTimer.Start();
                this.Title = openDialog.FileName;
                return;
            }
        }
        #endregion

        #region Method
        public void GetFacesList()
        {
            //haar cascade classifier
            if (!File.Exists(Config.HaarCascadePath))
            {
                string text = "Cannot find Haar cascade data file:\n\n";
                text += Config.HaarCascadePath;
                MessageBoxResult result = MessageBox.Show(text, "Error",
                       MessageBoxButton.OK, MessageBoxImage.Error);
            }

            haarCascade = new CascadeClassifier(Config.HaarCascadePath);
            faceList.Clear();
            string line;
            FaceData faceInstance = null;

            // Create empty directory / file for face data if it doesn't exist
            if (!Directory.Exists(Config.FacePhotosPath))
            {
                Directory.CreateDirectory(Config.FacePhotosPath);
            }

            if (!File.Exists(Config.FaceListTextFile))
            {
                string text = "Cannot find face data file:\n\n";
                text += Config.FaceListTextFile + "\n\n";
                text += "If this is your first time running the app, an empty file will be created for you.";
                MessageBoxResult result = MessageBox.Show(text, "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                switch (result)
                {
                    case MessageBoxResult.OK:
                        String dirName = Path.GetDirectoryName(Config.FaceListTextFile);
                        Directory.CreateDirectory(dirName);
                        File.Create(Config.FaceListTextFile).Close();
                        break;
                }
            }

            StreamReader reader = new StreamReader(Config.FaceListTextFile);
            int i = 0;
            while ((line = reader.ReadLine()) != null)
            {
                string[] lineParts = line.Split(':');
                faceInstance = new FaceData();
                faceInstance.FaceImage = new Image<Gray, byte>(Config.FacePhotosPath + lineParts[0] + Config.ImageFileExtension);
                faceInstance.TZ = lineParts[1];
                faceList.Add(faceInstance);
            }
            foreach (var face in faceList)
            {
                imageList.Push(face.FaceImage.Mat);
                nameList.Add(face.TZ);
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

        private void ProcessFrame()
        {
            bgrFrame = videoCapture.QueryFrame().ToImage<Bgr, Byte>();

            if (bgrFrame != null)
            {
                try
                {//for emgu cv bug
                    Image<Gray, byte> grayframe = bgrFrame.Convert<Gray, byte>();

                    Rectangle[] faces = haarCascade.DetectMultiScale(grayframe, 1.2, 10, new System.Drawing.Size(50, 50), new System.Drawing.Size(200, 200));

                    //detect face
                    FaceTz = "No face detected";
                    foreach (var face in faces)
                    {
                        bgrFrame.Draw(face, new Bgr(255, 255, 0), 2);
                        detectedFace = bgrFrame.Copy(face).Convert<Gray, byte>();
                        FaceRecognition();
                        break;
                    }
                    CameraCapture = bgrFrame.ToBitmap();
                }
                catch (Exception ex)
                {

                    //todo log
                }

            }
        }


        private void FaceRecognition()
        {
            if (imageList.Size != 0)
            {
                //Eigen Face Algorithm
                FaceRecognizer.PredictionResult result = recognizer.Predict(detectedFace.Resize(100, 100, Inter.Cubic)); //מגדיר משתנה ששווה לפנים שזוהו
                FaceTz = nameList[result.Label]; //פה מכניסים למשתנה את הת.ז מהרשימה ונכנסים לפונקציה (nameList[result.Label]) 
                CameraCaptureFace = detectedFace.ToBitmap();
            }
            else
            {
                FaceTz = "Please Add Face";
            }
        }
        /// <summary>
        /// Convert bitmap to bitmap image for image control
        /// </summary>
        /// <param name="bitmap">Bitmap image</param>
        /// <returns>Image Source</returns>
        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        #endregion


    }
}
