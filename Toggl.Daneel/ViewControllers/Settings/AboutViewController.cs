﻿using MvvmCross.Binding.BindingContext;
using MvvmCross.Binding.iOS;
using MvvmCross.iOS.Views;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels.Settings;
using static Toggl.Daneel.Extensions.LayoutConstraintExtensions;

namespace Toggl.Daneel.ViewControllers.Settings
{
    public sealed partial class AboutViewController : MvxViewController<AboutViewModel>
    {
        public AboutViewController() : base(nameof(AboutViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TopConstraint.AdaptForIos10(NavigationController.NavigationBar);
            Title = Resources.About;

            var bindingSet = this.CreateBindingSet<AboutViewController, AboutViewModel>();

            bindingSet.Bind(LicensesView)
                      .For(v => v.BindTap())
                      .To(vm => vm.LicensesCommand);

            bindingSet.Apply();
        }
    }
}
