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
using System.IO;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;

namespace skeletonViewer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 定义全局变量
        KinectSensor myKinect;      // kinect对象
        byte[] imagePiexel;         // 一块内存，用于存储一帧图片数据
        Skeleton[] skeletonData;    // 用于存放kinect检测到的骨架数据

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        ///  加载窗口，这个函数在 WPF 窗口刚生成的时候调用，用于初始化一些操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wndLoad(object sender, RoutedEventArgs e)
        {
            // 初始化kinect对象，选择电脑上的一个kinect
            myKinect = (from sensor in KinectSensor.KinectSensors
                        where sensor.Status == KinectStatus.Connected
                        select sensor).FirstOrDefault();
            // 彩色图像流
            myKinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);    // 开启采集彩色图片帧的功能
            myKinect.ColorFrameReady += color_ready;        // 如果kinect采集到一帧彩色帧，那么执行 color_ready() 函数

            // 骨架图像流
            myKinect.SkeletonStream.Enable();                                           // 开启采集骨架数据帧的功能
            myKinect.SkeletonFrameReady += skeleton_ready;  // 如果kinect采集到一帧骨架，那么执行 skeleton_ready() 函数
            //myKinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;         // 把kinect修改成半身模式，默认为全身模式


            // 开启 kinect
            myKinect.Start();
        }

        /// <summary>
        ///  销毁窗口，这个函数在WPF窗口要关闭的时候执行，用于执行一些销毁操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void wndClose(object sender, EventArgs e)
        {
            myKinect.Stop();    // 关闭程序前，先关闭kinect
            System.Environment.Exit(System.Environment.ExitCode);   // 强制退出进程
        }

        /// <summary>
        /// 当kinect准备好一帧彩色数据之后，触发这个函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void color_ready(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imgframe = e.OpenColorImageFrame()) // 获取彩色帧数据
            {
                if (imgframe != null)   // 如果彩色帧是非空的
                {
                    imagePiexel = new byte[imgframe.PixelDataLength]; // 为imagePixel 这个变量分配内存空间
                    imgframe.CopyPixelDataTo(imagePiexel);            // 把imgframe的内容复制到imagePixel变量中
                    // 把彩色图像数据，显示到WPF窗口上
                    colorImage.Source = BitmapSource.Create(imgframe.Width, imgframe.Height, 96, 96, PixelFormats.Bgr32, null, imagePiexel, imgframe.Width * imgframe.BytesPerPixel);
                }
            }
        }

        /// <summary>
        /// 当kinect准备好一帧骨架数据之后，触发这个函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void skeleton_ready(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame()) // 获取骨架帧
            {
                if (skeletonFrame != null)  // 如果这个骨架帧非空
                {
                    // 把骨架帧中的数据，复制到 skeletonData 变量中
                    skeletonData = new Skeleton[myKinect.SkeletonStream.FrameSkeletonArrayLength];  
                    skeletonFrame.CopySkeletonDataTo(skeletonData);
                    // 摄像头中可能出现两人个，只取其中一个
                    Skeleton skeleton = (from s in skeletonData
                                         where s.TrackingState == SkeletonTrackingState.Tracked
                                         select s).FirstOrDefault();
                    if (skeleton != null)   // 如果骨架帧非空
                    {
                        // 遍历这个骨架中的所有的关节结点
                        foreach (Joint joint in skeleton.Joints)
                        {
                            byte[] buffer = new byte[50];
                            switch (joint.JointType)    // 分析这个关节点的类型
                            {
                                case JointType.Head:
                                    setPointPosition(headPoint, joint);
                                    break;
                                case JointType.ShoulderLeft:
                                    setPointPosition(shoulderleftPoint, joint);
                                    break;
                                case JointType.ShoulderRight:
                                    setPointPosition(shoulderrightPoint, joint);
                                    break;
                                case JointType.ShoulderCenter:
                                    setPointPosition(shouldercenterPoint, joint);
                                    break;
                                case JointType.ElbowRight:
                                    setPointPosition(elbowrightPoint, joint);
                                    break;
                                case JointType.ElbowLeft:
                                    setPointPosition(elbowleftPoint, joint);
                                    break;
                                case JointType.WristRight:
                                    setPointPosition(wristrightPoint, joint);
                                    break;
                                case JointType.WristLeft:
                                    setPointPosition(wristleftPoint, joint);
                                    break;
                                case JointType.HandLeft:
                                    setPointPosition(handleftPoint, joint);
                                    break;
                                case JointType.HandRight:
                                    setPointPosition(handrightPoint, joint);
                                    break;
                                case JointType.Spine:
                                    setPointPosition(spinePoint, joint);
                                    break;
                                case JointType.HipCenter:
                                    setPointPosition(hipcenterPoint, joint);
                                    break;
                                case JointType.HipLeft:
                                    setPointPosition(hipleftPoint, joint);
                                    break;
                                case JointType.HipRight:
                                    setPointPosition(hiprightPoint, joint);
                                    break;
                                case JointType.KneeLeft:
                                    setPointPosition(kneeleftPoint, joint);
                                    break;
                                case JointType.KneeRight:
                                    setPointPosition(kneerightPoint, joint);
                                    break;
                                case JointType.AnkleLeft:
                                    setPointPosition(ankleleftPoint, joint);
                                    break;
                                case JointType.AnkleRight:
                                    setPointPosition(anklerightPoint, joint);
                                    break;
                                case JointType.FootLeft:
                                    setPointPosition(footleftPoint, joint);
                                    break;
                                case JointType.FootRight:
                                    setPointPosition(footrightPoint, joint);
                                    break;
                            }


                        }//外if
                    }//using
                }//类的
            }
        }

        /// <summary>
        ///  把某一个关节点，显示到 colorImage 上
        /// </summary>
        /// <param name="ellipse"> XAML 文件中 Canvas 控件上，颜色图中表示关节点的圆</param>
        /// <param name="joint">骨架上的关节点</param>
        private void setPointPosition(FrameworkElement ellipse, Joint joint)
        {
            ColorImagePoint colorImagePoint = myKinect.CoordinateMapper.MapSkeletonPointToColorPoint(joint.Position, ColorImageFormat.RgbResolution640x480Fps30);
            Canvas.SetLeft(ellipse, colorImagePoint.X);
            Canvas.SetTop(ellipse, colorImagePoint.Y);
        }

        /// <summary>
        ///  让kinect抬头
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (myKinect.ElevationAngle == myKinect.MaxElevationAngle)
            {
                //myKinect.ElevationAngle -= 5;
            }
            else 
            {
                if (myKinect.ElevationAngle + 5 >= myKinect.MaxElevationAngle) myKinect.ElevationAngle = myKinect.MaxElevationAngle;
                else
                {
                    myKinect.ElevationAngle += 5;
                }
            }

        }

        /// <summary>
        ///  让kinect低头
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (myKinect.ElevationAngle == myKinect.MinElevationAngle)
            {
                //myKinect.ElevationAngle -= 5;
            }
            else 
            {
                if (myKinect.ElevationAngle - 5 <= myKinect.MinElevationAngle) myKinect.ElevationAngle = myKinect.MaxElevationAngle;
                else
                {
                    myKinect.ElevationAngle -= 5;
                }
            }
        }
    }
}