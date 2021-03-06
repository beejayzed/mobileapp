﻿using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Settings;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class AboutViewModelTests
    {
        public abstract class AboutViewModelTest : BaseViewModelTests<AboutViewModel>
        {
            protected override AboutViewModel CreateViewModel()
                => new AboutViewModel(NavigationService);
        }

        public sealed class TheConstructor : AboutViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void ThrowsIfTheArgumentIsNull()
            {
                Action tryingToConstructWithEmptyParameters =
                    () => new AboutViewModel(null);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class TheLicensesCommand : AboutViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheLicensesViewModel()
            {
                await ViewModel.LicensesCommand.ExecuteAsync();

                await NavigationService.Received().Navigate<LicensesViewModel>();
            }
        }
    }
}
