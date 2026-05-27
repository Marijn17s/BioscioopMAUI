namespace BioscoopMAUI.Controls;

public partial class PageHeader : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title),
        typeof(string),
        typeof(PageHeader),
        string.Empty);

    public static readonly BindableProperty SubtitleProperty = BindableProperty.Create(
        nameof(Subtitle),
        typeof(string),
        typeof(PageHeader),
        string.Empty,
        propertyChanged: OnSubtitleChanged);

    public static readonly BindableProperty BadgeTextProperty = BindableProperty.Create(
        nameof(BadgeText),
        typeof(string),
        typeof(PageHeader),
        string.Empty,
        propertyChanged: OnBadgeTextChanged);

    public static readonly BindableProperty TrailingContentProperty = BindableProperty.Create(
        nameof(TrailingContent),
        typeof(View),
        typeof(PageHeader),
        propertyChanged: OnTrailingContentChanged);

    public static readonly BindableProperty BottomContentProperty = BindableProperty.Create(
        nameof(BottomContent),
        typeof(View),
        typeof(PageHeader),
        propertyChanged: OnBottomContentChanged);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string BadgeText
    {
        get => (string)GetValue(BadgeTextProperty);
        set => SetValue(BadgeTextProperty, value);
    }

    public View? TrailingContent
    {
        get => (View?)GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    public View? BottomContent
    {
        get => (View?)GetValue(BottomContentProperty);
        set => SetValue(BottomContentProperty, value);
    }

    public PageHeader()
    {
        InitializeComponent();
        UpdateSubtitleVisibility(Subtitle);
        UpdateBadgeVisibility(BadgeText);
        UpdateTrailingContent(TrailingContent);
        UpdateBottomContent(BottomContent);
    }

    private static void OnSubtitleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PageHeader pageHeader)
            pageHeader.UpdateSubtitleVisibility(newValue as string ?? string.Empty);
    }

    private static void OnBadgeTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PageHeader pageHeader)
            pageHeader.UpdateBadgeVisibility(newValue as string ?? string.Empty);
    }

    private static void OnTrailingContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PageHeader pageHeader)
            pageHeader.UpdateTrailingContent(newValue as View);
    }

    private static void OnBottomContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PageHeader pageHeader)
            pageHeader.UpdateBottomContent(newValue as View);
    }

    private void UpdateSubtitleVisibility(string subtitle)
    {
        SubtitleLabel.IsVisible = !string.IsNullOrWhiteSpace(subtitle);
    }

    private void UpdateBadgeVisibility(string badgeText)
    {
        var hasBadge = !string.IsNullOrWhiteSpace(badgeText);
        BadgeBorder.IsVisible = hasBadge && TrailingContent is null;
        BadgeLabel.Text = badgeText;
    }

    private void UpdateTrailingContent(View? trailingContent)
    {
        TrailingContentPresenter.Content = trailingContent;
        TrailingContentPresenter.IsVisible = trailingContent is not null;
        UpdateBadgeVisibility(BadgeText);
    }

    private void UpdateBottomContent(View? bottomContent)
    {
        BottomContentPresenter.Content = bottomContent;
        BottomContentPresenter.IsVisible = bottomContent is not null;
    }
}