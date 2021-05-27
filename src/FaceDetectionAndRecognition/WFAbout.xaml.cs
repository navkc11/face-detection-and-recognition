using System.Windows;
using FireSharp.Interfaces;
using FireSharp.Config;
using FireSharp.Response;
using System;

namespace FaceDetectionAndRecognition
{
    /// <summary>
    /// Interaction logic for WFAbout.xaml
    /// </summary>
    public partial class WFAbout : Window
    {
        public WFAbout()
        {
            InitializeComponent();
            Loaded += WFAbout_Loaded;
        }

        private void WFAbout_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                client = new FireSharp.FirebaseClient(fcon);
            }
            catch
            {
                MessageBox.Show("There's a problem");
            }
        }

        IFirebaseConfig fcon = new FirebaseConfig()
        {
            AuthSecret = "yfAOROgj22FDDAsmRD8b5BeFWUBr95oiOkPfTsut",
            BasePath = "https://face-detection-1fc7d-default-rtdb.firebaseio.com/"
        };

        IFirebaseClient client;


        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            FaceData user = new FaceData()
            {
                TZ = txtTZ.Text,
                PersonName = txtNewName.Text,
            };

            var setter = client.Update("User/" + txtTZ.Text.ToString(), user);
            MessageBox.Show("Data Updated Succefully");
            Close();
        }
    }
}
