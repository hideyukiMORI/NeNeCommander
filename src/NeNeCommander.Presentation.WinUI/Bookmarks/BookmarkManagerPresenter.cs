using System;
using System.Collections.Generic;
using System.Linq;
using NeNeCommander.Application.Bookmarks;
using NeNeCommander.Application.Input;
using NeNeCommander.Application.Settings;
using NeNeCommander.Presentation.WinUI.Input;
using NeNeCommander.Presentation.WinUI.Settings;

namespace NeNeCommander.Presentation.WinUI.Bookmarks;

/// <summary>Projects session-owned bookmark editor state without making catalog decisions.</summary>
public static class BookmarkManagerPresenter
{
    /// <summary>Projects one complete settings snapshot using localized reserved-category labels.</summary>
    public static BookmarkManagerPresentation Present(
        SettingsSnapshot snapshot,
        string allCategoriesText,
        string uncategorizedText)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(allCategoriesText);
        ArgumentException.ThrowIfNullOrWhiteSpace(uncategorizedText);
        BookmarkCatalog catalog = snapshot.Settings.Bookmarks;
        BookmarksEditorState state = snapshot.BookmarksEditor;
        BookmarkBrowseContext context = ResolveContext(state);
        List<BookmarkCategoryOption> categories =
            CreateCategories(catalog, context.Filter, allCategoriesText, uncategorizedText);
        BookmarkBrowsePresentation browse = CreateBrowse(
            catalog,
            context,
            categories,
            uncategorizedText);
        BookmarkCategoryFilter draftFilter = state is BookmarkDrafting draft
            ? draft.Draft.Category
            : context.Filter;
        List<BookmarkCategoryOption> draftCategories =
            CreateCategories(catalog, draftFilter, allCategoriesText, uncategorizedText);
        BookmarkEditorDetails details = new(
            state,
            draftCategories
                .Where(option => option.Filter is not BookmarkAllCategoryFilter)
                .ToList(),
            CreateShortcutOptions(state),
            StatusResourceKey(state));
        BookmarkPersistencePresentation persistence = new(
            SettingsPresenter.PresentSaveStatus(snapshot.Persistence),
            SettingsPresenter.PresentWarning(snapshot.Persistence));
        return new BookmarkManagerPresentation(
            snapshot.Editor,
            browse,
            details,
            persistence);
    }

    private static BookmarkBrowseContext ResolveContext(BookmarksEditorState state)
    {
        return state switch
        {
            BookmarksBrowsing browsing => browsing.Context,
            BookmarkDrafting draft => draft.ReturnContext,
            BookmarkCategoryDrafting category => category.ReturnContext,
            BookmarkCategoryDeleteConfirmation deletion => deletion.ReturnContext,
            BookmarkNavigationPending pending => pending.ReturnContext,
            BookmarkNavigationFailed failed => failed.ReturnContext,
            _ => new BookmarkBrowseContext(string.Empty, BookmarkCategoryFilter.All, null),
        };
    }

    private static List<BookmarkCategoryOption> CreateCategories(
        BookmarkCatalog catalog,
        BookmarkCategoryFilter selected,
        string allCategoriesText,
        string uncategorizedText)
    {
        List<BookmarkCategoryOption> options =
        [
            new(
                BookmarkCategoryFilter.All,
                allCategoriesText,
                null,
                selected is BookmarkAllCategoryFilter
                    ? BookmarkOptionSelection.Selected
                    : BookmarkOptionSelection.NotSelected),
            new(
                BookmarkCategoryFilter.Uncategorized,
                uncategorizedText,
                null,
                selected is BookmarkUncategorizedCategoryFilter
                    ? BookmarkOptionSelection.Selected
                    : BookmarkOptionSelection.NotSelected),
        ];
        foreach (BookmarkCategoryName category in catalog.Categories)
        {
            BookmarkCategoryFilter filter = BookmarkCategoryFilter.For(category);
            options.Add(new BookmarkCategoryOption(
                filter,
                category.Value,
                catalog.Select(category),
                selected == filter
                    ? BookmarkOptionSelection.Selected
                    : BookmarkOptionSelection.NotSelected));
        }
        return options;
    }

    private static BookmarkBrowsePresentation CreateBrowse(
        BookmarkCatalog catalog,
        BookmarkBrowseContext context,
        IReadOnlyList<BookmarkCategoryOption> categories,
        string uncategorizedText)
    {
        List<BookmarkRow> rows = [];
        BookmarkRow? selectedRow = null;
        foreach (BookmarkEntry entry in catalog.Bookmarks)
        {
            if (!Matches(entry, context, uncategorizedText))
            {
                continue;
            }
            BookmarkSelection selection = new(entry);
            BookmarkRow row = new(
                selection,
                entry.Category?.Value ?? uncategorizedText,
                ShortcutLabel(entry.ShortcutSlot),
                context.Selection == selection
                    ? BookmarkOptionSelection.Selected
                    : BookmarkOptionSelection.NotSelected);
            rows.Add(row);
            if (row.IsSelected)
            {
                selectedRow = row;
            }
        }
        return new BookmarkBrowsePresentation(context.SearchText, categories, rows, selectedRow);
    }

    private static bool Matches(
        BookmarkEntry entry,
        BookmarkBrowseContext context,
        string uncategorizedText)
    {
        bool categoryMatches = context.Filter switch
        {
            BookmarkAllCategoryFilter => true,
            BookmarkUncategorizedCategoryFilter => entry.Category is null,
            BookmarkUserCategoryFilter user => StringComparer.OrdinalIgnoreCase.Equals(
                entry.Category?.Value,
                user.Category.Value),
            _ => false,
        };
        if (!categoryMatches)
        {
            return false;
        }
        string categoryText = entry.Category?.Value ?? uncategorizedText;
        return string.IsNullOrEmpty(context.SearchText) ||
            entry.Name.Value.Contains(context.SearchText, StringComparison.OrdinalIgnoreCase) ||
            entry.Path.Value.CanonicalText.Contains(context.SearchText, StringComparison.OrdinalIgnoreCase) ||
            categoryText.Contains(context.SearchText, StringComparison.OrdinalIgnoreCase);
    }

    private static List<BookmarkShortcutOption> CreateShortcutOptions(BookmarksEditorState state)
    {
        BookmarkShortcutSlot? selected = (state as BookmarkDrafting)?.Draft.Shortcut;
        List<BookmarkShortcutOption> options =
        [
            new(
                null,
                "BookmarkShortcutUnassigned",
                selected is null
                    ? BookmarkOptionSelection.Selected
                    : BookmarkOptionSelection.NotSelected),
        ];
        foreach (BookmarkShortcutSlot slot in BookmarkShortcutSlot.All)
        {
            options.Add(new BookmarkShortcutOption(
                slot,
                ShortcutLabel(slot),
                selected == slot
                    ? BookmarkOptionSelection.Selected
                    : BookmarkOptionSelection.NotSelected));
        }
        return options;
    }

    private static string ShortcutLabel(BookmarkShortcutSlot? slot)
    {
        if (slot is null)
        {
            return "KeyLabelUnmapped";
        }
        KeyBinding? binding = KeyboardIntentMapper.BindingsFor(KeyboardContext.FileList)
            .FirstOrDefault(candidate =>
                candidate.Intent is BookmarkShortcutSelection shortcut &&
                shortcut.Slot == slot);
        return binding?.KeyLabelResourceKey ?? "KeyLabelUnmapped";
    }

    private static string StatusResourceKey(BookmarksEditorState state)
    {
        BookmarkEditorProblem? problem = state switch
        {
            BookmarksBrowsing browsing => browsing.Problem,
            BookmarkDrafting draft => draft.Problem,
            BookmarkCategoryDrafting category => category.Problem,
            _ => null,
        };
        return problem is not null
            ? ProblemResourceKey(problem)
            : state switch
            {
                BookmarkDrafting draft => draft.Original is null
                    ? "BookmarkStatusAdding"
                    : "BookmarkStatusEditing",
                BookmarkCategoryDrafting category => category.Original is null
                    ? "BookmarkStatusAddingCategory"
                    : "BookmarkStatusRenamingCategory",
                BookmarkCategoryDeleteConfirmation => "BookmarkStatusConfirmCategoryDelete",
                BookmarkNavigationPending => "BookmarkStatusNavigationPending",
                BookmarkNavigationFailed => "BookmarkStatusNavigationFailed",
                _ => "BookmarkStatusReady",
            };
    }

    private static string ProblemResourceKey(BookmarkEditorProblem problem)
    {
        if (problem == BookmarkEditorProblem.InvalidName)
        {
            return "BookmarkProblemInvalidName";
        }
        if (problem == BookmarkEditorProblem.InvalidPath)
        {
            return "BookmarkProblemInvalidPath";
        }
        if (problem == BookmarkEditorProblem.InvalidCategory)
        {
            return "BookmarkProblemInvalidCategory";
        }
        if (problem == BookmarkEditorProblem.DuplicateCategory)
        {
            return "BookmarkProblemDuplicateCategory";
        }
        if (problem == BookmarkEditorProblem.DuplicateBookmark)
        {
            return "BookmarkProblemDuplicateBookmark";
        }
        if (problem == BookmarkEditorProblem.DuplicateShortcut)
        {
            return "BookmarkProblemDuplicateShortcut";
        }
        if (problem == BookmarkEditorProblem.CategoryLimit)
        {
            return "BookmarkProblemCategoryLimit";
        }
        string fallback = problem == BookmarkEditorProblem.BookmarkLimit
            ? "BookmarkProblemBookmarkLimit"
            : "BookmarkProblemStaleSelection";
        return problem == BookmarkEditorProblem.CategoryDeleteCollision
            ? "BookmarkProblemCategoryDeleteCollision"
            : fallback;
    }
}
