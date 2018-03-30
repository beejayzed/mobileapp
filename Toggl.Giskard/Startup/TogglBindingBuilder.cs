using Toggl.Foundation.MvvmCross.Combiners;
using MvvmCross.Binding.Droid;
using System;

namespace Toggl.Giskard
{
    public sealed class TogglBindingBuilder : MvxAndroidBindingBuilder
    {
        protected override void FillValueCombiners(MvvmCross.Binding.Combiners.IMvxValueCombinerRegistry registry)
        {
            registry.AddOrOverwrite("Duration", new DurationValueCombiner());

            var shortDateTimeOffsetDateFormatCombiner = new DateTimeOffsetDateFormatValueCombiner(TimeZoneInfo.Local, false);
            registry.AddOrOverwrite("ShortDateTimeOffsetDateFormat", shortDateTimeOffsetDateFormatCombiner);

            base.FillValueCombiners(registry);
        }
    }
}
