/// TODO:
///     1. Load and register the Markdown grammar using an IRegistryOptions implementation
///     2. Map TextMate scopes to Avalonia Brushes
///     3. Write a custom control that renders each line
///     4. Override OnKeyDown, OnTextInput, OnPointerPressed to
///           i. mutate lines
///          ii. track caret position
///         iii. track selection ranges
///     5. Virtualization: only render visible lines by clipping drawing to Bounds.
///     6. Line measuring cache: reuse FormattedText objects where possible
///     7. Smooth scrolling: manage an offset and draw from y = -scrollY.
///     8. Search, code folding, & bracket matching: leverage the same token streams plus simple
///        algorithms.
module EditorControl
