---
paths: ["**/*.cs"]
topic: architecture
---
# Architecture

## Rules
```yaml
R1: "∀ static_reference_data ⇒ ¬ ALLOWED. ∃ IReferenceDataService, IFormsCatalog, IMetadataCatalog ∈ DI"
```

## Anti-patterns
```yaml
- p: "using Defaults.Forms or static ReferenceDataService"
  fix: "Inject IReferenceDataService, IFormsCatalog, IMetadataCatalog via DI"
```