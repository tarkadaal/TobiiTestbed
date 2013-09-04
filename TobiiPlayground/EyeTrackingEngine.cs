namespace TobiiPlayground
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Tobii.Gaze.Config;
    using Tobii.Gaze.Core;
    using System.IO;

    public enum EyeTrackingState
    {
        NotInitialized,
        TobiiEyeTrackingInitialized,
        ConfigurationInitialized,
        EyeTrackerInitialized,
        EyeTrackerConnected,
        EyeTrackingPrepared,
        Tracking,

        // Error States Below
        TobiiEyeTrackingNotAvailable,
        TobiiEyeTrackingIncompatible,
        IncompleteConfiguration,
        InvalidConfiguration,
        ConnectionFailed,
        EyeTrackerError
    }

    /// <summary>
    /// The eyetracking engine provides gaze data from the currently setup eyetracker.
    /// It reads and validates the current eye tracker configuration,
    /// connects to and prepares the eye tracker for eyetracking and then
    /// provides gaze data until the eye tracker is disconnected or eyetracking engine is disposed. 
    /// </summary>
    public sealed class EyeTrackingEngine : IDisposable
    {
        public EventHandler<EyeTrackingStateChangedEventArgs> StateChanged;
        public EventHandler<GazePointEventArgs> GazePoint;
        private readonly IEyeTrackerConfigLibrary _eyeTrackerConfigLibrary;

        private static readonly Dictionary<EyeTrackingState, string> ErrorMessages = new Dictionary<EyeTrackingState, string>
        {
            { EyeTrackingState.TobiiEyeTrackingNotAvailable, "Tobii Eye Tracking is not installed on this computer." },
            { EyeTrackingState.TobiiEyeTrackingIncompatible, "This program cannot be used with the installed version of Tobii Eye Tracking." },
            { EyeTrackingState.IncompleteConfiguration, "Tobii Eye Tracking has not been set up for use." },
            { EyeTrackingState.InvalidConfiguration, "The system has changed so that the Tobii Eye Tracking setup is not valid anymore." },
            { EyeTrackingState.EyeTrackerError, "The eye tracker reported an error." },
            { EyeTrackingState.ConnectionFailed, "The connection to the eye tracker failed." }
        };

        private readonly IConfigurationProvider _configurationProvider;
        private EyeTrackingState _state = EyeTrackingState.NotInitialized;
        private Uri _eyeTrackerUrl;
        private string _userProfile;
        private IEyeTracker _eyeTracker;
        private Rect? _screenBounds;
        private TrackedEyes _trackedEyes;
        private Thread _thread;
        private long? _startTime;
        private EyeTrackingLogger _logger;


        /// <summary>
        /// Create eye tracking engine 
        /// Throws EyeTrackerException if not successful
        /// </summary>
        public EyeTrackingEngine()
        {
            _eyeTrackerConfigLibrary = new EyeTrackerConfigLibrary();
            _configurationProvider = new ConfigurationProvider(_eyeTrackerConfigLibrary);
            _logger = new EyeTrackingLogger();
            _logger.LogHeaders();
        }

        public EyeTrackingState State
        {
            get
            {
                return _state;
            }

            private set
            {
                if (_state != value)
                {
                    _state = value;
                    OnStateChange();
                }
            }
        }

        /// <summary>
        /// The eyetracking screen bounds 
        /// </summary>
        public Rect? EyeTrackingScreenBounds
        {
            get { return _screenBounds; }
        }

        /// <summary>
        /// Initialize the eye tracker engine and start eyetracking in the following order:
        /// - Initialize Tobii Eyetracking
        /// - Validate current eye tracker configuration (current calibration and display area)
        /// - Connect to and prepare eye tracker for tracking (set current calibration and display area)
        /// - Provide gaze data to the client through GazePoint event handler
        /// 
        /// If any of the steps above fail the eye tracker engine is set in an error state. 
        /// State changes are notified to the client with the StateChanged event handler. 
        /// </summary>
        public void Initialize()
        {
            if (State != EyeTrackingState.NotInitialized)
            {
                throw new InvalidOperationException("EyeTrackingEngine can not be initialized when not in state NotInitialized");
            }

            InitializeTobiiEyeTracking();
        }

        /// <summary>
        /// Launches the Tobii EyeTracking Control Panel to  
        /// allow the user to manually configure display area and perform a calibration. 
        /// This function should be called if the eye tracker is in one of the following states:
        /// - EyeTrackingState.IncompleteConfiguration
        /// - EyeTrackingState.InvalidConfiguration
        /// When user is done with setup in Control Panel, refresh the state of eye tracker engine 
        /// by calling Retry().
        /// </summary>
        public void ResolveError()
        {
            try
            {
                _configurationProvider.LaunchControlPanel();
            }
            catch (EyeTrackerException)
            {
                State = EyeTrackingState.TobiiEyeTrackingNotAvailable;
            }
        }

        /// <summary>
        ///  Retry to Initialize eye tracker engine. Should be called when user has manually
        /// changed the configuration or performed a calibration with the Tobii EyeTracking Control Panel.
        /// </summary>
        public void Retry()
        {
            Reset();

            // No need to initialize Tetio again, set state to start at configuring eye tracker 
            State = EyeTrackingState.TobiiEyeTrackingInitialized;
        }
        /// <summary>
        /// Stop eye tracking and dispose eye tracking engine and Tobii EyeTracking
        /// </summary>
        public void Dispose()
        {
            Reset();
            if (_eyeTrackerConfigLibrary != null)
            {
                _eyeTrackerConfigLibrary.Dispose();
            }
        }

        private bool CanRetry
        {
            get
            {
                if (State == EyeTrackingState.IncompleteConfiguration ||
                    State == EyeTrackingState.InvalidConfiguration ||
                    State == EyeTrackingState.ConnectionFailed ||
                    State == EyeTrackingState.EyeTrackerError)
                {
                    return true;
                }

                return false;
            }
        }

        private bool CanResolve
        {
            get
            {
                if (State != EyeTrackingState.TobiiEyeTrackingNotAvailable &&
                    State != EyeTrackingState.TobiiEyeTrackingIncompatible)
                {
                    return true;
                }

                return false;
            }
        }

        private string ErrorMessage
        {
            get { return ErrorMessages.ContainsKey(State) ? ErrorMessages[State] : string.Empty; }
        }

        private void OnStateChange()
        {
            RaiseStateChanged();

            switch (State)
            {
                case EyeTrackingState.NotInitialized:
                    InitializeTobiiEyeTracking();
                    break;

                case EyeTrackingState.TobiiEyeTrackingInitialized:
                    InitializeConfiguration();
                    break;

                case EyeTrackingState.ConfigurationInitialized:
                    InitializeEyeTrackerAndRunEventLoop();
                    break;

                case EyeTrackingState.EyeTrackerInitialized:
                    ConnectToEyeTracker();
                    break;

                case EyeTrackingState.EyeTrackerConnected:
                    PrepareEyeTracker();
                    break;

                case EyeTrackingState.EyeTrackingPrepared:
                    StartTracking();
                    break;

                case EyeTrackingState.Tracking:
                    // We're done!
                    break;

                default:
                    // All other states are error states
                    return;
            }
        }

        private void InitializeTobiiEyeTracking()
        {
            if (State != EyeTrackingState.NotInitialized)
            {
                throw new InvalidOperationException("EyeTrackingEngine can not be initialized when not in state NotInitialized");
            }

            State = EyeTrackingState.TobiiEyeTrackingInitialized;
        }

        private void InitializeConfiguration()
        {
            if (State != EyeTrackingState.TobiiEyeTrackingInitialized)
            {
                throw new InvalidOperationException("Can not initialize configuration when not in state TobiiEyeTrackingInitialized");
            }

            if (ValidateConfiguration() && GetCurrentConfiguration())
            {
                State = EyeTrackingState.ConfigurationInitialized;
            }
        }

        private bool ValidateConfiguration()
        {
            var validationResult = _configurationProvider.Validate();

            switch (validationResult)
            {
                case ErrorCode.Success:
                    break;

                case ErrorCode.TobiiEyeTrackingNotAvailable:
                    State = EyeTrackingState.TobiiEyeTrackingNotAvailable;
                    return false;

                case ErrorCode.TobiiEyeTrackingIncompatible:
                    State = EyeTrackingState.TobiiEyeTrackingIncompatible;
                    return false;

                case ErrorCode.ConfigIncomplete:
                    State = EyeTrackingState.IncompleteConfiguration;
                    return false;

                case ErrorCode.ConfigInvalid:
                default:
                    State = EyeTrackingState.InvalidConfiguration;
                    return false;
            }

            return true;
        }

        private bool GetCurrentConfiguration()
        {
            try
            {
                _eyeTrackerUrl = _configurationProvider.GetDefaultEyeTrackerUrl();
                _screenBounds = _configurationProvider.GetScreenBoundsPixels(_eyeTrackerUrl);
                _userProfile = _configurationProvider.GetCurrentUserProfile();
                _trackedEyes = _configurationProvider.GetTrackedEye(_userProfile);
            }
            catch (EyeTrackerException)
            {
                State = EyeTrackingState.InvalidConfiguration;
                return false;
            }

            return true;
        }

        private void InitializeEyeTrackerAndRunEventLoop()
        {
            if (State != EyeTrackingState.ConfigurationInitialized)
            {
                throw new InvalidOperationException("Can not initialize eye tracker and run event loop when not in state ConfigurationInitialized");
            }

            if (_eyeTracker == null)
            {
                try
                {
                    _eyeTracker = new EyeTracker(_eyeTrackerUrl);
                    _eyeTracker.EyeTrackerError += OnEyeTrackerError;
                    _eyeTracker.GazeData += OnGazeData;

                    CreateAndRunEventLoopThread();

                    State = EyeTrackingState.EyeTrackerInitialized;
                }
                catch (EyeTrackerException)
                {
                    State = EyeTrackingState.ConnectionFailed;
                }
            }
        }

        private void CreateAndRunEventLoopThread()
        {
            if (_thread != null)
            {
                throw new InvalidOperationException("_thread parameter is already set");
            }

            _thread = new Thread(() =>
            {
                try
                {
                    _eyeTracker.RunEventLoop();
                }
                catch (EyeTrackerException)
                {
                    State = EyeTrackingState.EyeTrackerError;
                }
            });

            _thread.Start();
        }

        private void ConnectToEyeTracker()
        {
            if (State != EyeTrackingState.EyeTrackerInitialized)
            {
                throw new InvalidOperationException("Can not connect to eye tracker when not in state EyeTrackerInitialized");
            }

            _eyeTracker.ConnectAsync(errorCode =>
                {
                    if (errorCode != ErrorCode.Success)
                    {
                        State = EyeTrackingState.ConnectionFailed;
                    }
                    else
                    {
                        State = EyeTrackingState.EyeTrackerConnected;
                    }
                });
        }

        private void PrepareEyeTracker()
        {
            if (State != EyeTrackingState.EyeTrackerConnected)
            {
                throw new InvalidOperationException("Can not prepare eye tracker when eye tracker is not connected");
            }

            _configurationProvider.PrepareEyeTrackerAsync(
                _eyeTrackerUrl,
                _userProfile,
                _eyeTracker,
                errorCode =>
                {
                    if (errorCode != ErrorCode.Success)
                    {
                        State = EyeTrackingState.ConnectionFailed;
                    }
                    else
                    {
                        State = EyeTrackingState.EyeTrackingPrepared;
                    }
                });
        }

        private void StartTracking()
        {
            if (State != EyeTrackingState.EyeTrackingPrepared)
            {
                throw new InvalidOperationException("Can not start tracking when eye tracker is not prepared for tracking");
            }

            _eyeTracker.StartTrackingAsync(errorCode =>
                {
                    if (errorCode != ErrorCode.Success)
                    {
                        State = EyeTrackingState.ConnectionFailed;
                    }
                    else
                    {
                        State = EyeTrackingState.Tracking;
                    }
                });
        }

        private void Reset()
        {
            if (_eyeTracker != null)
            {
                _eyeTracker.BreakEventLoop();
                if (_thread != null)
                {
                    _thread.Join();
                }

                _eyeTracker.EyeTrackerError -= OnEyeTrackerError;
                _eyeTracker.GazeData -= OnGazeData;
                _eyeTracker.Dispose();
                _eyeTracker = null;
            }

            if (_thread != null)
            {
                _thread.Abort();
                _thread = null;
            }
        }

        private void RaiseStateChanged()
        {
            var handler = StateChanged;

            if (handler != null)
            {
                handler(this, new EyeTrackingStateChangedEventArgs(State, ErrorMessage, CanResolve, CanRetry));
            }
        }

        private void RaiseGazePoint(Point2D point)
        {
            var handler = GazePoint;

            if (handler != null && _screenBounds.HasValue)
            {
                var x = Convert.ToInt32(_screenBounds.Value.X + (point.X * (_screenBounds.Value.Width)));
                var y = Convert.ToInt32(_screenBounds.Value.Y + (point.Y * (_screenBounds.Value.Height)));

                handler(this, new GazePointEventArgs(x, y));
            }
        }

        private void OnEyeTrackerError(object sender, EyeTrackerErrorEventArgs eyeTrackerErrorEventArgs)
        {
            if (eyeTrackerErrorEventArgs.ErrorCode != ErrorCode.Success)
            {
                State = EyeTrackingState.ConnectionFailed;
            }
        }

        private void OnGazeData(object sender, GazeDataEventArgs gazeDataEventArgs)
        {
            var gazeData = gazeDataEventArgs.GazeData;
            if (!_startTime.HasValue)
            {
                _startTime = gazeData.Timestamp;
            }

            _logger.LogGazeDataAsCsv(gazeData, NormalizeTimestamp(gazeData));

            switch (_trackedEyes)
            {
                case TrackedEyes.BothEyes:
                    if (gazeData.TrackingStatus == TrackingStatus.BothEyesTracked ||
                        gazeData.TrackingStatus == TrackingStatus.OneEyeTrackedUnknownWhich)
                    {
                        var p = new Point2D(
                            (gazeData.Left.GazePointOnDisplayNormalized.X +
                             gazeData.Right.GazePointOnDisplayNormalized.X)/2,
                            (gazeData.Left.GazePointOnDisplayNormalized.Y +
                             gazeData.Right.GazePointOnDisplayNormalized.Y)/2);
    
                        RaiseGazePoint(p);
                    }
                    else if (gazeData.TrackingStatus == TrackingStatus.OnlyLeftEyeTracked ||
                             gazeData.TrackingStatus == TrackingStatus.OneEyeTrackedProbablyLeft)
                    {
                        RaiseGazePoint(gazeData.Left.GazePointOnDisplayNormalized);
                    }
                    else if (gazeData.TrackingStatus == TrackingStatus.OnlyRightEyeTracked ||
                             gazeData.TrackingStatus == TrackingStatus.OneEyeTrackedProbablyRight)
                    {
                        RaiseGazePoint(gazeData.Right.GazePointOnDisplayNormalized);
                    }

                    break;

                case TrackedEyes.LeftEyeOnly:
                    if (gazeData.TrackingStatus == TrackingStatus.BothEyesTracked ||
                        gazeData.TrackingStatus == TrackingStatus.OnlyLeftEyeTracked ||
                        gazeData.TrackingStatus == TrackingStatus.OneEyeTrackedProbablyLeft ||
                        gazeData.TrackingStatus == TrackingStatus.OneEyeTrackedUnknownWhich)
                    {
                        RaiseGazePoint(gazeData.Left.GazePointOnDisplayNormalized);
                    }

                    break;

                case TrackedEyes.RightEyeOnly:
                    if (gazeData.TrackingStatus == TrackingStatus.BothEyesTracked ||
                        gazeData.TrackingStatus == TrackingStatus.OnlyRightEyeTracked ||
                        gazeData.TrackingStatus == TrackingStatus.OneEyeTrackedProbablyRight ||
                        gazeData.TrackingStatus == TrackingStatus.OneEyeTrackedUnknownWhich)
                    {
                        RaiseGazePoint(gazeData.Right.GazePointOnDisplayNormalized);
                    }

                    break;
            }
        }

        private long NormalizeTimestamp(GazeData gazeData)
        {
            if (!_startTime.HasValue)
            {
                throw new InvalidOperationException("_startTime is expected to have been initialized at this point.");
            }

            return gazeData.Timestamp - _startTime.Value;
        }
    }
}
