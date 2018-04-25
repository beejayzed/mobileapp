using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck.Xunit;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Models;
using Xunit;
using static Toggl.Foundation.Helper.Constants;
using static Toggl.Multivac.Extensions.FunctionalExtensions;
using static Toggl.Multivac.Extensions.StringExtensions;
using ITimeEntryPrototype = Toggl.Foundation.Models.ITimeEntryPrototype;
using TextFieldInfo = Toggl.Foundation.Autocomplete.TextFieldInfo;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class StartTimeEntryViewModelTests
    {
        public abstract class StartTimeEntryViewModelTest : BaseViewModelTests<StartTimeEntryViewModel>
        {
            protected const string TagName = "Mobile";

            protected const long TagId = 20;
            protected const long TaskId = 30;
            protected const long ProjectId = 10;
            protected const long WorkspaceId = 40;
            protected const string ProjectName = "Toggl";
            protected const string ProjectColor = "#F41F19";
            protected const string Description = "Testing Toggl mobile apps";

            protected IAutocompleteProvider AutocompleteProvider { get; } = Substitute.For<IAutocompleteProvider>();

            protected StartTimeEntryParameters DefaultParameter { get; } = new StartTimeEntryParameters(DateTimeOffset.UtcNow, "", null);

            protected StartTimeEntryViewModelTest()
            {
                DataSource.AutocompleteProvider.Returns(AutocompleteProvider);

                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1);
            }

            protected override void AdditionalSetup()
            {
                DialogService.Confirm(
                   Arg.Any<string>(),
                   Arg.Any<string>(),
                   Arg.Any<string>(),
                   Arg.Any<string>()
               ).Returns(true);
            }

            protected override StartTimeEntryViewModel CreateViewModel()
                => new StartTimeEntryViewModel(
                    TimeService,
                    DataSource,
                    DialogService,
                    UserPreferences,
                    OnboardingStorage,
                    InteractorFactory,
                    NavigationService);
        }

        public sealed class TheConstructor : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ClassData(typeof(SevenParameterConstructorTestData))]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource, 
                bool useTimeService, 
                bool useDialogService,
                bool useUserPreferences,
                bool useInteractorFactory,
                bool useOnboardingStorage,
                bool useNavigationService)
            {
                var dataSource = useDataSource ? DataSource : null;
                var timeService = useTimeService ? TimeService : null;
                var dialogService = useDialogService ? DialogService : null;
                var userPreferences = useUserPreferences ? UserPreferences : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var onboardingStorage = useOnboardingStorage ? OnboardingStorage : null;
                var navigationService = useNavigationService ? NavigationService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new StartTimeEntryViewModel(
                        timeService,
                        dataSource,
                        dialogService,
                        userPreferences,
                        onboardingStorage,
                        interactorFactory,
                        navigationService);

                tryingToConstructWithEmptyParameters
                    .ShouldThrow<ArgumentNullException>();
            }
        }

        public sealed class ThePrepareMethod : StartTimeEntryViewModelTest
        {
            [Property]
            public void SetsTheDateAccordingToTheDateParameterReceived(DateTimeOffset date, string placeholder, TimeSpan? duration)
            {
                var parameter = new StartTimeEntryParameters(date, placeholder, duration);

                ViewModel.Prepare(parameter);

                ViewModel.StartTime.Should().BeSameDateAs(date);
                ViewModel.PlaceholderText.Should().Be(placeholder);
                ViewModel.Duration.Should().Be(duration);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotStartTheTimerWhenDurationIsNotNull()
            {
                var observable = Substitute.For<IConnectableObservable<DateTimeOffset>>();
                TimeService.CurrentDateTimeObservable.Returns(observable);
                var duration = TimeSpan.FromSeconds(130);
                var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", duration);

                ViewModel.Prepare(parameter);

                TimeService.CurrentDateTimeObservable.DidNotReceiveWithAnyArgs().Subscribe(null);
            }

            [Fact, LogIfTooSlow]
            public void StarsTheTimerWhenDurationIsNull()
            {
                var observable = Substitute.For<IConnectableObservable<DateTimeOffset>>();
                TimeService.CurrentDateTimeObservable.Returns(observable);
                var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", null);

                ViewModel.Prepare(parameter);

                TimeService.CurrentDateTimeObservable.ReceivedWithAnyArgs().Subscribe(null);
            }

            [Fact, LogIfTooSlow]
            public void SetsTheDisplayedTimeToTheValueOfTheDurationParameter()
            {
                var duration = TimeSpan.FromSeconds(130);
                var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", duration);

                ViewModel.Prepare(parameter);

                ViewModel.DisplayedTime.Should().Be(duration);
            }

            [Fact, LogIfTooSlow]
            public void ClearsTheIsDirtyFlag()
            {
                var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", null);

                ViewModel.Prepare(parameter);

                ViewModel.IsDirty.Should().BeFalse();
            }
        }

        public sealed class TheInitializeMethod : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(true)]
            [InlineData(false)]
            public async Task ChecksIfBillableIsAvailableForTheDefaultWorkspace(bool billableValue)
            {
                var user = new MockUser { DefaultWorkspaceId = 10 };
                DataSource.User.Current.Returns(Observable.Return(user));
            
                InteractorFactory
                    .IsBillableAvailableForWorkspace(10)
                    .Execute()
                    .Returns(Observable.Return(billableValue));
                var parameter = new StartTimeEntryParameters(DateTimeOffset.UtcNow, "", null);
                ViewModel.Prepare(parameter);

                await ViewModel.Initialize();

                ViewModel.IsBillableAvailable.Should().Be(billableValue);
            }
        }

        public abstract class TheSuggestCreationProperty : StartTimeEntryViewModelTest
        {
            protected abstract int MaxLength { get; }
            protected abstract char QuerySymbol { get; }
            protected abstract string QueryWithExactSuggestionMatch { get; }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfSuggestingTimeEntries()
            {
                await ViewModel.Initialize();

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor("", 1);

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheCurrentQueryIsEmpty()
            {
                await ViewModel.Initialize();

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor($"{QuerySymbol}", 1);

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheCurrentQueryIsOnlyWhitespace()
            {
                await ViewModel.Initialize();

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor($"{QuerySymbol}    ", 1);

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTheCurrentQueryIsLongerThanMaxLength()
            {
                await ViewModel.Initialize();

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo
                    .WithTextAndCursor($"{QuerySymbol}{createLongString(MaxLength + 1)}", 1);
                
                ViewModel.SuggestCreation.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfSuchSuggestionAlreadyExists()
            {
                await ViewModel.Initialize();

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo
                    .WithTextAndCursor($"{QuerySymbol}{QueryWithExactSuggestionMatch}", 1);

                ViewModel.SuggestCreation.Should().BeFalse();
            }

            private string createLongString(int length)
                => Enumerable
                    .Range(0, length)
                    .Aggregate(new StringBuilder(), (builder, _) => builder.Append('A'))
                    .ToString();


            public sealed class WhenSuggestingProjects : TheSuggestCreationProperty
            {
                protected override int MaxLength => MaxProjectNameLengthInBytes;
                protected override char QuerySymbol => '@';
                protected override string QueryWithExactSuggestionMatch => ProjectName;

                public WhenSuggestingProjects()
                {
                    var project = Substitute.For<IDatabaseProject>();
                    project.Id.Returns(10);
                    project.Name.Returns(ProjectName);
                    project.WorkspaceId.Returns(40);
                    project.Workspace.Name.Returns("Some workspace");
                    var projectSuggestion = new ProjectSuggestion(project);

                    DataSource.AutocompleteProvider
                        .Query(Arg.Is<QueryInfo>(info => info.SuggestionType == AutocompleteSuggestionType.Projects))
                              .Returns(Observable.Return(new ProjectSuggestion[] { projectSuggestion }));

                    ViewModel.Prepare(DefaultParameter);
                }

                [Fact, LogIfTooSlow]
                public void ReturnsFalseIfAProjectIsAlreadySelected()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty(1)
                        .WithProjectInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor);

                    ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor("abcde @fgh", 10);

                    ViewModel.SuggestCreation.Should().BeFalse();
                }

                [Fact, LogIfTooSlow]
                public void ReturnsFalseIfAProjectIsAlreadySelectedEvenIfInProjectSelectionMode()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty(1)
                        .WithProjectInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor);
                    ViewModel.ToggleProjectSuggestionsCommand.Execute();

                    ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor("abcde @fgh", 10);

                    ViewModel.SuggestCreation.Should().BeFalse();
                }
            }

            public sealed class WhenSuggestingTags : TheSuggestCreationProperty
            {
                protected override int MaxLength => MaxTagNameLengthInBytes;
                protected override char QuerySymbol => '#';
                protected override string QueryWithExactSuggestionMatch => TagName;

                public WhenSuggestingTags()
                {
                    var tag = Substitute.For<IDatabaseTag>();
                    tag.Id.Returns(20);
                    tag.Name.Returns(TagName);
                    var tagSuggestion = new TagSuggestion(tag);

                    DataSource.AutocompleteProvider
                        .Query(Arg.Is<QueryInfo>(info => info.SuggestionType == AutocompleteSuggestionType.Tags))
                        .Returns(Observable.Return(new TagSuggestion[] { tagSuggestion }));

                    ViewModel.Prepare(DefaultParameter);
                }

                [Fact, LogIfTooSlow]
                public async Task ReturnsTrueNoMatterThatAProjectIsAlreadySelected()
                {
                    ViewModel.Prepare();
                    await ViewModel.Initialize();
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty(1)
                        .WithProjectInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor);

                    ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor("abcde #fgh", 10);

                    ViewModel.SuggestCreation.Should().BeTrue();
                }

                [Fact, LogIfTooSlow]
                public async Task ReturnsTrueNoMatterThatAProjectIsAlreadySelectedAndInTagSuccestionMode()
                {
                    ViewModel.Prepare();
                    await ViewModel.Initialize();
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty(1)
                        .WithProjectInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor);
                    ViewModel.ToggleTagSuggestionsCommand.Execute();

                    ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor("abcde #fgh", 10);

                    ViewModel.SuggestCreation.Should().BeTrue();
                }
            }
        }

        public sealed class TheCreateProjectCommand
        {
            public sealed class WhenSuggestingProjects : StartTimeEntryViewModelTest
            {
                private const string currentQuery = "My awesome Toggl project";

                public WhenSuggestingProjects()
                {
                    var project = Substitute.For<IDatabaseProject>();
                    project.Id.Returns(10);
                    DataSource.Projects.GetById(Arg.Any<long>()).Returns(Observable.Return(project));
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor($"@{currentQuery}", 15);

                    ViewModel.Prepare();
                    ViewModel.Prepare(DefaultParameter);
                }

                [Fact, LogIfTooSlow]
                public async Task CallsTheCreateProjectViewModel()
                {
                    await ViewModel.CreateCommand.ExecuteAsync();

                    await NavigationService.Received()
                        .Navigate<EditProjectViewModel, string, long?>(Arg.Any<string>());
                }

                [Fact, LogIfTooSlow]
                public async Task UsesTheCurrentQueryAsTheParameterForTheCreateProjectViewModel()
                {
                    await ViewModel.CreateCommand.ExecuteAsync();

                    await NavigationService.Received()
                        .Navigate<EditProjectViewModel, string, long?>(currentQuery);
                }

                [Fact, LogIfTooSlow]
                public async Task SelectsTheCreatedProject()
                {
                    long projectId = 200;
                    NavigationService
                        .Navigate<EditProjectViewModel, string, long?>(Arg.Is(currentQuery))
                        .Returns(projectId);
                    var project = Substitute.For<IDatabaseProject>();
                    project.Id.Returns(projectId);
                    project.Name.Returns(currentQuery);
                    DataSource.Projects.GetById(Arg.Is(projectId)).Returns(Observable.Return(project));

                    await ViewModel.CreateCommand.ExecuteAsync();

                    ViewModel.TextFieldInfo.ProjectName.Should().Be(currentQuery);
                }
            }

            public sealed class WhenSuggestingTags : StartTimeEntryViewModelTest
            {
                private const string currentQuery = "My awesome Toggl project";

                public WhenSuggestingTags()
                {
                    ViewModel.Prepare();
                    ViewModel.Prepare(DefaultParameter);
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor($"#{currentQuery}", 1);
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesTagWithCurrentQueryAsName()
                {
                    await ViewModel.CreateCommand.ExecuteAsync();

                    await DataSource.Tags.Received()
                        .Create(Arg.Is(currentQuery), Arg.Any<long>());
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesTagInProjectsWorkspaceIfAProjectIsSelected()
                {
                    long workspaceId = 100;
                    long projectId = 101;
                    var project = Substitute.For<IDatabaseProject>();
                    project.Id.Returns(projectId);
                    project.WorkspaceId.Returns(workspaceId);
                    DataSource.Projects.GetById(Arg.Is(projectId))
                        .Returns(Observable.Return(project));
                    ViewModel.TextFieldInfo = ViewModel.TextFieldInfo
                        .WithProjectInfo(workspaceId, projectId, "Project", "0000AF");

                    await ViewModel.CreateCommand.ExecuteAsync();

                    await DataSource.Tags.Received()
                        .Create(Arg.Any<string>(), Arg.Is(workspaceId));
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesTagInUsersDefaultWorkspaceIfNoProjectIsSelected()
                {
                    long workspaceId = 100;
                    var user = Substitute.For<IDatabaseUser>();
                    user.DefaultWorkspaceId.Returns(workspaceId);
                    DataSource.User.Current.Returns(Observable.Return(user));
                    await ViewModel.Initialize();

                    await ViewModel.CreateCommand.ExecuteAsync();

                    await DataSource.Tags.Received()
                        .Create(Arg.Any<string>(), Arg.Is(workspaceId));
                }

                [Fact, LogIfTooSlow]
                public async Task SelectsTheCreatedTag()
                {
                    DataSource.Tags.Create(Arg.Any<string>(), Arg.Any<long>())
                        .Returns(callInfo =>
                        {
                            var tag = Substitute.For<IDatabaseTag>();
                            tag.Name.Returns(callInfo.Arg<string>());
                            return Observable.Return(tag);
                        });

                    await ViewModel.CreateCommand.ExecuteAsync();

                    ViewModel.TextFieldInfo.Tags.Should()
                        .Contain(tag => tag.Name == currentQuery);
                }
            }
        }

        public sealed class TheBackCommand : StartTimeEntryViewModelTest
        {
            public TheBackCommand()
            {
                var parameter = StartTimeEntryParameters.ForTimerMode(DateTimeOffset.Now);
                ViewModel.Prepare(parameter);
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModelIfUserDoesNotChangeAnything()
            {
                await ViewModel.BackCommand.ExecuteAsync();

                await NavigationService.Received().Close(ViewModel);
            }

            [Fact, LogIfTooSlow]
            public async Task ShowsAConfirmationDialogIfUserEnteredSomething()
            {
                makeDirty();

                await ViewModel.BackCommand.ExecuteAsync();

                await DialogService.Received().ConfirmDestructiveAction(ActionType.DiscardNewTimeEntry);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotCloseTheViewIfUserWantsToContinueEditing()
            {
                makeDirty();
                DialogService.ConfirmDestructiveAction(ActionType.DiscardNewTimeEntry)
                             .Returns(_ => Task.FromResult(false));

                await ViewModel.BackCommand.ExecuteAsync();

                await NavigationService.DidNotReceive().Close(ViewModel);
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewIfUserWantsToDiscardTheEnteredInformation()
            {
                makeDirty();
                DialogService.ConfirmDestructiveAction(ActionType.DiscardNewTimeEntry)
                             .Returns(_ => Task.FromResult(true));

                await ViewModel.BackCommand.ExecuteAsync();

                await NavigationService.Received().Close(ViewModel);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotCallTheAnalyticsServiceSinceNoTimeEntryWasCreated()
            {
                await ViewModel.BackCommand.ExecuteAsync();

                AnalyticsService.DidNotReceive().TrackStartedTimeEntry(Arg.Any<TimeEntryStartOrigin>());
            }

            private void makeDirty()
            {
                ViewModel.ToggleBillableCommand.Execute();
            }
        }

        public sealed class TheToggleBillableCommand : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void TogglesTheIsBillableProperty()
            {
                var expected = !ViewModel.IsBillable;

                ViewModel.ToggleBillableCommand.Execute();

                ViewModel.IsBillable.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsDirtyFlag()
            {
                ViewModel.ToggleBillableCommand.Execute();

                ViewModel.IsDirty.Should().BeTrue();
            }
        }

        public sealed class TheToggleProjectSuggestionsCommand : StartTimeEntryViewModelTest
        {
            public TheToggleProjectSuggestionsCommand()
            {
                var suggestions = ProjectSuggestion.FromProjects(Enumerable.Empty<IDatabaseProject>());
                AutocompleteProvider
                    .Query(Arg.Is<TextFieldInfo>(info => info.Text.Contains("@")))
                    .Returns(Observable.Return(suggestions));

                AutocompleteProvider
                    .Query(Arg.Is<QueryInfo>(
                        arg => arg.SuggestionType == AutocompleteSuggestionType.Projects))
                    .Returns(Observable.Return(suggestions));
            }

            [Fact, LogIfTooSlow]
            public void StartProjectSuggestionEvenIfTheProjectHasAlreadyBeenSelected()
            {
                ViewModel.Prepare(DefaultParameter);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1)
                    .WithTextAndCursor(Description, Description.Length)
                    .WithProjectInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.IsSuggestingProjects.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsSuggestingProjectsPropertyToTrueIfNotInProjectSuggestionMode()
            {
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.IsSuggestingProjects.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void AddsAnAtSymbolAtTheEndOfTheQueryInOrderToStartProjectSuggestionMode()
            {
                const string description = "Testing Toggl Apps";
                var expected = $"{description} @";
                ViewModel.Prepare(DefaultParameter);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(description, description.Length);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.TextFieldInfo.Text.Should().Be(expected);
            }

            [Theory, LogIfTooSlow]
            [InlineData("@")]
            [InlineData("@somequery")]
            [InlineData("@some query")]
            [InlineData("@some query@query")]
            [InlineData("Testing Toggl Apps @")]
            [InlineData("Testing Toggl Apps @somequery")]
            [InlineData("Testing Toggl Apps @some query")]
            [InlineData("Testing Toggl Apps @some query @query")]
            public void SetsTheIsSuggestingProjectsPropertyToFalseIfAlreadyInProjectSuggestionMode(string description)
            {
                ViewModel.Prepare(DefaultParameter);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(description, description.Length);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.IsSuggestingProjects.Should().BeFalse();
            }

            [Theory, LogIfTooSlow]
            [InlineData("@", "")]
            [InlineData("@somequery", "")]
            [InlineData("@some query", "")]
            [InlineData("@some query@query", "@some query")]
            [InlineData("Testing Toggl Apps @", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps @somequery", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps @some query", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps @some query @query", "Testing Toggl Apps @some query ")]
            public void RemovesTheAtSymbolFromTheDescriptionTextIfAlreadyInProjectSuggestionMode(
                string description, string expected)
            {
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(description, description.Length);

                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.TextFieldInfo.Text.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotSetTheIsDirtyFlag()
            {
                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                ViewModel.IsDirty.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void SetsProjectOrTagWasAdded()
            {
                ViewModel.ToggleProjectSuggestionsCommand.Execute();

                OnboardingStorage.Received().ProjectOrTagWasAdded();
            }
        }

        public sealed class TheToggleTagSuggestionsCommand : StartTimeEntryViewModelTest
        {
            public TheToggleTagSuggestionsCommand()
            {
                var tag = Substitute.For<IDatabaseTag>();
                tag.Id.Returns(TagId);
                tag.Name.Returns(TagName);
                var suggestions = TagSuggestion.FromTags(new[] { tag });
                AutocompleteProvider
                    .Query(Arg.Is<TextFieldInfo>(info => info.Text.Contains("#")))
                    .Returns(Observable.Return(suggestions));
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsSuggestingTagsPropertyToTrueIfNotInTagSuggestionMode()
            {
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);

                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.IsSuggestingTags.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void AddsHashtagSymbolAtTheEndOfTheQueryInOrderToTagSuggestionMode()
            {
                const string description = "Testing Toggl Apps";
                var expected = $"{description} #";
                ViewModel.Prepare(DefaultParameter);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(description, description.Length);

                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.TextFieldInfo.Text.Should().Be(expected);
            }

            [Theory, LogIfTooSlow]
            [InlineData("#")]
            [InlineData("#somequery")]
            [InlineData("#some query")]
            [InlineData("#some quer#query")]
            [InlineData("Testing Toggl Apps #")]
            [InlineData("Testing Toggl Apps #somequery")]
            [InlineData("Testing Toggl Apps #some query")]
            [InlineData("Testing Toggl Apps #some query #query")]
            public void SetsTheIsSuggestingTagsPropertyToFalseIfAlreadyInTagSuggestionMode(string description)
            {
                ViewModel.Prepare(DefaultParameter);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(description, description.Length);

                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.IsSuggestingTags.Should().BeFalse();
            }

            [Theory, LogIfTooSlow]
            [InlineData("#", "")]
            [InlineData("#somequery", "")]
            [InlineData("#some query", "")]
            [InlineData("#some query#query", "#some query")]
            [InlineData("Testing Toggl Apps #", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps #somequery", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps #some query", "Testing Toggl Apps ")]
            [InlineData("Testing Toggl Apps #some query #query", "Testing Toggl Apps #some query ")]
            public void RemovesTheHashtagSymbolFromTheDescriptionTextIfAlreadyInTagSuggestionMode(
                string description, string expected)
            {
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1)
                    .WithProjectInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor)
                    .WithTextAndCursor(description, description.Length);

                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.TextFieldInfo.Text.Should().Be(expected);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotSetTheIsDirtyFlag()
            {
                ViewModel.ToggleTagSuggestionsCommand.Execute();

                ViewModel.IsDirty.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void SetsProjectOrTagWasAdded()
            {
                ViewModel.ToggleTagSuggestionsCommand.Execute();

                OnboardingStorage.Received().ProjectOrTagWasAdded();
            }
        }

        public sealed class TheChangeTimeCommand : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task SetsTheStartDateToTheValueReturnedByTheEditDurationViewModel()
            {
                var now = DateTimeOffset.UtcNow;
                var parameter = new StartTimeEntryParameters(now, "", null);
                var parameterToReturn = DurationParameter.WithStartAndDuration(now.AddHours(-2), null);
                NavigationService
                    .Navigate<EditDurationViewModel, DurationParameter, DurationParameter>(Arg.Any<DurationParameter>())
                    .Returns(parameterToReturn);
                ViewModel.Prepare(parameter);

                await ViewModel.ChangeTimeCommand.ExecuteAsync();

                ViewModel.StartTime.Should().Be(parameterToReturn.Start);
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheIsEditingDurationDateToTrueWhileTheViewDoesNotReturnAndThenSetsItBackToFalse()
            {
                var now = DateTimeOffset.UtcNow;
                var parameter = new StartTimeEntryParameters(now, "", null);
                var parameterToReturn = DurationParameter.WithStartAndDuration(now.AddHours(-2), null);
                var tcs = new TaskCompletionSource<DurationParameter>();
                NavigationService
                    .Navigate<EditDurationViewModel, DurationParameter, DurationParameter>(Arg.Any<DurationParameter>())
                    .Returns(tcs.Task);
                ViewModel.Prepare(parameter);

                var toWait = ViewModel.ChangeTimeCommand.ExecuteAsync();
                ViewModel.IsEditingTime.Should().BeTrue();
                tcs.SetResult(parameterToReturn);
                await toWait;

                ViewModel.IsEditingTime.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheStopDateToTheValueReturnedByTheEditDurationViewModelIfUserStoppsTheTimeEntry()
            {
                var now = DateTimeOffset.UtcNow;
                var currentTimeSubject = new Subject<DateTimeOffset>();
                var parameter = new StartTimeEntryParameters(now, "", null);
                var observable = currentTimeSubject.AsObservable().Replay();
                observable.Connect();
                TimeService.CurrentDateTimeObservable.Returns(observable);
                var parameterToReturn = DurationParameter.WithStartAndDuration(now.AddHours(-2), TimeSpan.FromMinutes(30));
                var tcs = new TaskCompletionSource<DurationParameter>();
                NavigationService
                    .Navigate<EditDurationViewModel, DurationParameter, DurationParameter>(Arg.Any<DurationParameter>())
                    .Returns(tcs.Task);
                ViewModel.Prepare(parameter);

                var toWait = ViewModel.ChangeTimeCommand.ExecuteAsync();
                ViewModel.IsEditingTime.Should().BeTrue();
                tcs.SetResult(parameterToReturn);
                await toWait;

                ViewModel.IsEditingTime.Should().BeFalse();
                ViewModel.DisplayedTime.Should().Be(parameterToReturn.Duration.Value);

                currentTimeSubject.OnNext(now.AddHours(10));

                ViewModel.DisplayedTime.Should().Be(parameterToReturn.Duration.Value);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotSetTheIsDirtyFlagIfNothingChanges()
            {
                var now = DateTimeOffset.UtcNow;
                var parameter = new StartTimeEntryParameters(now, "", null);
                ViewModel.Prepare(parameter);
                var parameterToReturn = DurationParameter.WithStartAndDuration(now, null);
                NavigationService
                    .Navigate<EditDurationViewModel, DurationParameter, DurationParameter>(Arg.Any<DurationParameter>())
                    .Returns(Task.FromResult(parameterToReturn));

                await ViewModel.ChangeTimeCommand.ExecuteAsync();

                ViewModel.IsDirty.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheIsDirtyFlagWhenStartTimeChanges()
            {
                var now = DateTimeOffset.UtcNow;
                var parameter = new StartTimeEntryParameters(now, "", null);
                var parameterToReturn = DurationParameter.WithStartAndDuration(now.AddHours(-2), null);
                NavigationService
                    .Navigate<EditDurationViewModel, DurationParameter, DurationParameter>(Arg.Any<DurationParameter>())
                    .Returns(Task.FromResult(parameterToReturn));
                ViewModel.Prepare(parameter);

                await ViewModel.ChangeTimeCommand.ExecuteAsync();

                ViewModel.IsDirty.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheIsDirtyFlagWhenDurationChanges()
            {
                var now = DateTimeOffset.UtcNow;
                var parameter = new StartTimeEntryParameters(now, "", null);
                var parameterToReturn = DurationParameter.WithStartAndDuration(now, TimeSpan.FromMinutes(10));
                NavigationService
                    .Navigate<EditDurationViewModel, DurationParameter, DurationParameter>(Arg.Any<DurationParameter>())
                    .Returns(Task.FromResult(parameterToReturn));
                ViewModel.Prepare(parameter);

                await ViewModel.ChangeTimeCommand.ExecuteAsync();

                ViewModel.IsDirty.Should().BeTrue();
            }
        }

        public sealed class TheSetStartDateCommand : StartTimeEntryViewModelTest
        {
            private static readonly DateTimeOffset now = new DateTimeOffset(2018, 02, 13, 23, 59, 12, TimeSpan.FromHours(-1));
            private static readonly StartTimeEntryParameters prepareParameters = StartTimeEntryParameters.ForTimerMode(now);

            public TheSetStartDateCommand()
            {
                TimeService.CurrentDateTime.Returns(now);
            }

            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheSelectDateTimeViewModel()
            {
                ViewModel.Prepare(prepareParameters);

                await ViewModel.SetStartDateCommand.ExecuteAsync();

                await NavigationService.Received().Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>());
            }

            [Fact]
            public async Task OpensTheSelectDateTimeViewModelWithCorrectLimitsForARunnningTimeEntry()
            {
                ViewModel.Prepare();
                ViewModel.Prepare(prepareParameters);

                await ViewModel.SetStartDateCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(
                        Arg.Is<DateTimePickerParameters>(param => param.MinDate == now - MaxTimeEntryDuration && param.MaxDate == now));
            }

            [Fact]
            public async Task OpensTheSelectDateTimeViewModelWithCorrectLimitsForAStoppedTimeEntry()
            {
                var stoppedParametsrs = StartTimeEntryParameters.ForManualMode(now);
                ViewModel.Prepare(stoppedParametsrs);

                await ViewModel.SetStartDateCommand.ExecuteAsync();

                await NavigationService.Received()
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(
                        Arg.Is<DateTimePickerParameters>(param => param.MinDate == EarliestAllowedStartTime && param.MaxDate == LatestAllowedStartTime));
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheStartDateToTheValueReturnedByTheSelectDateTimeViewModel()
            {
                var parameterToReturn = now.AddDays(-2);
                NavigationService
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>())
                    .Returns(parameterToReturn);

                ViewModel.Prepare(prepareParameters);
                await ViewModel.SetStartDateCommand.ExecuteAsync();

                ViewModel.StartTime.Date.Should().Be(parameterToReturn.Date);
            }

            [Fact, LogIfTooSlow]
            public async Task RespectsTheTimeZone()
            {
                var now = new DateTimeOffset(2018, 2, 20, 13, 20, 0, TimeSpan.FromHours(5));
                TimeService.CurrentDateTime.Returns(now);
                var selectedDate = new DateTimeOffset(2018, 2, 14, 0, 0, 0, TimeSpan.FromHours(5));
                NavigationService
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>())
                    .Returns(selectedDate);

                ViewModel.Prepare(prepareParameters);
                await ViewModel.SetStartDateCommand.ExecuteAsync();

                ViewModel.StartTime.LocalDateTime.Date.Should().Be(selectedDate.LocalDateTime.Date);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotSetTheIsDirtyFlagWhenNothingChanges()
            {
                ViewModel.Prepare(prepareParameters);
                NavigationService
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>())
                    .Returns(prepareParameters.StartTime);

                await ViewModel.SetStartDateCommand.ExecuteAsync();

                ViewModel.IsDirty.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheIsDirtyFlagWhenTheStartTimeChanges()
            {
                var parameterToReturn = now.AddDays(-2);
                NavigationService
                    .Navigate<SelectDateTimeViewModel, DateTimePickerParameters, DateTimeOffset>(Arg.Any<DateTimePickerParameters>())
                    .Returns(parameterToReturn);

                ViewModel.Prepare(prepareParameters);
                await ViewModel.SetStartDateCommand.ExecuteAsync();

                ViewModel.IsDirty.Should().BeTrue();
            }
        }

        public sealed class TheDoneCommand : StartTimeEntryViewModelTest
        {
            public sealed class StartsANewTimeEntry : StartTimeEntryViewModelTest
            {
                private const long taskId = 9;
                private const long userId = 10;
                private const long projectId = 11;
                private const long defaultWorkspaceId = 12;
                private const long projectWorkspaceId = 13;
                private const string description = "Testing Toggl apps";

                private readonly IDatabaseUser user = Substitute.For<IDatabaseUser>();
                private readonly IDatabaseProject project = Substitute.For<IDatabaseProject>();

                private readonly DateTimeOffset startDate = DateTimeOffset.UtcNow;

                public StartsANewTimeEntry()
                {
                    user.Id.Returns(userId);
                    user.DefaultWorkspaceId.Returns(defaultWorkspaceId);
                    DataSource.User.Current
                        .Returns(Observable.Return(user));

                    project.Id.Returns(projectId);
                    project.WorkspaceId.Returns(projectWorkspaceId);
                    DataSource.Projects
                         .GetById(projectId)
                         .Returns(Observable.Return(project));

                    var parameter = new StartTimeEntryParameters(startDate, "", null);
                    ViewModel.Prepare(parameter);
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty(defaultWorkspaceId);
                
                }

                [Fact, LogIfTooSlow]
                public async Task CallsTheCreateTimeEntryInteractor()
                {
                    await ViewModel.DoneCommand.ExecuteAsync();

                    InteractorFactory.Received().CreateTimeEntry(ViewModel);
                }

                [Fact, LogIfTooSlow]
                public async Task ExecutesTheCreateTimeEntryInteractor()
                {
                    var mockedInteractor = Substitute.For<IInteractor<IObservable<IDatabaseTimeEntry>>>();
                    InteractorFactory.CreateTimeEntry(Arg.Any<ITimeEntryPrototype>()).Returns(mockedInteractor);

                    await ViewModel.DoneCommand.ExecuteAsync();

                    await mockedInteractor.Received().Execute();
                }

                [Theory, LogIfTooSlow]
                [InlineData(null)]
                [InlineData(" ")]
                [InlineData("\t")]
                [InlineData("\n")]
                [InlineData("               ")]
                [InlineData("      \t  \n     ")]
                public async Task ReducesDescriptionConsistingOfOnlyEmptyCharactersToAnEmptyString(string description)
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(description, 0);

                    await ViewModel.DoneCommand.ExecuteAsync();

                    InteractorFactory.Received().CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(timeEntry =>
                        timeEntry.Description.Length == 0
                    ));
                }

                [Theory, LogIfTooSlow]
                [InlineData("   abcde", "abcde")]
                [InlineData("abcde     ", "abcde")]
                [InlineData("  abcde ", "abcde")]
                [InlineData("abcde  fgh", "abcde  fgh")]
                [InlineData("      abcd\nefgh     ", "abcd\nefgh")]
                public async Task TrimsDescriptionFromTheStartAndTheEndBeforeSaving(string description, string trimmed)
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(description, description.Length);

                    await ViewModel.DoneCommand.ExecuteAsync();

                    InteractorFactory.Received().CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(timeEntry =>
                        timeEntry.Description == trimmed
                    ));
                }

                [Property]
                public void DoesNotCreateARunningTimeEntryWhenDurationIsNotNull(TimeSpan duration)
                {
                    if (duration < TimeSpan.Zero) return;
                    
                    var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", duration);

                    ViewModel.Prepare(parameter);
                    ViewModel.DoneCommand.ExecuteAsync().Wait();

                    InteractorFactory.Received().CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(timeEntry =>
                        timeEntry.Duration.HasValue
                    ));
                }

                [Fact, LogIfTooSlow]
                public async Task CreatesARunningTimeEntryWhenDurationIsNull()
                {
                    var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", null);

                    ViewModel.Prepare(parameter);
                    await ViewModel.DoneCommand.ExecuteAsync();

                    InteractorFactory.Received().CreateTimeEntry(Arg.Is<ITimeEntryPrototype>(timeEntry =>
                        timeEntry.Duration.HasValue == false
                    ));
                }

                private TagSuggestion tagSuggestionFromInt(int i)
                {
                    var tag = Substitute.For<IDatabaseTag>();
                    tag.Id.Returns(i);
                    tag.Name.Returns(i.ToString());

                    return new TagSuggestion(tag);
                }
            }

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                var user = Substitute.For<IDatabaseUser>();
                user.Id.Returns(1);
                user.DefaultWorkspaceId.Returns(10);
                DataSource.User.Current.Returns(Observable.Return(user));
                var parameter = new StartTimeEntryParameters(DateTimeOffset.Now, "", null);
                ViewModel.Prepare(parameter);
                await ViewModel.Initialize();

                await ViewModel.DoneCommand.ExecuteAsync();

                await NavigationService.Received().Close(ViewModel);
            }
        }

        public sealed class TheSelectSuggestionCommand
        {
            public abstract class SelectSuggestionTest<TSuggestion> : StartTimeEntryViewModelTest
                where TSuggestion : AutocompleteSuggestion
            {
                protected IDatabaseTag Tag { get; }
                protected IDatabaseTask Task { get; }
                protected IDatabaseProject Project { get; }
                protected IDatabaseTimeEntry TimeEntry { get; }
                protected IDatabaseWorkspace Workspace { get; }

                protected abstract TSuggestion Suggestion { get; }

                protected SelectSuggestionTest()
                {
                    Workspace = Substitute.For<IDatabaseWorkspace>();
                    Workspace.Id.Returns(WorkspaceId);

                    Project = Substitute.For<IDatabaseProject>();
                    Project.Id.Returns(ProjectId);
                    Project.Name.Returns(ProjectName);
                    Project.Color.Returns(ProjectColor);
                    Project.Workspace.Returns(Workspace);
                    Project.WorkspaceId.Returns(WorkspaceId);

                    Task = Substitute.For<IDatabaseTask>();
                    Task.Id.Returns(TaskId);
                    Task.Project.Returns(Project);
                    Task.ProjectId.Returns(ProjectId);
                    Task.WorkspaceId.Returns(WorkspaceId);
                    Task.Name.Returns(TaskId.ToString());

                    TimeEntry = Substitute.For<IDatabaseTimeEntry>();
                    TimeEntry.Description.Returns(Description);
                    TimeEntry.Project.Returns(Project);

                    Tag = Substitute.For<IDatabaseTag>();
                    Tag.Id.Returns(TagId);
                    Tag.Name.Returns(TagName);
                }

                [Fact, LogIfTooSlow]
                public void SetsTheIsDirtyFlag()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.IsDirty.Should().BeTrue();
                }
            }

            public abstract class ProjectSettingSuggestion<TSuggestion> : SelectSuggestionTest<TSuggestion>
                where TSuggestion : AutocompleteSuggestion
            {
                [Fact, LogIfTooSlow]
                public void SetsTheProjectIdToTheSuggestedProjectId()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.ProjectId.Should().Be(ProjectId);
                }

                [Fact, LogIfTooSlow]
                public void SetsTheProjectNameToTheSuggestedProjectName()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.ProjectName.Should().Be(ProjectName);
                }

                [Fact, LogIfTooSlow]
                public void SetsTheProjectColorToTheSuggestedProjectColor()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.ProjectColor.Should().Be(ProjectColor);
                }

                [Theory, LogIfTooSlow]
                [InlineData(true)]
                [InlineData(false)]
                [InlineData(null)]
                public void SetsTheAppropriateBillableValue(bool? billableValue)
                {
                    InteractorFactory.GetWorkspaceById(WorkspaceId).Execute().Returns(Observable.Return(Workspace));
                    InteractorFactory.IsBillableAvailableForProject(ProjectId).Execute()
                        .Returns(Observable.Return(true));
                    InteractorFactory.ProjectDefaultsToBillable(ProjectId).Execute()
                        .Returns(Observable.Return(billableValue ?? false));

                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.IsBillable.Should().Be(billableValue ?? false);
                    ViewModel.IsBillableAvailable.Should().BeTrue();
                }

                [Theory, LogIfTooSlow]
                [InlineData(true)]
                [InlineData(false)]
                [InlineData(null)]
                public void DisablesBillableIfTheWorkspaceOfTheSelectedProjectDoesNotAllowIt(bool? billableValue)
                {
                    Project.Billable.Returns(billableValue);
                    DataSource.Projects.GetById(ProjectId).Returns(Observable.Return(Project));
                    InteractorFactory.GetWorkspaceById(WorkspaceId).Execute().Returns(Observable.Return(Workspace));
                    InteractorFactory
                        .IsBillableAvailableForWorkspace(10)
                        .Execute()
                        .Returns(Observable.Return(false));

                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.IsBillable.Should().BeFalse();
                    ViewModel.IsBillableAvailable.Should().BeFalse();
                }
            }

            public abstract class ProjectTaskSuggestion<TSuggestion> : ProjectSettingSuggestion<TSuggestion>
                where TSuggestion : AutocompleteSuggestion
            {
                protected ProjectTaskSuggestion()
                {
                    ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor("Something @togg", 15);
                }

                [Fact, LogIfTooSlow]
                public void RemovesTheProjectQueryFromTheTextFieldInfo()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Text.Should().Be("Something ");
                }

                [Fact, LogIfTooSlow]
                public async Task ShowsConfirmDialogIfWorkspaceIsAboutToBeChanged()
                {
                    var user = Substitute.For<IDatabaseUser>();
                    user.DefaultWorkspaceId.Returns(100);
                    DataSource.User.Current.Returns(Observable.Return(user));
                    ViewModel.Prepare();
                    await ViewModel.Initialize();

                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    await DialogService.Received().Confirm(
                        Arg.Is(Resources.DifferentWorkspaceAlertTitle),
                        Arg.Is(Resources.DifferentWorkspaceAlertMessage),
                        Arg.Is(Resources.Ok),
                        Arg.Is(Resources.Cancel)
                    );
                }

                [Fact, LogIfTooSlow]
                public async Task DoesNotShowConfirmDialogIfWorkspaceIsNotGoingToChange()
                {
                    var user = Substitute.For<IDatabaseUser>();
                    user.DefaultWorkspaceId.Returns(WorkspaceId);
                    DataSource.User.Current.Returns(Observable.Return(user));
                    ViewModel.Prepare();
                    await ViewModel.Initialize();

                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    await DialogService.DidNotReceive().Confirm(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<string>()
                    );
                }

                [Fact, LogIfTooSlow]
                public async Task ClearsTagsIfWorkspaceIsChanged()
                {
                    var user = Substitute.For<IDatabaseUser>();
                    user.DefaultWorkspaceId.Returns(100);
                    DataSource.User.Current.Returns(Observable.Return(user));
                    ViewModel.Prepare();
                    await ViewModel.Initialize();
                    Enumerable.Range(100, 10)
                        .Select(i =>
                        {
                            var tag = Substitute.For<IDatabaseTag>();
                            tag.Id.Returns(i);
                            return new TagSuggestion(tag);
                        }).ForEach(ViewModel.SelectSuggestionCommand.Execute);

                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Tags.Should().BeEmpty();
                }
            }

            public sealed class WhenSelectingATimeEntrySuggestion : ProjectSettingSuggestion<TimeEntrySuggestion>
            {
                protected override TimeEntrySuggestion Suggestion { get; }

                public WhenSelectingATimeEntrySuggestion()
                {
                    Suggestion = new TimeEntrySuggestion(TimeEntry);
                }

                [Fact, LogIfTooSlow]
                public void SetsTheTextFieldInfoTextToTheValueOfTheSuggestedDescription()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Text.Should().Be(Description);
                }
            }

            public sealed class WhenSelectingATaskSuggestion : ProjectTaskSuggestion<TaskSuggestion>
            {
                protected override TaskSuggestion Suggestion { get; }

                public WhenSelectingATaskSuggestion()
                {
                    Suggestion = new TaskSuggestion(Task);
                }

                [Fact, LogIfTooSlow]
                public void SetsTheTaskIdToTheSameIdAsTheSelectedSuggestion()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.TaskId.Should().Be(TaskId);
                }
            }

            public sealed class WhenSelectingAProjectSuggestion : ProjectTaskSuggestion<ProjectSuggestion>
            {
                protected override ProjectSuggestion Suggestion { get; }

                public WhenSelectingAProjectSuggestion()
                {
                    Suggestion = new ProjectSuggestion(Project);
                }

                [Fact, LogIfTooSlow]
                public void SetsTheTaskIdToNull()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.TaskId.Should().BeNull();
                }
            }

            public sealed class WhenSelectingATagSuggestion : SelectSuggestionTest<TagSuggestion>
            {
                protected override TagSuggestion Suggestion { get; }

                public WhenSelectingATagSuggestion()
                {
                    Suggestion = new TagSuggestion(Tag);

                    ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor("Something #togg", 15);
                }

                [Fact, LogIfTooSlow]
                public void RemovesTheTagQueryFromTheTextFieldInfo()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Text.Should().Be("Something ");
                }

                [Fact, LogIfTooSlow]
                public void AddsTheSuggestedTagToTheList()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Tags.Should().Contain(Suggestion);
                }
            }

            public sealed class WhenSelectingAQuerySymbolSuggestion : SelectSuggestionTest<QuerySymbolSuggestion>
            {
                protected override QuerySymbolSuggestion Suggestion { get; } = QuerySymbolSuggestion.Suggestions.First();

                [Fact, LogIfTooSlow]
                public void SetsTheTextToTheQuerySymbolSelected()
                {
                    ViewModel.SelectSuggestionCommand.Execute(Suggestion);

                    ViewModel.TextFieldInfo.Text.Should().Be(Suggestion.Symbol);
                }
            }
        }

        public sealed class TheSuggestionsProperty : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void IsClearedWhenThereAreNoWordsToQuery()
            {
                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor("", 0);

                ViewModel.Suggestions.Should().HaveCount(0);
            }

            [Fact, LogIfTooSlow]
            public void DoesNotSuggestAnythingWhenAProjectIsAlreadySelected()
            {
                var description = "abc";
                var projectA = Substitute.For<IDatabaseProject>();
                projectA.Id.Returns(ProjectId);
                var projectB = Substitute.For<IDatabaseProject>();
                projectB.Id.Returns(ProjectId + 1);
                var timeEntryA = Substitute.For<IDatabaseTimeEntry>();
                timeEntryA.Description.Returns(description);
                timeEntryA.Project.Returns(projectA);
                var timeEntryB = Substitute.For<IDatabaseTimeEntry>();
                timeEntryB.Description.Returns(description);
                timeEntryB.Project.Returns(projectB);
                var suggestions = Observable.Return(new AutocompleteSuggestion[]
                    {
                        new TimeEntrySuggestion(timeEntryA),
                        new TimeEntrySuggestion(timeEntryB) 
                    });
                AutocompleteProvider.Query(Arg.Any<QueryInfo>()).Returns(suggestions);
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);
                ViewModel.TextFieldInfo =
                    TextFieldInfo.Empty(1).WithProjectInfo(WorkspaceId, ProjectId, ProjectName, ProjectColor);

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor(description, description.Length);

                ViewModel.Suggestions.Should().HaveCount(1);
                ViewModel.Suggestions[0].Should().HaveCount(1);
                var suggestion = ViewModel.Suggestions[0][0]; 
                suggestion.Should().BeOfType<TimeEntrySuggestion>();
                ((TimeEntrySuggestion)suggestion).ProjectId.Should().Be(ProjectId);
            }
        }

        public sealed class TheTextFieldInfoProperty : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("abc @def")]
            [InlineData("abc #def")]
            public void DoesNotChangeSuggestionsWhenOnlyTheCursorMovesForward(string text)
            {
                ViewModel.Prepare(DefaultParameter);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(text, text.Length);
                AutocompleteProvider.ClearReceivedCalls();

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor(text, 0);

                AutocompleteProvider.DidNotReceive().Query(Arg.Any<QueryInfo>());
            }

            [Theory, LogIfTooSlow]
            [InlineData("abc @def")]
            [InlineData("abc #def")]
            public void ChangesSuggestionsWhenTheCursorMovesBackBehindTheOldCursorPosition(string text)
            {
                var extendedText = text + "x";
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(extendedText, text.Length);
                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor(extendedText, 0);
                AutocompleteProvider.ClearReceivedCalls();

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor(extendedText, extendedText.Length);

                AutocompleteProvider.Received().Query(Arg.Any<QueryInfo>());
            }

            [Theory, LogIfTooSlow]
            [InlineData("abc @def")]
            [InlineData("abc #def")]
            public void ChangesSuggestionsWhenTheCursorMovesBeforeTheQuerySymbolAndUserStartsTyping(string text)
            {
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(text, text.Length);
                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor(text, 0);
                AutocompleteProvider.ClearReceivedCalls();

                ViewModel.TextFieldInfo = ViewModel.TextFieldInfo.WithTextAndCursor("x" + text, 1);

                AutocompleteProvider.Received().Query(Arg.Is<QueryInfo>(query => query.Text.StartsWith("x")));
            }

            [Fact, LogIfTooSlow]
            public void DoesNotSetTheIsDirtyFlagIfTheTextFieldIsEmpty()
            {
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);

                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1);

                ViewModel.IsDirty.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void SetsTheIsDirtyFlag()
            {
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);

                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor("a", 1);

                ViewModel.IsDirty.Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public void ClearsTheIsDirtyFlagIfTheTextFieldIsErased()
            {
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);

                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor("a", 1);
                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1);

                ViewModel.IsDirty.Should().BeFalse();
            }
        }

        public sealed class TheDescriptionRemainingBytesProperty : StartTimeEntryViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void IsMaxIfTheTextIsEmpty()
            {
                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1);

                ViewModel.DescriptionRemainingBytes.Should()
                    .Be(MaxTimeEntryDescriptionLengthInBytes);
            }

            [Theory, LogIfTooSlow]
            [InlineData("Hello fox")]
            [InlineData("Some emojis: 🔥😳👻")]
            [InlineData("Some weird characters: āčēļķīņš")]
            [InlineData("Some random arabic characters: ظۓڰڿڀ")]
            public void IsDecreasedForEachByteInTheText(string text)
            {
                var expectedRemainingByteCount
                    = MaxTimeEntryDescriptionLengthInBytes - text.LengthInBytes();

                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(text, 0);

                ViewModel.DescriptionRemainingBytes.Should()
                    .Be(expectedRemainingByteCount);
            }

            [Fact, LogIfTooSlow]
            public void IsNegativeWhenTextLengthExceedsMax()
            {
                var bytesOverLimit = 5;
                var longString = new string('0', MaxTimeEntryDescriptionLengthInBytes + bytesOverLimit);

                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(longString, 0);

                ViewModel.DescriptionRemainingBytes.Should().Be(-bytesOverLimit);
            }
        }

        public sealed class TheDescriptionLengthExceededproperty : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(0)]
            [InlineData(20)]
            [InlineData(2999)]
            [InlineData(3000)]
            public void IsFalseIfTextIsShorterOrEqualToMax(int byteCount)
            {
                var text = new string('0', byteCount);

                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(text, 0);

                ViewModel.DescriptionLengthExceeded.Should().BeFalse();
            }

            [Theory, LogIfTooSlow]
            [InlineData(MaxTimeEntryDescriptionLengthInBytes + 1)]
            [InlineData(MaxTimeEntryDescriptionLengthInBytes + 20)]
            public void IsTrueWhenTextIsLongerThanMax(int byteCount)
            {
                var text = new string('0', byteCount);

                ViewModel.TextFieldInfo = TextFieldInfo.Empty(1).WithTextAndCursor(text, 0);

                ViewModel.DescriptionLengthExceeded.Should().BeTrue();
            }
        }

        public sealed class TheShouldShowNoTagsInfoMessage : StartTimeEntryViewModelTest
        {
            [Theory, LogIfTooSlow]
            [InlineData("")]
            [InlineData("asd ")]
            [InlineData("\tasd asd ")]
            [InlineData("x")]
            public async Task ReturnsTrueWhenSuggestingTagsAndUserHasNoTags(string query)
            {
                ViewModel.Prepare();
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.Initialize();
                ViewModel.TextFieldInfo = TextFieldInfo
                    .Empty(1)
                    .WithTextAndCursor($"{QuerySymbols.Tags}{query}", 1);
                
                ViewModel.ShouldShowNoTagsInfoMessage.Should().BeTrue();
            }

            [Theory, LogIfTooSlow]
            [InlineData(1, "")]
            [InlineData(2, "#tag")]
            [InlineData(8, "@Project")]
            [InlineData(3, "Time entry")]
            [InlineData(1, "#")]
            public async Task ReturnsFalseInAnyOtherCase(int tagCount, string query)
            {
                var tags = Enumerable
                    .Range(0, tagCount)
                    .Select(_ => Substitute.For<IDatabaseTag>());
                DataSource.Tags.GetAll().Returns(Observable.Return(tags));
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.Initialize();
                ViewModel.TextFieldInfo = TextFieldInfo
                    .Empty(1)
                    .WithTextAndCursor(query, 1);

                ViewModel.ShouldShowNoTagsInfoMessage.Should().BeFalse();
                                     
            }

            [Theory, LogIfTooSlow]
            [InlineData("")]
            [InlineData("asd ")]
            [InlineData("\tasd asd ")]
            [InlineData("x")]
            public async Task ReturnsFalseAfterCreatingATag(string query)
            {
                var tag = Substitute.For<IDatabaseTag>();
                DataSource
                    .Tags.Create(Arg.Any<string>(), Arg.Any<long>())
                    .Returns(Observable.Return(tag));
                ViewModel.Prepare(DefaultParameter);
                await ViewModel.Initialize();
                ViewModel.TextFieldInfo = TextFieldInfo
                    .Empty(1)
                    .WithWorkspace(10)
                    .WithTextAndCursor($"{QuerySymbols.Tags}{query}", 1);

                ViewModel.CreateCommand.Execute();

                ViewModel.ShouldShowNoTagsInfoMessage.Should().BeFalse();
            }
        }
    }
}
