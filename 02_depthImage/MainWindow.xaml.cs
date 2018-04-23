using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace depthImage
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor myKinect;
        short[] pixelData;

        public MainWindow()
        {
            InitializeComponent();
        }
        private void win_load(object sender, RoutedEventArgs e)
        {
            // init
            myKinect = (from sensor in KinectSensor.KinectSensors
                        where sensor.Status == KinectStatus.Connected
                        select sensor).FirstOrDefault();

            myKinect.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            myKinect.Start();
            myKinect.DepthFrameReady += depth_ready;

        }

        private void win_close(object sender, EventArgs e)
        {
            myKinect.Stop();
        }

        void depth_ready(object sensor, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthImage = e.OpenDepthImageFrame())
            {
                if (depthImage != null)
                {
                    pixelData = new short[depthImage.PixelDataLength];
                    depthImage.CopyPixelDataTo(pixelData);
                    depthshow.Source = BitmapSource.Create(depthImage.Width, depthImage.Height, 96, 96, PixelFormats.Gray16, null, pixelData, depthImage.Width * depthImage.BytesPerPixel);
                }
            }
        }
    }

}
