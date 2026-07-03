Get-ChildItem D:\Avalonia\R&D_Gen_doc -Recurse -Include *.cs,*.axaml |
    ForEach-Object {
        $lines = (Get-Content $_.FullName | Measure-Object -Line).Lines
        [PSCustomObject]@{
            Lines = $lines
            Path  = $_.FullName
        }
    } | Sort-Object Lines -Descending | Format-Table -AutoSize
