using System.Collections.Generic;
using System.Linq;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS;
using MvvmCross.Binding.iOS.Views;
using MvvmCross.Core.ViewModels;
using MvvmCross.Plugins.Color.iOS;
using MvvmCross.Plugins.Visibility;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Suggestions;
using UIKit;
using static Toggl.Daneel.Extensions.ViewBindingExtensions;

namespace Toggl.Daneel.Suggestions
{
    public sealed class SuggestionsView : MvxView
    {
        private const int suggestionCount = 3;
        private const float titleSize = 12;
        private const float sideMargin = 16;
        private const float suggestionHeight = 64;
        private const float emptyHeight = 32;
        private const float distanceAbowTitleLabel = 20;
        private const float distanceBelowTitleLabel = 16;
        private const float distanceBetweenSuggestions = 12;

        private readonly UILabel titleLabel = new UILabel();
        private readonly List<SuggestionView> suggestionViews
            = new List<SuggestionView>(suggestionCount);

        public IMvxCommand<Suggestion> SuggestionTappedCommad { get; set; }

        public SuggestionsView()
        {
            TranslatesAutoresizingMaskIntoConstraints = false;
            BackgroundColor = UIColor.White;
            ClipsToBounds = true;

            for (int i = 0; i < suggestionCount; i++)
                suggestionViews.Add(SuggestionView.Create());
        }

        public override void MovedToSuperview()
        {
            base.MovedToSuperview();
             
            TopAnchor.ConstraintEqualTo(Superview.TopAnchor).Active = true;
            WidthAnchor.ConstraintEqualTo(Superview.WidthAnchor).Active = true;
            CenterXAnchor.ConstraintEqualTo(Superview.CenterXAnchor).Active = true;
            //Actual value is set with bindings a few lines below
            var heightConstraint = HeightAnchor.ConstraintEqualTo(1);
            heightConstraint.Active = true;

            prepareTitleLabel();
            prepareSuggestionViews();

            SetNeedsLayout();
            LayoutIfNeeded();

            this.DelayBind(() =>
            {
                var heightConverter = new CollectionSizeToHeightConverter<Suggestion>(
                    emptyHeight: emptyHeight,
                    heightPerElement: suggestionHeight + distanceBetweenSuggestions,
                    additionalHeight: distanceAbowTitleLabel
                                    + distanceBelowTitleLabel
                                    + (float)titleLabel.Frame.Height
                );
                heightConverter.MaxCollectionSize = suggestionViews.Count;

                var bindingSet = this.CreateBindingSet<SuggestionsView, SuggestionsViewModel>();

                bindingSet.Bind(this)
                          .For(v => v.BindVisibility())
                          .To(vm => vm.IsEmpty);

                bindingSet.Bind(heightConstraint)
                          .For(c => c.BindConstant())
                          .To(vm => vm.Suggestions)
                          .WithConversion(heightConverter);
                
                for (int i = 0; i < suggestionViews.Count; i++)
                {
                    bindingSet.Bind(suggestionViews[i])
                              .For(v => v.Suggestion)
                              .To(vm => vm.Suggestions[i]);
                }

                bindingSet.Apply();
            });
        }

        private void prepareTitleLabel()
        {
            AddSubview(titleLabel);
            titleLabel.TranslatesAutoresizingMaskIntoConstraints = false;
            titleLabel.Text = Resources.SuggestionsHeader;
            titleLabel.Font = UIFont.SystemFontOfSize(titleSize, UIFontWeight.Medium);
            titleLabel.TextColor = Color.Main.SuggestionsTitle.ToNativeColor();
            titleLabel.TopAnchor.ConstraintEqualTo(Superview.TopAnchor, distanceAbowTitleLabel).Active = true;
            titleLabel.LeadingAnchor.ConstraintEqualTo(Superview.LeadingAnchor, sideMargin).Active = true;
        }

        private void prepareSuggestionViews()
        {
            for (int i = 0; i < suggestionViews.Count; i++)
            {
                var suggestionView = suggestionViews[i];
                AddSubview(suggestionView);
                suggestionView.TranslatesAutoresizingMaskIntoConstraints = false;
                suggestionView.HeightAnchor.ConstraintEqualTo(suggestionHeight).Active = true;
                suggestionView.CenterXAnchor.ConstraintEqualTo(Superview.CenterXAnchor).Active = true;
                suggestionView.WidthAnchor.ConstraintEqualTo(Superview.WidthAnchor, 1, -2 * sideMargin).Active = true;
                suggestionView.TopAnchor.ConstraintEqualTo(titleLabel.BottomAnchor, distanceFromTitleLabel(i)).Active = true;

                suggestionView.AddGestureRecognizer(new UITapGestureRecognizer(() =>
                {
                    SuggestionTappedCommad?.Execute(suggestionView.Suggestion);
                }));
            }
        }

        private float distanceFromTitleLabel(int index)
            => distanceBelowTitleLabel
               + index * distanceBetweenSuggestions
               + index * suggestionHeight;
    }
}
