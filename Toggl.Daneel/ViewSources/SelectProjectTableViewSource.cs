﻿using System;
using Foundation;
using Toggl.Daneel.Views;
using Toggl.Foundation.Autocomplete.Suggestions;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class SelectProjectTableViewSource : GroupedCollectionTableViewSource<AutocompleteSuggestion>
    {
        private const string cellIdentifier = nameof(ProjectSuggestionViewCell);
        private const string headerCellIdentifier = nameof(WorkspaceHeaderViewCell);

        public SelectProjectTableViewSource(UITableView tableView)
            : base(tableView, cellIdentifier, headerCellIdentifier)
        {
            tableView.RegisterNibForCellReuse(ProjectSuggestionViewCell.Nib, cellIdentifier);
            tableView.RegisterNibForHeaderFooterViewReuse(WorkspaceHeaderViewCell.Nib, headerCellIdentifier);
        }

        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath) => 48;

        public override nfloat GetHeightForHeader(UITableView tableView, nint section) => 40;
    }
}