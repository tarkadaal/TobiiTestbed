using System;

namespace TobiiPlayground
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class EyeTrackingStateChangedEventArgs : EventArgs
    {
        public EyeTrackingStateChangedEventArgs(EyeTrackingState eyeTrackingState, string errorMessage, bool canResolve, bool canRetry)
        {
            EyeTrackingState = eyeTrackingState;
            CanRetry = canRetry;
            CanResolve = canResolve;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Gets the EyeTrackingState.
        /// </summary>
        public EyeTrackingState EyeTrackingState { get; private set; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string ErrorMessage { get; private set; }

        public bool CanRetry { get; private set; }

        public bool CanResolve { get; private set; }
    }
}
