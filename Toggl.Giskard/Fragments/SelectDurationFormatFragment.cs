﻿using System;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using MvvmCross.Binding.Droid.BindingContext;
using MvvmCross.Droid.Support.V4;
using MvvmCross.Droid.Views.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Extensions;

namespace Toggl.Giskard.Fragments
{
    [MvxDialogFragmentPresentation(AddToBackStack = true)]
    public sealed class SelectDurationFormatFragment : MvxDialogFragment<SelectDurationFormatViewModel>
    {
        public SelectDurationFormatFragment() { }

        public SelectDurationFormatFragment(IntPtr javaReference, JniHandleOwnership transfer)
            : base (javaReference, transfer) { }
        
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = this.BindingInflate(Resource.Layout.SelectDurationFormatFragment, null);
            return view;
        }

        public override void OnResume()
        {
            base.OnResume();

            var displayMetrics = new DisplayMetrics();
            Activity.WindowManager.DefaultDisplay.GetMetrics(displayMetrics);
            var screenWidth = displayMetrics.WidthPixels;
            var isLargeScreen = screenWidth > 360.DpToPixels(Context);

            var width = isLargeScreen ? screenWidth - 72.DpToPixels(Context) : 312.DpToPixels(Context);
            var height = 268.DpToPixels(Context);

            Dialog.Window.SetLayout(width, height);
        }

        public override void OnCancel(IDialogInterface dialog)
        {
            ViewModel.CloseCommand.ExecuteAsync();
        }
    }
}
