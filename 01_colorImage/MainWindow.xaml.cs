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

namespace colorImage
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 定义kinect设备
        KinectSensor myKinect;
        byte[] image;
        public MainWindow()
        {
            InitializeComponent();
        }

        // **************************************************
        // 定义窗口加载程序
        // **************************************************
        private void win_load(object sender, RoutedEventArgs e)
        {
            // 初始化kinect对象
            myKinect = (from sensor in KinectSensor.KinectSensors
                        where sensor.Status == KinectStatus.Connected
                        select sensor).FirstOrDefault();

            // 使能彩色图
            myKinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            // 开启
            myKinect.Start();

            // 委托彩色帧处理事件
            myKinect.ColorFrameReady += color_ready;
        }

        // **************************************************
        // 定义窗口退出程序
        // **************************************************
        private void win_close(object sender, EventArgs e)
        {
            myKinect.Stop();
        }

        void color_ready(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imgframe = e.OpenColorImageFrame())
            {
                if (imgframe != null)
                {
                    image = new byte[imgframe.PixelDataLength]; // 为彩色数据分配内存空间
                    imgframe.CopyPixelDataTo(image);
                    img.Source = BitmapSource.Create(imgframe.Width, imgframe.Height, 96, 96, PixelFormats.Bgr32, null, image, imgframe.Width * imgframe.BytesPerPixel);
                }
            }
        }

    }
}
