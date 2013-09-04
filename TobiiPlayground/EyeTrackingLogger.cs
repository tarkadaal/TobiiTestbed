using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Tobii.Gaze.Core;
using TobiiPlayground.Extensions;

namespace TobiiPlayground
{
    public class EyeTrackingLogger : IDisposable
    {
        public EyeTrackingLogger()
            : this("output.log")
        { }

        public EyeTrackingLogger(string filename)
        {
            _file = File.Open(filename, FileMode.Create);
            _writer = new StreamWriter(_file);
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Close();
                _writer = null;
            }

            if (_file != null)
            {
                _file = null;
            }
        }

        public void LogGazeDataAsCsv(GazeData gazeData, long normalizedTimestamp)
        {
            lock (_fileLock)
            {
                var s = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24}",
                    gazeData.Timestamp.ToString(),
                    normalizedTimestamp.ToString(),
                    gazeData.TrackingStatus.ToString(),
                    gazeData.Left.EyePositionFromEyeTrackerMM.X.ToString(),
                    gazeData.Left.EyePositionFromEyeTrackerMM.Y.ToString(),
                    gazeData.Left.EyePositionFromEyeTrackerMM.Z.ToString(),
                    gazeData.Left.EyePositionInTrackBoxNormalized.X.ToString(),
                    gazeData.Left.EyePositionInTrackBoxNormalized.Y.ToString(),
                    gazeData.Left.EyePositionInTrackBoxNormalized.Z.ToString(),
                    gazeData.Left.GazePointFromEyeTrackerMM.X.ToString(),
                    gazeData.Left.GazePointFromEyeTrackerMM.Y.ToString(),
                    gazeData.Left.GazePointFromEyeTrackerMM.Z.ToString(),
                    gazeData.Left.GazePointOnDisplayNormalized.X.ToString(),
                    gazeData.Left.GazePointOnDisplayNormalized.Y.ToString(),
                    gazeData.Right.EyePositionFromEyeTrackerMM.X.ToString(),
                    gazeData.Right.EyePositionFromEyeTrackerMM.Y.ToString(),
                    gazeData.Right.EyePositionFromEyeTrackerMM.Z.ToString(),
                    gazeData.Right.EyePositionInTrackBoxNormalized.X.ToString(),
                    gazeData.Right.EyePositionInTrackBoxNormalized.Y.ToString(),
                    gazeData.Right.EyePositionInTrackBoxNormalized.Z.ToString(),
                    gazeData.Right.GazePointFromEyeTrackerMM.X.ToString(),
                    gazeData.Right.GazePointFromEyeTrackerMM.Y.ToString(),
                    gazeData.Right.GazePointFromEyeTrackerMM.Z.ToString(),
                    gazeData.Right.GazePointOnDisplayNormalized.X.ToString(),
                    gazeData.Right.GazePointOnDisplayNormalized.Y.ToString());
                _writer.WriteLine(s);
            }
        }

        public void LogGazeData(GazeData gazeData)
        {
            lock (_fileLock)
            {
                // Timestamp is given in UNIX Epoch
                // Eyetracker defaults to 2007
                LogLine("Timestamp", gazeData.Timestamp.UnixTimeStampToDateTime().ToString("O"));
                LogLine("Tracking Status", gazeData.TrackingStatus.ToString());
                LogEye("Left", gazeData.Left);
                LogEye("Right", gazeData.Right);
                _writer.WriteLine("-----------");
            }
        }

        public void LogEye(string eyeName, GazeDataEye eye)
        {
            LogLine(MakeEyeLabel(eyeName, "EyePositionFromEyeTrackerMM"), eye.EyePositionFromEyeTrackerMM.ToString());
            LogLine(MakeEyeLabel(eyeName, "EyePostitionInTrackBoxNormalized"), eye.EyePositionInTrackBoxNormalized.ToString());
            LogLine(MakeEyeLabel(eyeName, "GazePointFromEyeTrackerMM"), eye.GazePointFromEyeTrackerMM.ToString());
            LogLine(MakeEyeLabel(eyeName, "GazePointOnDisplayNormalized"), eye.GazePointOnDisplayNormalized.ToString());
        }

        public void LogHeaders()
        {
            lock (_fileLock)
            {
                var s = String.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23},{24}",
                    "Timestamp",
                    "TimestampOffset",
                    "Tracking Status",
                    "LeftEyePositionFromEyeTrackerMMX",
                    "LeftEyePositionFromEyeTrackerMMY",
                    "LeftEyePositionFromEyeTrackerMMZ",
                    "LeftEyePostitionInTrackBoxNormalizedX",
                    "LeftEyePostitionInTrackBoxNormalizedY",
                    "LeftEyePostitionInTrackBoxNormalizedZ",
                    "LeftGazePointFromEyeTrackerMMX",
                    "LeftGazePointFromEyeTrackerMMY",
                    "LeftGazePointFromEyeTrackerMMZ",
                    "LeftGazePointOnDisplayNormalizedX",
                    "LeftGazePointOnDisplayNormalizedY",
                    "RightEyePositionFromEyeTrackerMMX",
                    "RightEyePositionFromEyeTrackerMMY",
                    "RightEyePositionFromEyeTrackerMMZ",
                    "RightEyePostitionInTrackBoxNormalizedX",
                    "RightEyePostitionInTrackBoxNormalizedY",
                    "RightEyePostitionInTrackBoxNormalizedZ",
                    "RightGazePointFromEyeTrackerMMX",
                    "RightGazePointFromEyeTrackerMMY",
                    "RightGazePointFromEyeTrackerMMZ",
                    "RightGazePointOnDisplayNormalizedX",
                    "RightGazePointOnDisplayNormalizedY");
                _writer.WriteLine(s);
            }
        }

        private void LogLine(string label, string value)
        {
            _writer.WriteLine(String.Format("{0}: {1}", label, value));
        }

        private string MakeEyeLabel(string eyeName, string propertyName)
        {
            return String.Format("{0}: {1}", eyeName, propertyName);
        }

        private FileStream _file;
        private StreamWriter _writer;
        private Object _fileLock = new object();
    }
}
