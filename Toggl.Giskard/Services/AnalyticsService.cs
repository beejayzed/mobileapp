using System;
using System.Collections.Generic;
using Android.App;
using Android.OS;
using Firebase.Analytics;
using Toggl.Foundation.Analytics;

namespace Toggl.Giskard.Services
{
    public sealed class AnalyticsService : BaseAnalyticsService
    {
        private const string exceptionEventName = "HandledException";
        private const string exceptionTypeParameter = "ExceptionType";
        private const string exceptionMessageParameter = "ExceptionMessage";

        private FirebaseAnalytics firebaseAnalytics = FirebaseAnalytics.GetInstance(Application.Context);

        protected override void NativeTrackEvent(string eventName, Dictionary<string, string> parameters)
        {
            var bundle = bundleFromParameters(parameters);
            firebaseAnalytics.LogEvent(eventName, bundle);
        }

        protected override void NativeTrackException(Exception exception)
        {
            NativeTrackEvent(exceptionEventName, new Dictionary<string, string>
            {
                [exceptionTypeParameter] = exception.GetType().FullName,
                [exceptionMessageParameter] = exception.Message
            });
        }

        private Bundle bundleFromParameters(Dictionary<string, string> parameters)
        {
            var bundle = new Bundle();

            foreach(var entry in parameters)
            {
                bundle.PutString(entry.Key, entry.Value);
            }

            return bundle;
        }
    }
}
