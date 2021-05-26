using Emgu.CV;
using Emgu.CV.Structure;
using System;

namespace FaceDetectionAndRecognition
{
    class FaceData
    {
        public string TZ { get; set; }
        public string PersonName { get; set; }
        public Image<Gray, byte> FaceImage { get; set; }      
    }
}
