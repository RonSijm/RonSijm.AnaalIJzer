using System.Collections.Immutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RonSijm.AnaalIJzer.Graphing.Model;

namespace RonSijm.AnaalIJzer.GraphEditor.Wpf.Controls;

public sealed partial class ArchitectureGraphEditorControl
{
	private bool showActiveExceptionReviews = true;
	private bool showInvalidExceptionReviews = true;
	private bool showExpiringSoonExceptionReviews = true;
	private bool showExpiredExceptionReviews = true;
	private bool showStaleExceptionReviews = true;

	private void AddExceptionReviewSection(StackPanel panel, string? ownerLayerPath)
	{
		var reviews = GetVisibleExceptionReviews(ownerLayerPath);
		panel.Children.Add(CreateSectionTitle("Exception reviews"));
		panel.Children.Add(CreateHintTextBlock("These are ARCH017 review items. Filter by status to focus on invalid, expiring, expired, or stale exceptions.", new Thickness(0, 0, 0, 4)));
		panel.Children.Add(CreateExceptionReviewFilterPanel());
		if (reviews.Length == 0)
		{
			panel.Children.Add(CreateHintTextBlock("No exception reviews match the current filter.", new Thickness(0, 4, 0, 4)));
			return;
		}

		foreach (var review in reviews)
		{
			panel.Children.Add(CreateExceptionReviewEditor(review));
		}
	}

	private UIElement CreateExceptionReviewFilterPanel()
	{
		var panel = new WrapPanel { Margin = new Thickness(0, 0, 0, 4) };
		panel.Children.Add(CreateExceptionStatusCheckBox("Active", showActiveExceptionReviews, value => showActiveExceptionReviews = value));
		panel.Children.Add(CreateExceptionStatusCheckBox("Invalid", showInvalidExceptionReviews, value => showInvalidExceptionReviews = value));
		panel.Children.Add(CreateExceptionStatusCheckBox("ExpiringSoon", showExpiringSoonExceptionReviews, value => showExpiringSoonExceptionReviews = value));
		panel.Children.Add(CreateExceptionStatusCheckBox("Expired", showExpiredExceptionReviews, value => showExpiredExceptionReviews = value));
		panel.Children.Add(CreateExceptionStatusCheckBox("Stale", showStaleExceptionReviews, value => showStaleExceptionReviews = value));

		return panel;
	}

	private CheckBox CreateExceptionStatusCheckBox(string label, bool isChecked, Action<bool> setter)
	{
		var checkBox = new CheckBox
		{
			Content = label,
			IsChecked = isChecked,
			Margin = new Thickness(0, 0, 8, 4)
		};
		checkBox.Checked += (_, _) =>
		{
			setter(true);
			RenderSelection(currentSelection);
		};
		checkBox.Unchecked += (_, _) =>
		{
			setter(false);
			RenderSelection(currentSelection);
		};

		return checkBox;
	}

	private UIElement CreateExceptionReviewEditor(ArchitectureGraphExceptionReview review)
	{
		var statusBrush = GetExceptionStatusBrush(review.Status);
		var header = $"[{review.Status}] {review.MatcherKind} {review.MatcherLabel}";
		var expander = new Expander
		{
			Header = header,
			IsExpanded = review.Status is "Invalid" or "Expired",
			Margin = new Thickness(0, 4, 0, 0),
			Foreground = statusBrush
		};
		var panel = new StackPanel();
		if (!string.IsNullOrWhiteSpace(review.OwnerLayerPath))
		{
			AddReadOnlyRow(panel, "Owner layer", review.OwnerLayerPath);
		}

		AddReadOnlyRow(panel, "Message", review.Message);
		AddReadOnlyRow(panel, "Reason", string.IsNullOrWhiteSpace(review.Reason) ? "(none)" : review.Reason!);
		AddReadOnlyRow(panel, "Owner", string.IsNullOrWhiteSpace(review.Owner) ? "(none)" : review.Owner!);
		AddReadOnlyRow(panel, "Expires on", string.IsNullOrWhiteSpace(review.ExpiresOn) ? "(none)" : review.ExpiresOn!);
		AddReadOnlyRow(panel, "Source", review.SourcePath + (review.XmlLineNumber > 0 ? ":" + review.XmlLineNumber : string.Empty));
		expander.Content = panel;

		return expander;
	}

	private Brush GetExceptionStatusBrush(string status)
	{
		var result = status switch
		{
			"Invalid" => theme.ErrorForeground,
			"Expired" => theme.ErrorForeground,
			"ExpiringSoon" => Brushes.DarkOrange,
			"Stale" => Brushes.DarkOrange,
			_ => theme.SuccessForeground
		};

		return result;
	}

	private System.Collections.Immutable.ImmutableArray<ArchitectureGraphExceptionReview> GetVisibleExceptionReviews(string? ownerLayerPath)
	{
		var result = snapshot.ExceptionReviews
			.Where(review => MatchesExceptionReviewOwner(review.OwnerLayerPath, ownerLayerPath))
			.Where(IsExceptionReviewVisible)
			.OrderBy(review => GetExceptionStatusSortOrder(review.Status))
			.ThenBy(review => review.OwnerLayerPath, StringComparer.Ordinal)
			.ThenBy(review => review.MatcherLabel, StringComparer.Ordinal)
			.ToImmutableArray();

		return result;
	}

	private bool IsExceptionReviewVisible(ArchitectureGraphExceptionReview review)
	{
		var result = review.Status switch
		{
			"Active" => showActiveExceptionReviews,
			"Invalid" => showInvalidExceptionReviews,
			"ExpiringSoon" => showExpiringSoonExceptionReviews,
			"Expired" => showExpiredExceptionReviews,
			"Stale" => showStaleExceptionReviews,
			_ => true
		};

		return result;
	}

	private static bool MatchesExceptionReviewOwner(string reviewOwnerLayerPath, string? selectedOwnerLayerPath)
	{
		if (string.IsNullOrWhiteSpace(selectedOwnerLayerPath))
		{
			return true;
		}

		var result = string.Equals(reviewOwnerLayerPath, selectedOwnerLayerPath, StringComparison.Ordinal)
		             || reviewOwnerLayerPath.StartsWith(selectedOwnerLayerPath + "/", StringComparison.Ordinal);

		return result;
	}

	private static int GetExceptionStatusSortOrder(string status)
	{
		var result = status switch
		{
			"Invalid" => 0,
			"Expired" => 1,
			"ExpiringSoon" => 2,
			"Stale" => 3,
			"Active" => 4,
			_ => 5
		};

		return result;
	}
}
