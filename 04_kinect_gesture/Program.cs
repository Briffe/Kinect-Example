/// ****************************************
/// Detect the gesture with Kinect
/// Author: lilei
/// Date: 2018-4-15
/// ****************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Collections;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Microsoft.Kinect;

namespace kinect_gesture
{
    class ActionRecognize
    {
        int start_count = 0;   // 连续100个开始，会真正开启
        int stable_statue = 0; // 稳定状态 0==>散乱
        Queue trail = new Queue();

        Point meanpt(Point pt1, Point pt2)
        {
            return new Point((pt1.X + pt2.X) / 2, (pt1.Y + pt2.Y) / 2);
        }

        double euclidean(Point pt1, Point pt2)
        {
            return Math.Sqrt(Math.Pow(pt1.X - pt2.X, 2) + Math.Pow(pt1.Y - pt2.Y, 2));
        }
        public int get_statue(Point[] bodypt)
        {
            if (stable_statue == 0) 
            {
                if (euclidean(bodypt[0], bodypt[1]) < 10) start_count++;
                else start_count = 0;
                if (start_count >= 30) stable_statue = 1;
            }
            if(stable_statue == 1)
            {
                trail.Enqueue(meanpt(bodypt[0],bodypt[1]));
                if(bodypt[1].X<bodypt[2].X && bodypt[1].Y < bodypt[2].Y)
                {
                    stable_statue = 2;
                }
            }
            return stable_statue;
        }
        public Mat draw_trail(Mat img)
        {
            Point lastpt = new Point(0,0);
            foreach (Point pt in trail)
            {
                CvInvoke.Circle(img, pt, 2, new MCvScalar(1, 0, 0), -1);
                if(lastpt == new Point(0, 0))
                {
                    lastpt = pt;
                }
                else
                {
                    CvInvoke.Line(img, lastpt, pt, new MCvScalar(0, 0, 1), 2);
                    lastpt = pt;
                }
            }
            return img;
        }
    }

    class Program
    {
        static KinectSensor myKinect;
        static ActionRecognize actRecog = new ActionRecognize();
        static void Main(string[] args)
        {
            // Mat img_color = new Mat();
            // Mat img_depth = new Mat();
            // img_color.Create(480, 640, DepthType.Cv8U, 3);
            // img_depth.Create(240, 320, DepthType.Cv16U, 1);

            // 初始化kinect对象
            myKinect = (from sensor in KinectSensor.KinectSensors
                        where sensor.Status == KinectStatus.Connected
                        select sensor).FirstOrDefault();

            myKinect.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);// 使能彩色图
            myKinect.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);   // 使能深度图
            TransformSmoothParameters smooth = new TransformSmoothParameters
            {
                Smoothing = 0.8f,
                Correction = 0.2f,
                Prediction = 0.5f,
                JitterRadius = 0.05f,
                MaxDeviationRadius = 0.1f
            };
            myKinect.SkeletonStream.Enable(smooth);                                 // 使能骨架流
            myKinect.Start();// 开启 kinect
            myKinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;

            for (int i = 0; i < 2000; i++)
            {
                // 轮询数据
                ColorImageFrame frame_color;
                DepthImageFrame frame_depth;
                SkeletonFrame frame_skeleton;
                while (true) {
                    frame_color = myKinect.ColorStream.OpenNextFrame(5);
                    if (frame_color != null) break;
                }
                while (true) {
                    frame_depth = myKinect.DepthStream.OpenNextFrame(5);
                    if (frame_depth != null) break;
                }
                while (true){
                    frame_skeleton = myKinect.SkeletonStream.OpenNextFrame(5);
                    if (frame_skeleton != null) break;
                }

                // 数据处理
                Mat img_color = color_frame2mat(frame_color);
                Mat img_depth = depth_frame2mat(frame_depth);
                Point[] bodypt = skeleton_frame2point(frame_skeleton);
                if (bodypt != null)
                {
                    img_color = draw_bodypt(img_color, bodypt);
                    int statue = actRecog.get_statue(bodypt);
                    if (statue == 1)
                    {
                        CvInvoke.PutText(img_color, "START", new Point(50, 50), FontFace.HersheySimplex, 1, new MCvScalar(0,0,255), 3);
                    }
                    img_color = actRecog.draw_trail(img_color);
                }
                CvInvoke.Imshow("color", img_color);
                CvInvoke.Imshow("depth", img_depth);
                if (CvInvoke.WaitKey(2) == 81) break;
            }

            myKinect.Stop();// 关闭 kinect
        }

        static Mat color_frame2mat(ColorImageFrame frame)
        {
            byte[] img_rgba = new byte[frame.PixelDataLength];
            frame.CopyPixelDataTo(img_rgba);
            byte[] img_rgb = new byte[frame.Width * frame.Height * 3];
            for (int i = 0; i < frame.Width * frame.Height; i++)
            {
                img_rgb[i * 3] = img_rgba[i * 4];
                img_rgb[i * 3 + 1] = img_rgba[i * 4 + 1];
                img_rgb[i * 3 + 2] = img_rgba[i * 4 + 2];
            }
            Mat img = new Mat();
            img.Create(frame.Height, frame.Width, DepthType.Cv8U, 3);
            img.SetTo(img_rgb);
            return img;
        }
        static Mat depth_frame2mat(DepthImageFrame frame)
        {
            short[] data = new short[frame.PixelDataLength];
            frame.CopyPixelDataTo(data);
            //CvInvoke.WaitKey();
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] < 0) data[i] = 0;
                if (data[i] > 2000 * 8) data[i] = 0;
            }
            Mat img = new Mat();
            img.Create(frame.Height, frame.Width, DepthType.Cv16U, 1);
            img.SetTo(data);
            Mat img1 = new Mat();
            //CvInvoke.Threshold(img, img1, 200, 255, ThresholdType.Binary);
            return img;
        }
        static Point[] skeleton_frame2point(SkeletonFrame frame)
        {
            Skeleton[] skeletonData = new Skeleton[myKinect.SkeletonStream.FrameSkeletonArrayLength];
            frame.CopySkeletonDataTo(skeletonData);
            Skeleton skeleton = (from s in skeletonData
                                 where s.TrackingState == SkeletonTrackingState.Tracked
                                 select s).FirstOrDefault();
            if (skeleton == null) return null;

            Point[] keypt = new Point[5];//左手、右手、左肩、中肩、右肩
            foreach (Joint joint in skeleton.Joints)
            {
                ColorImagePoint colorpt = myKinect.CoordinateMapper.MapSkeletonPointToColorPoint(joint.Position, ColorImageFormat.RgbResolution640x480Fps30);
                Point pt = new Point(colorpt.X, colorpt.Y);
                switch (joint.JointType)
                {
                    case JointType.HandLeft:
                        keypt[0] = pt;
                        break;
                    case JointType.HandRight:
                        keypt[1] = pt;
                        break;
                    case JointType.ShoulderLeft:
                        keypt[2] = pt;
                        break;
                    case JointType.ShoulderCenter:
                        keypt[3] = pt;
                        break;
                    case JointType.ShoulderRight:
                        keypt[4] = pt;
                        break;
                }
            }
            return keypt;
        }
        static Mat draw_bodypt(Mat img, Point[] bodypt)
        {
            foreach( Point pt in bodypt)
            {
                CvInvoke.Circle(img, pt, 10, new MCvScalar(1, 0, 0), -1);
            }
            return img;
        }
        /*static Mat draw_trail(Mat img, SkeletonFrame frame) {
            Skeleton[] skeletonData = new Skeleton[myKinect.SkeletonStream.FrameSkeletonArrayLength];
            frame.CopySkeletonDataTo(skeletonData);
            Skeleton skeleton = (from s in skeletonData
                                 where s.TrackingState == SkeletonTrackingState.Tracked
                                 select s).FirstOrDefault();
            if (skeleton != null) {
                foreach (Joint joint in skeleton.Joints)
                {
                    ColorImagePoint colorpt = myKinect.CoordinateMapper.MapSkeletonPointToColorPoint(joint.Position, ColorImageFormat.RgbResolution640x480Fps30);
                    CvInvoke.Circle(img, new Point(colorpt.X, colorpt.Y), 10, new MCvScalar(1, 0, 0), -1);
                    if (joint.JointType == JointType.HandRight)
                    {
                        trail_length++;
                        trail.Enqueue(new Point(colorpt.X, colorpt.Y));
                        if (trail_length > 100) {
                            trail_length++;
                            trail.Dequeue();
                        }
                        
                    }
                    // 在图片上画轨迹
                    foreach(Point pt in trail)
                    {
                        CvInvoke.Circle(img, pt, 2, new MCvScalar(1, 0, 0), -1);
                    }
                }
            }
            return img;
        }*/
    }
}
