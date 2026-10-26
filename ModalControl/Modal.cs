using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace ModalControl;

/// <summary>
/// Provides a reusable modal overlay control with configurable content,
/// appearance, and background-click behavior.
/// </summary>
public class Modal : ContentControl
{
    /// <summary>
    /// Defines the <see cref="IsOpen"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsOpenProperty = AvaloniaProperty.Register<
        Modal,
        bool
    >(nameof(IsOpen), defaultValue: false);

    /// <summary>
    /// Gets or sets whether the modal is currently visible.
    /// </summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="ContentBackground"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> ContentBackgroundProperty =
        AvaloniaProperty.Register<Modal, IBrush?>(
            nameof(ContentBackground),
            defaultValue: Brushes.White
        );

    /// <summary>
    /// Gets or sets the background brush of the modal content area.
    /// </summary>
    public IBrush? ContentBackground
    {
        get => GetValue(ContentBackgroundProperty);
        set => SetValue(ContentBackgroundProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="BackgroundClickCommand"/> property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> BackgroundClickCommandProperty =
        AvaloniaProperty.Register<Modal, ICommand?>(nameof(BackgroundClickCommand));

    /// <summary>
    /// Gets or sets the command executed when the modal background is clicked.
    /// </summary>
    public ICommand? BackgroundClickCommand
    {
        get => GetValue(BackgroundClickCommandProperty);
        set => SetValue(BackgroundClickCommandProperty, value);
    }

    /// <summary>
    /// Defines the <see cref="ContentCornerRadius"/> property.
    /// </summary>
    public static readonly StyledProperty<CornerRadius> ContentCornerRadiusProperty =
        AvaloniaProperty.Register<Modal, CornerRadius>(
            nameof(ContentCornerRadius),
            defaultValue: new CornerRadius(5)
        );

    /// <summary>
    /// Gets or sets the corner radius applied to the modal content.
    /// </summary>
    public CornerRadius ContentCornerRadius
    {
        get => GetValue(ContentCornerRadiusProperty);
        set => SetValue(ContentCornerRadiusProperty, value);
    }

    /// <summary>
    /// Applies the control template and configures pointer handling for the modal background
    /// and content areas.
    /// </summary>
    /// <param name="e">Event arguments containing the applied template's name scope.</param>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Retrieve the named template parts used for background and content interaction.
        var backgroundGrid = e.NameScope.Find<Grid>("PART_BackgroundGrid");
        var modalContent = e.NameScope.Find<Border>("PART_ModalContent");

        backgroundGrid?.PointerPressed += (sender, args) =>
        {
            // Only trigger the background command when the overlay itself was clicked,
            // not when the pointer event originated from a child element.
            if (args.Source == backgroundGrid)
            {
                if (BackgroundClickCommand?.CanExecute(this) == true)
                    BackgroundClickCommand.Execute(this);
            }
        };

        modalContent?.PointerPressed += (sender, args) =>
        {
            // Prevent clicks inside the modal content from propagating to the background.
            args.Handled = true;
        };
    }
}
