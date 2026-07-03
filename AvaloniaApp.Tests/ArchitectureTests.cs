using System;
using System.Linq;
using AvaloniaApp.ViewModels;
using AvaloniaApp.Views;
using NetArchTest.Rules;

namespace AvaloniaApp.Tests;

public class ArchitectureTests
{
    private static readonly Types Types = Types.InCurrentDomain();

    [Fact]
    public void ViewModels_ShouldInheritFromViewModelBase()
    {
        var result = Types
            .That()
            .ResideInNamespace("AvaloniaApp.ViewModels")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveName("ViewModelBase") // базовый класс не наследует сам себя
            .And()
            .DoNotHaveNameEndingWith("DialogService") // вложенные заглушки (NullDialogService)
            .And()
            .DoNotHaveNameEndingWith("ClipboardService") // вложенные заглушки (NullClipboardService)
            .And()
            .DoNotHaveNameEndingWith("FileSystemService") // вложенные заглушки (NullFileSystemService)
            .And()
            .DoNotHaveNameEndingWith("Factory") // OCP: фабрики — не ViewModel'ы
            .And()
            .DoNotHaveNameEndingWith("Registry") // OCP: реестр — не ViewModel
            .Should()
            .Inherit(typeof(ViewModelBase))
            .GetResult();

        Assert.True(result.IsSuccessful, $"Failing types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Views_ShouldResideInViewsNamespace()
    {
        var result = Types
            .That()
            .Inherit(typeof(Avalonia.Controls.Window))
            .Or()
            .Inherit(typeof(Avalonia.Controls.UserControl))
            .Should()
            .ResideInNamespace("AvaloniaApp.Views")
            .GetResult();

        Assert.True(result.IsSuccessful, $"Failing types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void ViewModels_ShouldNotReferenceViews()
    {
        var result = Types
            .That()
            .ResideInNamespace("AvaloniaApp.ViewModels")
            .ShouldNot()
            .HaveDependencyOnAny(new[] { "AvaloniaApp.Views" })
            .GetResult();

        Assert.True(result.IsSuccessful, $"Failing types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Models_ShouldNotReferenceViewModelsOrViews()
    {
        var result = Types
            .That()
            .ResideInNamespace("AvaloniaApp.Models")
            .ShouldNot()
            .HaveDependencyOnAny(new[] { "AvaloniaApp.ViewModels", "AvaloniaApp.Views" })
            .GetResult();

        Assert.True(result.IsSuccessful, $"Failing types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Services_ShouldNotReferenceViews()
    {
        var result = Types
            .That()
            .ResideInNamespace("AvaloniaApp.Services")
            .ShouldNot()
            .HaveDependencyOnAny(new[] { "AvaloniaApp.Views" })
            .GetResult();

        Assert.True(result.IsSuccessful, $"Failing types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void ViewModels_ShouldNotReferenceAvaloniaControls()
    {
        var result = Types
            .That()
            .ResideInNamespace("AvaloniaApp.ViewModels")
            .ShouldNot()
            .HaveDependencyOnAny(new[] { "Avalonia.Controls" })
            .GetResult();

        Assert.True(result.IsSuccessful, $"Failing types: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }
}
