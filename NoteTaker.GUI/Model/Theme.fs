namespace NoteTaker.Model

open Avalonia.Media

/// Represents base16 theme
type Theme = {
    Name : string
    Author : string
    IsDark : bool
    /// Default Background
    Base00 : Color
    /// Lighter Background (Status bars, line numbers)
    Base01 : Color
    /// Selection Background
    Base02 : Color
    /// Comments, Invisibles, Line Highlighting
    Base03 : Color
    /// Dark Foreground (Status bars)
    Base04 : Color
    /// Default Foreground, Caret, Delimiters, Operators
    Base05 : Color
    /// Light Foreground (Not often used)
    Base06 : Color
    /// Light Background (Not often used)
    Base07 : Color
    /// Variables, XML Tags, Markup Link Text, Markup Lists, Diff Deleted
    Base08 : Color
    /// Integers, Boolean, Constants, XML Attributes, Markup Link Url
    Base09 : Color
    /// Classes, Markup Bold, Search Text Background
    Base0A : Color
    /// Strings, Inherited Class, Markup Code, Diff Inserted
    Base0B : Color
    /// Support, Regular Expressions, Escape Characters, Markup Quotes
    Base0C : Color
    /// Functions, Methods, Attribute IDs, Headings
    Base0D : Color
    /// Keywords, Storage, Selector, Markup Italic, Diff Changed
    Base0E : Color
    /// Deprecated, Opening/Closing Embedded Language Tags
    Base0F : Color
}

module Theme =
    module Presets =
        let solarizedDark : Theme = {
            Name = "Solarized Dark"
            Author = "Ethan Schoonover"
            IsDark = true
            Base00 = "#002b36" |> Color.Parse
            Base01 = "#073642" |> Color.Parse
            Base02 = "#586e75" |> Color.Parse
            Base03 = "#657b83" |> Color.Parse
            Base04 = "#839496" |> Color.Parse
            Base05 = "#93a1a1" |> Color.Parse
            Base06 = "#eee8d5" |> Color.Parse
            Base07 = "#fdf6e3" |> Color.Parse
            Base08 = "#dc322f" |> Color.Parse
            Base09 = "#cb4b16" |> Color.Parse
            Base0A = "#b58900" |> Color.Parse
            Base0B = "#859900" |> Color.Parse
            Base0C = "#2aa198" |> Color.Parse
            Base0D = "#268bd2" |> Color.Parse
            Base0E = "#6c71c4" |> Color.Parse
            Base0F = "#d33682" |> Color.Parse
        }

        let solarizedLight : Theme = {
            Name = "Solarized Light"
            Author = "Ethan Schoonover"
            IsDark = false
            Base00 = "#fdf6e3" |> Color.Parse
            Base01 = "#eee8d5" |> Color.Parse
            Base02 = "#93a1a1" |> Color.Parse
            Base03 = "#839496" |> Color.Parse
            Base04 = "#657b83" |> Color.Parse
            Base05 = "#586e75" |> Color.Parse
            Base06 = "#073642" |> Color.Parse
            Base07 = "#002b36" |> Color.Parse
            Base08 = "#dc322f" |> Color.Parse
            Base09 = "#cb4b16" |> Color.Parse
            Base0A = "#b58900" |> Color.Parse
            Base0B = "#859900" |> Color.Parse
            Base0C = "#2aa198" |> Color.Parse
            Base0D = "#268bd2" |> Color.Parse
            Base0E = "#6c71c4" |> Color.Parse
            Base0F = "#d33682" |> Color.Parse
        }

    let mapping theme =
        [
            // Document structure
            "text.html.markdown", theme.Base05
            "meta.frontmatter.markdown", theme.Base0F
            "meta.embedded.block.frontmatter", theme.Base0F

            // Headings
            "markup.heading.markdown", theme.Base0D
            "markup.heading.setext.1.markdown", theme.Base0D
            "markup.heading.setext.2.markdown", theme.Base0D
            "punctuation.definition.heading.markdown", theme.Base04
            "entity.name.section.markdown", theme.Base0D

            // Emphasis and formatting
            "markup.bold.markdown", theme.Base0B
            "markup.italic.markdown", theme.Base0E
            "markup.bold.italic.markdown", theme.Base0B
            "punctuation.definition.bold.markdown", theme.Base04
            "punctuation.definition.italic.markdown", theme.Base04
            "punctuation.definition.emphasis.markdown", theme.Base04

            // Quotes
            "markup.quote.markdown", theme.Base0C
            "punctuation.definition.quote.markdown", theme.Base03
            "beginning.punctuation.definition.quote.markdown", theme.Base03

            // Lists
            "markup.list.unnumbered.markdown", theme.Base0A
            "markup.list.numbered.markdown", theme.Base0A
            "beginning.punctuation.definition.list.markdown", theme.Base03
            "punctuation.definition.list.begin.markdown", theme.Base03

            // Links
            "markup.underline.link.markdown", theme.Base09
            "markup.underline.link.image.markdown", theme.Base08
            "meta.link.inline.markdown", theme.Base09
            "meta.link.reference.markdown", theme.Base09
            "meta.link.reference.def.markdown", theme.Base09
            "meta.link.reference.literal.markdown", theme.Base09
            "meta.link.email.lt-gt.markdown", theme.Base09
            "meta.link.inet.markdown", theme.Base09
            "string.other.link.title.markdown", theme.Base0B
            "string.other.link.description.markdown", theme.Base0B
            "string.other.link.description.title.markdown", theme.Base0B
            "punctuation.definition.link.markdown", theme.Base04
            "punctuation.definition.link.begin.markdown", theme.Base04
            "punctuation.definition.link.end.markdown", theme.Base04
            "punctuation.separator.key-value.markdown", theme.Base04
            "constant.other.reference.link.markdown", theme.Base08
            "punctuation.definition.constant.markdown", theme.Base04
            "punctuation.definition.constant.begin.markdown", theme.Base04
            "punctuation.definition.constant.end.markdown", theme.Base04

            // Images
            "meta.image.inline.markdown", theme.Base08
            "meta.image.reference.markdown", theme.Base08
            "markup.image.markdown", theme.Base08
            "string.other.image.title.markdown", theme.Base08

            // Code - Inline
            "markup.inline.raw.string.markdown", theme.Base0B
            "markup.inline.raw.markdown", theme.Base0B
            "punctuation.definition.raw.markdown", theme.Base04
            "punctuation.definition.raw.begin.markdown", theme.Base04
            "punctuation.definition.raw.end.markdown", theme.Base04

            // Code - Fenced blocks
            "markup.fenced_code.block.markdown", theme.Base0B
            "punctuation.definition.markdown", theme.Base03
            "fenced_code.block.language", theme.Base0E
            "fenced_code.block.language.attributes", theme.Base0E
            "fenced_code.block.marker.backtick.markdown", theme.Base03

            // Embedded code blocks (various languages)
            "meta.embedded.block.css", theme.Base0B
            "meta.embedded.block.html", theme.Base0B
            "meta.embedded.block.ini", theme.Base0B
            "meta.embedded.block.java", theme.Base0B
            "meta.embedded.block.lua", theme.Base0B
            "meta.embedded.block.makefile", theme.Base0B
            "meta.embedded.block.perl", theme.Base0B
            "meta.embedded.block.r", theme.Base0B
            "meta.embedded.block.ruby", theme.Base0B
            "meta.embedded.block.php", theme.Base0B
            "meta.embedded.block.sql", theme.Base0B
            "meta.embedded.block.vs_net", theme.Base0B
            "meta.embedded.block.xml", theme.Base0B
            "meta.embedded.block.xsl", theme.Base0B
            "meta.embedded.block.yaml", theme.Base0B
            "meta.embedded.block.dosbatch", theme.Base0B
            "meta.embedded.block.clojure", theme.Base0B
            "meta.embedded.block.coffee", theme.Base0B
            "meta.embedded.block.c", theme.Base0B
            "meta.embedded.block.cpp", theme.Base0B
            "meta.embedded.block.diff", theme.Base0B
            "meta.embedded.block.dockerfile", theme.Base0B
            "meta.embedded.block.git_commit", theme.Base0B
            "meta.embedded.block.git_rebase", theme.Base0B
            "meta.embedded.block.go", theme.Base0B
            "meta.embedded.block.groovy", theme.Base0B
            "meta.embedded.block.jade", theme.Base0B
            "meta.embedded.block.javascript", theme.Base0B
            "meta.embedded.block.js_regexp", theme.Base0B
            "meta.embedded.block.json", theme.Base0B
            "meta.embedded.block.less", theme.Base0B
            "meta.embedded.block.objc", theme.Base0B
            "meta.embedded.block.scss", theme.Base0B
            "meta.embedded.block.perl6", theme.Base0B
            "meta.embedded.block.powershell", theme.Base0B
            "meta.embedded.block.python", theme.Base0B
            "meta.embedded.block.regexp_python", theme.Base0B
            "meta.embedded.block.rust", theme.Base0B
            "meta.embedded.block.scala", theme.Base0B
            "meta.embedded.block.shellscript", theme.Base0B
            "meta.embedded.block.typescript", theme.Base0B
            "meta.embedded.block.typescriptreact", theme.Base0B
            "meta.embedded.block.csharp", theme.Base0B
            "meta.embedded.block.fsharp", theme.Base0B

            // Raw blocks
            "markup.raw.block.markdown", theme.Base0B

            // Separators
            "meta.separator.markdown", theme.Base03

            // HTML
            "comment.block.html", theme.Base03
            "punctuation.definition.comment.html", theme.Base03

            // Paragraphs
            "meta.paragraph.markdown", theme.Base05

            // String delimiters
            "punctuation.definition.string.begin.markdown", theme.Base04
            "punctuation.definition.string.end.markdown", theme.Base04
            "punctuation.definition.string.markdown", theme.Base04
            "punctuation.definition.metadata.markdown", theme.Base04

            // Math (if supported)
            "markup.math.inline.markdown", theme.Base0C
            "punctuation.definition.math.begin.markdown", theme.Base04
            "punctuation.definition.math.end.markdown", theme.Base04

            // Tables
            "markup.table.markdown", theme.Base0A
            "punctuation.separator.table.markdown", theme.Base03

            // Footnotes
            "markup.footnote.definition.markdown", theme.Base09
            "markup.footnote.reference.markdown", theme.Base09

            // Special characters
            "meta.other.valid-ampersand.markdown", theme.Base05
            "meta.other.valid-bracket.markdown", theme.Base05
            "constant.character.escape.markdown", theme.Base08

            // YAML frontmatter
            "meta.block.yaml.markdown", theme.Base0F
            "meta.separator.metadata.markdown", theme.Base0F

            // Default fallbacks
            "source", theme.Base05
            "text", theme.Base05
        ]
        |> Map.ofList
