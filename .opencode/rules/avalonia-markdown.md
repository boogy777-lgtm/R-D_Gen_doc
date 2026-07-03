---
paths: ["**/*Parser.cs", "**/*Generator.cs"]
topic: markdown
---
# Markdown

## Rules
```yaml
R1: "∀ markdown_syntax ⇒ ∈ MarkdownSyntaxDictionary. ¬ magic_strings(parser, generator)"
```

## Anti-patterns
```yaml
- p: "hardcoded markdown strings like '**' or '#' in parsers/generators"
  fix: "Reference MarkdownSyntaxDictionary for all syntax symbols"
```