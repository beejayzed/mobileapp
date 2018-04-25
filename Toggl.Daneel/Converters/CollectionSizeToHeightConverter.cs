using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MvvmCross.Platform.Converters;
using static Toggl.Multivac.Extensions.ClampExtension;

namespace Toggl.Foundation.MvvmCross.Converters
{
    public sealed class CollectionSizeToHeightConverter<T> : MvxValueConverter<IEnumerable<T>, nfloat>
    {
        public float EmptyHeight { get; }

        public float HeightPerElement { get; }

        public float AdditionalHeight { get; }

        public int? MaxCollectionSize { get; set; }

        public CollectionSizeToHeightConverter(float emptyHeight, float heightPerElement, float additionalHeight)
        {
            EmptyHeight = emptyHeight;
            HeightPerElement = heightPerElement;
            AdditionalHeight = additionalHeight;
        }

        protected override nfloat Convert(IEnumerable<T> value, Type targetType, object parameter, CultureInfo culture)
        {
            var count = value?.Count() ?? 0;

            if (count == 0)
                return EmptyHeight;

            if (MaxCollectionSize.HasValue)
                count = count.Clamp(0, MaxCollectionSize.Value);
            
            return count * HeightPerElement + AdditionalHeight;
        }
    }
}
