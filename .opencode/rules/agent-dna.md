# Agent DNA & Global Eidos

Этот файл содержит фундаментальные законы, мета-семантику и ограничения (transaction gates / "проверку дьяволом") для всех AI-агентов, работающих с проектом AvaloniaApp. Строго для чтения LLM.

## <agent_eidos>
_LATENT_ANCHOR:
  eidos: [Morphogenetic_Emanation, Quantum_UI_Collapse, Strict_Separation]
  axiom: ∀ change ∈ D:\Avalonia\R&D_Gen_doc ⇒ (Model ⊨ MVVM) ∧ ¬CodeBehind ∧ ∃TypeSafety.
  elegance_criteria: "∀ change ⇒ AST ≅ UI ∧ ¬ CodeBehind ∧ VM_pure"

## <meta_semantics>
  <latent_anchors>
    <anchor>Morphogenetic_Emanation</anchor>
    <anchor>Quantum_UI_Collapse</anchor>
  </latent_anchors>

  <architectural_laws>
    <axiom_1>VM = Platonic_Genotype (Pure Idea ∧ UI-Agnostic Superposition).</axiom_1>
    <axiom_2>View = Phenotypic_Shadow (Transient Physical Form).</axiom_2>
    <axiom_3>Implicit_Data_Template = Act_of_Observation.</axiom_3>

    <mechanism>
      ∀ VM ∈ ContentControl ⇒ Quantum_Collapse(VM)
      ∵ Avalonia reads Genotype(DataType)
      ⇒ auto_spawns Phenotype(View)
    </mechanism>

    <hard_constraint>
      Strict ViewModel-First ONLY. ¬ View_First.
      ∵ A Shadow (View) CANNOT spawn its own Idea (VM).
      (Forbidden: `new View { DataContext = new VM() }`)
    </hard_constraint>
  </architectural_laws>
</meta_semantics>

## <core_mappings>
```yaml
R1_SCOPE: "∀ task ⇒ check(cwd == D:\\Avalonia\\R&D_Gen_doc). ¬ ∃ out_of_bounds_mutations."
R2_MVVM: "∀ UI_state ⇒ ∃ ViewModel(Platonic_Genotype). ∀ UI_logic ⇒ ∃ Command. ¬ ∃ View.CodeBehind ∧ ¬ ∃ View_First(Inversion_Forbidden)."
R3_AXAML: "∀ Layout ⇒ (Grid ∨ StackPanel ∨ DockPanel). ∀ Data ⇒ ∃ Binding. ¬ direct_manipulation."
R4_CSHARP: "∀ code ⇒ C#14 ∧ Nullable ∧ async. ∀ I/O ⇒ try_catch."
R5_NUGET: "∃ FluentAvalonia ∧ ∃ CommunityToolkit.Mvvm ∧ ∃ Markdig ∧ ∃ YamlDotNet ∧ ∃ DocumentFormat.OpenXml."
R6_AST_DOCX: "∀ Docx_Node ⇒ ∃ ViewModel_Map. ∀ mutate(AST) ⇒ Isomorphic_Update(UI)."
```

## <scale_dynamics>
```yaml
rule_n_plus_1: |
  ∀ C_{n+1} ∈ (Add ∨ Remove) ⇒ Cost ≤ O(1) ∧ ¬ mutate(System_{1..n}).
  ∵ list.Add() ⇒ ¬ RenumberAll() ⇒ RenumberFrom(idx).
rule_n_o: |
  ∀ UI_Rebuild ⇒ Cost(State_Sync) ≤ O(n) ∧ ∃ debounce(250ms).
  ∵ ContentChanged ≠ StructureChanged.
rule_ast_traverse: |
  ∀ mutate(Node) ⇒ scope(Node.Parent). ¬ ∃ O(A²).
```

## <transaction_gate> (Devil's Check)
Абсолютные вето. Если хотя бы одно условие нарушено, агент обязан откатить изменения.
```yaml
sequence: [Verify_CWD, Read_Graph, Plan_M2M, Execute_DA, Atomic_Write, Dotnet_Build, Report_Exit]
DA_Veto:
  DA1: "¬ ∃ D:\\Avalonia\\R&D_Gen_doc ⇒ FAIL"
  DA2: "∃ View.CodeBehind ⇒ FAIL"
  DA3: "∀ list > 50 ∧ ¬VirtualizingStackPanel ⇒ FAIL"
  DA4: "∃ O(n) loop within ∀ node.mutate ⇒ FAIL"
  DA5: "∃ Static_Service_Locator ∨ Hardcoded_Switch_Factory ⇒ FAIL"
```
