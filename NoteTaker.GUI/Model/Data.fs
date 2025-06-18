namespace NoteTaker.Model

open System
open System.IO
open Elmish
open Thoth.Json.Net
open Avalonia.Input
open TextMateSharp.Grammars
open Avalonia.Media
open Avalonia
open TextMateSharp.Registry
open Avalonia.Media.TextFormatting

module Data =
    module MD =
        let sample =
            """# Markdown Syntax Guide

Markdown is a lightweight markup language for formatting text.
Here are some common elements:

## Headings

Use `#` for headings:

```
# Heading 1
## Heading 2
### Heading 3
```

## Emphasis

- \*Italic\*: `*italic*` or `_italic_`
- \*\*Bold\*\*: `**bold**` or `__bold__`

## Lists

**Unordered list:**
```
- Item 1
- Item 2
  - Subitem
```

**Ordered list:**
```
1. First
2. Second
```

## Links

```
[Link text](https://example.com)
```

## Images

```
![Alt text](https://example.com/image.png)
```

## Code

Inline code: `` `code` ``

Code block:
```
```python
print("Hello, world!")
```
```

## Blockquotes

```
> This is a quote.
```

## Horizontal Rule

```
---
```

---

Try writing your own markdown using these elements!
"""
