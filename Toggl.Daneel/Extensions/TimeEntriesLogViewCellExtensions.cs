using System;
using Foundation;
using CoreAnimation;
using Toggl.Daneel.Views;
using static Toggl.Foundation.MvvmCross.Helper.Animation;

namespace Toggl.Daneel.Extensions
{
    public enum Direction
    {
        Left = -1,
        Right = 1
    }

    public static class TimeEntriesLogViewCellExtensions
    {
        public static IDisposable RevealSwipeActionAnimation(this TimeEntriesLogViewCell cell, Direction direction)
        {
            var animation = CAKeyFrameAnimation.FromKeyPath("transform.translation.x");

            animation.Duration = 1.51;

            animation.TimingFunctions = new CAMediaTimingFunction[]
            {
                new CAMediaTimingFunction(1f, 1f, 1f, 1f),
                new CAMediaTimingFunction(.1f, .2f, .1f, 0f),
                new CAMediaTimingFunction(.1f, .1f, .1f, 0f),
                new CAMediaTimingFunction(.0f, .32f, .7f, 1f),
                new CAMediaTimingFunction(.38f, .1f, .7f, 1f),
                new CAMediaTimingFunction(1f, 1f, 1f, 1f)
            };

            animation.KeyTimes = new NSNumber[]
            {
                0.0,
                0.5,
                0.6,
                0.73,
                0.81,
                1.51
            };

            animation.Values = new[]
            {
                NSObject.FromObject(0.0),
                NSObject.FromObject((double)direction * 50.0),
                NSObject.FromObject(0.0),
                NSObject.FromObject((double)direction * 3.5),
                NSObject.FromObject(0.0),
                NSObject.FromObject(0.0)
            };

            animation.RepeatCount = int.MaxValue;
            cell.Layer.AddAnimation(animation, direction.ToString());

            return animation;
        }
    }
}
